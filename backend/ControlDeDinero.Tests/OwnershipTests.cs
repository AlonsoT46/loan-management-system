using ControlDeDinero.Application;
using ControlDeDinero.Application.Clients;
using ControlDeDinero.Application.Loans;
using ControlDeDinero.Application.Summary;

namespace ControlDeDinero.Tests;

/// <summary>
/// Two users share the same (fake) database. Each one must only see and change their own data.
/// Another user's data is "not found", exactly as if it did not exist.
/// </summary>
public class OwnershipTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 2, 0, 0, TimeSpan.Zero);
    private static readonly Guid UserA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private readonly FakeClientRepository _clientRepo = new();
    private readonly FakeLoanRepository _loanRepo;
    private readonly FakeCurrentUser _user = new();
    private readonly ClientService _clients;
    private readonly LoanService _loans;
    private readonly SummaryService _summary;

    public OwnershipTests()
    {
        _loanRepo = new FakeLoanRepository(_clientRepo);
        var time = new FixedTimeProvider(Now);

        _clients = new ClientService(_clientRepo, _user);
        _loans = new LoanService(_loanRepo, _clientRepo, _user, time);
        _summary = new SummaryService(_loanRepo, _user, time);

        _user.UserId = UserA;
    }

    /// <summary>As user A: a client, a 50,000 loan (1 quincena, total 60,000) and a 10,000 payment.</summary>
    private async Task<(Guid ClientId, Guid LoanId, Guid PaymentId)> SeedAsAAsync()
    {
        _user.UserId = UserA;

        var client = await _clients.CreateAsync(new CreateClientRequest("Ana", null, null));
        var loan = await _loans.CreateAsync(new CreateLoanRequest(
            client.Client.Id, 50_000m, new DateTime(2026, 11, 1), new DateTime(2026, 11, 14), null, null));
        var paid = await _loans.RegisterPaymentAsync(
            loan.Loan.Id, new RegisterPaymentRequest(10_000m, new DateTime(2026, 11, 1), null));

        Assert.NotNull(paid);
        return (client.Client.Id, loan.Loan.Id, paid!.Payments.Single().Id);
    }

    [Fact]
    public async Task CreateClient_AssignsTheCurrentUserAsOwner()
    {
        await _clients.CreateAsync(new CreateClientRequest("Ana", null, null));

        Assert.Equal(UserA, _clientRepo.Clients.Single().OwnerId);
    }

    [Fact]
    public async Task OtherUser_DoesNotSeeClients()
    {
        var seed = await SeedAsAAsync();

        _user.UserId = UserB;

        Assert.Empty(await _clients.SearchAsync(null, includeInactive: true));
        Assert.Null(await _clients.GetAsync(seed.ClientId));
    }

    [Fact]
    public async Task OtherUser_CannotChangeAClient()
    {
        var seed = await SeedAsAAsync();

        _user.UserId = UserB;

        Assert.Null(await _clients.UpdateAsync(seed.ClientId, new UpdateClientRequest("Hacked", null, null)));
        Assert.Null(await _clients.DeactivateAsync(seed.ClientId));
        Assert.Null(await _clients.ActivateAsync(seed.ClientId));

        var client = _clientRepo.Clients.Single();
        Assert.Equal("Ana", client.Name);
        Assert.True(client.IsActive);
    }

    [Fact]
    public async Task DuplicateWarning_IgnoresOtherUsersClients()
    {
        await SeedAsAAsync(); // user A already has an "Ana"

        _user.UserId = UserB;
        var result = await _clients.CreateAsync(new CreateClientRequest("Ana", null, null));

        Assert.Empty(result.PossibleDuplicates);
    }

    [Fact]
    public async Task OtherUser_CannotCreateALoanForSomeoneElsesClient()
    {
        var seed = await SeedAsAAsync();
        var loansBefore = _loanRepo.Loans.Count;

        _user.UserId = UserB;

        await Assert.ThrowsAsync<ValidationException>(() => _loans.CreateAsync(new CreateLoanRequest(
            seed.ClientId, 1_000m, new DateTime(2026, 11, 1), new DateTime(2026, 11, 14), null, null)));

        Assert.Equal(loansBefore, _loanRepo.Loans.Count);
    }

    [Fact]
    public async Task OtherUser_CannotReadOrChangeALoan()
    {
        var seed = await SeedAsAAsync();

        _user.UserId = UserB;

        Assert.Null(await _loans.GetAsync(seed.LoanId));
        Assert.Null(await _loans.RegisterPaymentAsync(seed.LoanId, new RegisterPaymentRequest(1_000m, null, null)));
        Assert.Null(await _loans.VoidPaymentAsync(seed.LoanId, seed.PaymentId, null));
        Assert.Null(await _loans.CancelAsync(seed.LoanId, null));

        // The owner still sees the loan exactly as it was.
        _user.UserId = UserA;
        var loan = await _loans.GetAsync(seed.LoanId);
        Assert.NotNull(loan);
        Assert.Equal(50_000m, loan!.Loan.RemainingBalance);
        Assert.Equal("Active", loan.Loan.Status);
        Assert.False(loan.Payments.Single().IsVoided);
    }

    [Fact]
    public async Task OtherUser_ListsAreEmpty_AndTheOwnerStillSeesTheirData()
    {
        await SeedAsAAsync();

        _user.UserId = UserB;
        Assert.Empty(await _loans.SearchAsync(null, null, false));
        Assert.Empty(await _loans.SearchPaymentsAsync(null, null, includeVoided: true));

        _user.UserId = UserA;
        Assert.Single(await _loans.SearchAsync(null, null, false));
        Assert.Single(await _loans.SearchPaymentsAsync(null, null, includeVoided: true));
    }

    [Fact]
    public async Task Summary_CountsOnlyTheUsersOwnLoans()
    {
        await SeedAsAAsync();

        _user.UserId = UserB;
        var other = await _summary.GetAsync(null);
        Assert.Equal(0m, other.TotalLent);
        Assert.Equal(0m, other.TotalCollected);
        Assert.Equal(0m, other.OutstandingBalance);
        Assert.Equal(0, other.ActiveLoans);

        _user.UserId = UserA;
        var own = await _summary.GetAsync(null);
        Assert.Equal(50_000m, own.TotalLent);
        Assert.Equal(10_000m, own.TotalCollected);
        Assert.Equal(50_000m, own.OutstandingBalance);
        Assert.Equal(1, own.ActiveLoans);
    }
}
