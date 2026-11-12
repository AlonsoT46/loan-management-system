using ControlDeDinero.Application;
using ControlDeDinero.Application.Loans;
using ControlDeDinero.Domain;

namespace ControlDeDinero.Tests;

public class LoanServiceTests
{
    // 2026-11-02 02:00 UTC is still 2026-11-01 20:00 in Costa Rica (UTC-6).
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 2, 0, 0, TimeSpan.Zero);
    private static readonly DateTime CostaRicaToday = new(2026, 11, 1);

    private readonly FakeClientRepository _clients = new();
    private readonly FakeLoanRepository _loans;

    public LoanServiceTests()
    {
        _loans = new FakeLoanRepository(_clients);
    }

    private LoanService NewService() => new(_loans, _clients, new FakeCurrentUser(), new FixedTimeProvider(Now));

    private Client AddClient(bool isActive = true)
    {
        var client = new Client { Id = Guid.NewGuid(), OwnerId = FakeCurrentUser.DefaultUserId, Name = "Ana", IsActive = isActive };
        _clients.Clients.Add(client);
        return client;
    }

    private static CreateLoanRequest Request(
        Guid clientId,
        decimal principal = 50_000m,
        DateTime? loanDate = null,
        DateTime? dueDate = null) =>
        new(clientId, principal, loanDate, dueDate ?? new DateTime(2026, 11, 14), null, null);

    [Fact]
    public async Task Create_CalculatesInterestAndTotal()
    {
        var client = AddClient();

        var result = await NewService().CreateAsync(
            Request(client.Id, 50_000m, new DateTime(2026, 11, 1), new DateTime(2026, 11, 30)));

        Assert.Equal(2, result.Loan.Quincenas);
        Assert.Equal(20_000m, result.Loan.InterestAmount);
        Assert.Equal(70_000m, result.Loan.TotalAmount);
        Assert.Equal(70_000m, result.Loan.RemainingBalance);
        Assert.Equal("Active", result.Loan.Status);
        Assert.False(result.Loan.IsOverdue);
        Assert.Empty(result.Payments);
        Assert.Single(_loans.Loans);
        Assert.Equal(1, _loans.SaveCount);
    }

    [Fact]
    public async Task Create_DefaultsLoanDateToTodayInCostaRica()
    {
        var client = AddClient();

        var result = await NewService().CreateAsync(Request(client.Id, loanDate: null));

        Assert.Equal(CostaRicaToday, result.Loan.LoanDate.Date);
    }

    [Fact]
    public async Task Create_RejectsUnknownClient()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            NewService().CreateAsync(Request(Guid.NewGuid())));
    }

    [Fact]
    public async Task Create_RejectsInactiveClient()
    {
        var client = AddClient(isActive: false);

        await Assert.ThrowsAsync<ValidationException>(() =>
            NewService().CreateAsync(Request(client.Id)));
    }

    [Fact]
    public async Task Create_RejectsMissingDueDate()
    {
        var client = AddClient();

        await Assert.ThrowsAsync<ValidationException>(() =>
            NewService().CreateAsync(new CreateLoanRequest(client.Id, 1_000m, null, null, null, null)));
    }

    [Fact]
    public async Task Create_TurnsDomainErrorsIntoValidationErrors()
    {
        var client = AddClient();

        await Assert.ThrowsAsync<ValidationException>(() =>
            NewService().CreateAsync(Request(client.Id, principal: 0m)));
        await Assert.ThrowsAsync<ValidationException>(() =>
            NewService().CreateAsync(Request(client.Id, loanDate: new DateTime(2026, 11, 10), dueDate: new DateTime(2026, 11, 1))));
        Assert.Empty(_loans.Loans);
    }

    [Fact]
    public async Task RegisterPayment_UpdatesBalanceAndReturnsThePayment()
    {
        var client = AddClient();
        var service = NewService();
        var created = await service.CreateAsync(Request(client.Id)); // 50,000 for 1 quincena => 60,000

        var result = await service.RegisterPaymentAsync(
            created.Loan.Id,
            new RegisterPaymentRequest(12_000m, new DateTime(2026, 11, 5), "primer abono"));

        Assert.NotNull(result);
        Assert.Equal(48_000m, result!.Loan.RemainingBalance);
        Assert.Equal(12_000m, result.Loan.TotalPaid);
        var payment = Assert.Single(result.Payments);
        Assert.Equal(10_000m, payment.PrincipalPaid);
        Assert.Equal(2_000m, payment.InterestPaid);
        Assert.Equal("primer abono", payment.Notes);
    }

    [Fact]
    public async Task RegisterPayment_DefaultsDateToTodayInCostaRica()
    {
        var client = AddClient();
        var service = NewService();
        var created = await service.CreateAsync(Request(client.Id));

        var result = await service.RegisterPaymentAsync(created.Loan.Id, new RegisterPaymentRequest(1_000m, null, null));

        Assert.Equal(CostaRicaToday, Assert.Single(result!.Payments).PaymentDate.Date);
    }

    [Fact]
    public async Task RegisterPayment_FullAmountClosesTheLoan()
    {
        var client = AddClient();
        var service = NewService();
        var created = await service.CreateAsync(Request(client.Id));

        var result = await service.RegisterPaymentAsync(created.Loan.Id, new RegisterPaymentRequest(60_000m, null, null));

        Assert.Equal("Paid", result!.Loan.Status);
        Assert.Equal(0m, result.Loan.RemainingBalance);
    }

    [Fact]
    public async Task RegisterPayment_RejectsOverpaymentAndBadDates()
    {
        var client = AddClient();
        var service = NewService();
        var created = await service.CreateAsync(Request(client.Id)); // total 60,000

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.RegisterPaymentAsync(created.Loan.Id, new RegisterPaymentRequest(60_000.01m, null, null)));
        await Assert.ThrowsAsync<ValidationException>(() =>
            service.RegisterPaymentAsync(created.Loan.Id, new RegisterPaymentRequest(1_000m, new DateTime(2026, 10, 1), null)));
        await Assert.ThrowsAsync<ValidationException>(() =>
            service.RegisterPaymentAsync(created.Loan.Id, new RegisterPaymentRequest(0m, null, null)));
    }

    [Fact]
    public async Task RegisterPayment_ReturnsNullWhenLoanDoesNotExist()
    {
        var result = await NewService().RegisterPaymentAsync(Guid.NewGuid(), new RegisterPaymentRequest(1_000m, null, null));

        Assert.Null(result);
    }

    [Fact]
    public async Task Get_ReturnsNullWhenLoanDoesNotExist()
    {
        Assert.Null(await NewService().GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Search_OnlyOverdue_UsesTheCostaRicaDate()
    {
        var client = AddClient();
        var service = NewService();

        // Today in Costa Rica is Nov 1 (UTC is already Nov 2).
        var late = await service.CreateAsync(Request(client.Id, loanDate: new DateTime(2026, 10, 15), dueDate: new DateTime(2026, 10, 31)));
        var dueToday = await service.CreateAsync(Request(client.Id, loanDate: new DateTime(2026, 10, 18), dueDate: new DateTime(2026, 11, 1)));
        await service.CreateAsync(Request(client.Id, loanDate: new DateTime(2026, 11, 1), dueDate: new DateTime(2026, 11, 30)));

        var overdue = await service.SearchAsync(null, null, onlyOverdue: true);
        var all = await service.SearchAsync(null, null, onlyOverdue: false);

        Assert.Equal(late.Loan.Id, Assert.Single(overdue).Id);
        Assert.True(overdue[0].IsOverdue);
        Assert.Equal(3, all.Count);
        Assert.False(all.Single(l => l.Id == dueToday.Loan.Id).IsOverdue);
    }

    [Fact]
    public async Task Search_FiltersByClientAndStatus()
    {
        var ana = AddClient();
        var luis = AddClient();
        var service = NewService();
        var anaLoan = await service.CreateAsync(Request(ana.Id));
        await service.CreateAsync(Request(luis.Id));
        await service.RegisterPaymentAsync(anaLoan.Loan.Id, new RegisterPaymentRequest(60_000m, null, null));

        var byClient = await service.SearchAsync(luis.Id, null, false);
        var paid = await service.SearchAsync(null, LoanStatus.Paid, false);

        Assert.Equal(luis.Id, Assert.Single(byClient).ClientId);
        Assert.Equal(anaLoan.Loan.Id, Assert.Single(paid).Id);
    }

    [Fact]
    public async Task Cancel_CancelsALoanWithoutPayments()
    {
        var client = AddClient();
        var service = NewService();
        var created = await service.CreateAsync(Request(client.Id));

        var result = await service.CancelAsync(created.Loan.Id, new CancelLoanRequest("registered by mistake"));

        Assert.Equal("Cancelled", result!.Loan.Status);
    }

    [Fact]
    public async Task Cancel_RejectsALoanWithActivePayments()
    {
        var client = AddClient();
        var service = NewService();
        var created = await service.CreateAsync(Request(client.Id));
        await service.RegisterPaymentAsync(created.Loan.Id, new RegisterPaymentRequest(1_000m, null, null));

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CancelAsync(created.Loan.Id, null));
    }

    [Fact]
    public async Task Cancel_ReturnsNullWhenLoanDoesNotExist()
    {
        Assert.Null(await NewService().CancelAsync(Guid.NewGuid(), null));
    }

    [Fact]
    public async Task VoidPayment_RecalculatesTheLoanAndKeepsThePaymentFlagged()
    {
        var client = AddClient();
        var service = NewService();
        var created = await service.CreateAsync(Request(client.Id)); // total 60,000
        var paid = await service.RegisterPaymentAsync(created.Loan.Id, new RegisterPaymentRequest(60_000m, null, null));
        Assert.Equal("Paid", paid!.Loan.Status);

        var result = await service.VoidPaymentAsync(
            created.Loan.Id,
            paid.Payments[0].Id,
            new VoidPaymentRequest("wrong amount"));

        Assert.Equal("Active", result!.Loan.Status);
        Assert.Equal(60_000m, result.Loan.RemainingBalance);
        var payment = Assert.Single(result.Payments);
        Assert.True(payment.IsVoided);
        Assert.Equal("wrong amount", payment.VoidReason);
        Assert.Equal(Now.UtcDateTime, payment.VoidedAtUtc);
    }

    [Fact]
    public async Task VoidPayment_ReturnsNullForUnknownLoanOrPayment()
    {
        var client = AddClient();
        var service = NewService();
        var created = await service.CreateAsync(Request(client.Id));

        Assert.Null(await service.VoidPaymentAsync(Guid.NewGuid(), Guid.NewGuid(), null));
        Assert.Null(await service.VoidPaymentAsync(created.Loan.Id, Guid.NewGuid(), null));
    }

    [Fact]
    public async Task Search_FiltersLoansByMonth_AndRejectsABadMonth()
    {
        var client = AddClient();
        var service = NewService();
        await service.CreateAsync(Request(client.Id, loanDate: new DateTime(2026, 10, 15), dueDate: new DateTime(2026, 10, 29)));
        var november = await service.CreateAsync(Request(client.Id, loanDate: new DateTime(2026, 11, 1), dueDate: new DateTime(2026, 11, 14)));

        var result = await service.SearchAsync(null, null, false, "2026-11");

        Assert.Equal(november.Loan.Id, Assert.Single(result).Id);
        await Assert.ThrowsAsync<ValidationException>(() => service.SearchAsync(null, null, false, "2026-13"));
        await Assert.ThrowsAsync<ValidationException>(() => service.SearchAsync(null, null, false, "noviembre"));
    }

    [Fact]
    public async Task SearchPayments_FiltersByMonthClientAndVoided()
    {
        var ana = AddClient();
        var luis = AddClient();
        var service = NewService();
        var anaLoan = await service.CreateAsync(Request(ana.Id, loanDate: new DateTime(2026, 10, 15), dueDate: new DateTime(2026, 10, 29)));
        var luisLoan = await service.CreateAsync(Request(luis.Id, loanDate: new DateTime(2026, 10, 15), dueDate: new DateTime(2026, 10, 29)));

        await service.RegisterPaymentAsync(anaLoan.Loan.Id, new RegisterPaymentRequest(12_000m, new DateTime(2026, 10, 20), null));
        var anaNovember = await service.RegisterPaymentAsync(anaLoan.Loan.Id, new RegisterPaymentRequest(6_000m, new DateTime(2026, 11, 5), null));
        await service.RegisterPaymentAsync(luisLoan.Loan.Id, new RegisterPaymentRequest(6_000m, new DateTime(2026, 11, 3), null));

        var november = await service.SearchPaymentsAsync(null, "2026-11", includeVoided: false);
        var onlyLuis = await service.SearchPaymentsAsync(luis.Id, null, includeVoided: false);

        Assert.Equal(2, november.Count);
        Assert.Equal(new DateTime(2026, 11, 5), november[0].PaymentDate); // newest first
        Assert.Equal(luis.Id, Assert.Single(onlyLuis).ClientId);

        await service.VoidPaymentAsync(anaLoan.Loan.Id, anaNovember!.Payments.Single(p => p.PaymentDate == new DateTime(2026, 11, 5)).Id, null);

        Assert.Single(await service.SearchPaymentsAsync(null, "2026-11", includeVoided: false));
        Assert.Equal(2, (await service.SearchPaymentsAsync(null, "2026-11", includeVoided: true)).Count);
    }

    [Fact]
    public async Task Archive_AndUnarchive_ChangeTheStatus()
    {
        var client = AddClient();
        var service = NewService();
        var created = await service.CreateAsync(Request(client.Id));

        var archived = await service.ArchiveAsync(created.Loan.Id, new ArchiveLoanRequest("no va a pagar"));
        Assert.Equal("Archived", archived!.Loan.Status);
        Assert.False(archived.Loan.IsOverdue);

        var back = await service.UnarchiveAsync(created.Loan.Id);
        Assert.Equal("Active", back!.Loan.Status);
    }

    [Fact]
    public async Task Archive_RejectsArchivingTwice()
    {
        var client = AddClient();
        var service = NewService();
        var created = await service.CreateAsync(Request(client.Id));
        await service.ArchiveAsync(created.Loan.Id, null);

        await Assert.ThrowsAsync<ValidationException>(() => service.ArchiveAsync(created.Loan.Id, null));
    }

    [Fact]
    public async Task Archive_ReturnsNullWhenLoanDoesNotExist()
    {
        Assert.Null(await NewService().ArchiveAsync(Guid.NewGuid(), null));
        Assert.Null(await NewService().UnarchiveAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Update_ChangesClientAmountAndDates()
    {
        var ana = AddClient();
        var luis = AddClient();
        var service = NewService();
        var created = await service.CreateAsync(Request(ana.Id, 50_000m, new DateTime(2026, 11, 1), new DateTime(2026, 11, 14)));

        var updated = await service.UpdateAsync(created.Loan.Id, new UpdateLoanRequest(
            luis.Id, 100_000m, new DateTime(2026, 11, 1), new DateTime(2026, 11, 30), "corregido"));

        Assert.Equal(luis.Id, updated!.Loan.ClientId);
        Assert.Equal(100_000m, updated.Loan.PrincipalAmount);
        Assert.Equal(2, updated.Loan.Quincenas);
        Assert.Equal(140_000m, updated.Loan.TotalAmount);
        Assert.Equal("corregido", updated.Loan.Notes);
    }

    [Fact]
    public async Task Update_WithPayments_AllowsOnlyTheNotes()
    {
        var client = AddClient();
        var service = NewService();
        var created = await service.CreateAsync(Request(client.Id, 50_000m, new DateTime(2026, 11, 1), new DateTime(2026, 11, 14)));
        await service.RegisterPaymentAsync(created.Loan.Id, new RegisterPaymentRequest(1_000m, null, null));

        var notesOnly = await service.UpdateAsync(created.Loan.Id, new UpdateLoanRequest(
            client.Id, 50_000m, new DateTime(2026, 11, 1), new DateTime(2026, 11, 14), "solo la nota"));
        Assert.Equal("solo la nota", notesOnly!.Loan.Notes);

        await Assert.ThrowsAsync<ValidationException>(() => service.UpdateAsync(created.Loan.Id, new UpdateLoanRequest(
            client.Id, 80_000m, new DateTime(2026, 11, 1), new DateTime(2026, 11, 14), null)));
    }

    [Fact]
    public async Task Update_RejectsAClientOfAnotherUser_AndReturnsNullForUnknownLoan()
    {
        var client = AddClient();
        var service = NewService();
        var created = await service.CreateAsync(Request(client.Id));

        var stranger = new Client { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid(), Name = "Otro" };
        _clients.Clients.Add(stranger);

        await Assert.ThrowsAsync<ValidationException>(() => service.UpdateAsync(created.Loan.Id, new UpdateLoanRequest(
            stranger.Id, 50_000m, null, new DateTime(2026, 11, 14), null)));

        Assert.Null(await service.UpdateAsync(Guid.NewGuid(), new UpdateLoanRequest(
            client.Id, 1_000m, null, new DateTime(2026, 11, 14), null)));
    }
}
