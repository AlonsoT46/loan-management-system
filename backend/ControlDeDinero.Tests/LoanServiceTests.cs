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
    private readonly FakeLoanRepository _loans = new();

    private LoanService NewService() => new(_loans, _clients, new FixedTimeProvider(Now));

    private Client AddClient(bool isActive = true)
    {
        var client = new Client { Id = Guid.NewGuid(), Name = "Ana", IsActive = isActive };
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
}
