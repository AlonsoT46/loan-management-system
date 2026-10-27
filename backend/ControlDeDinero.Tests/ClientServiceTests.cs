using ControlDeDinero.Application;
using ControlDeDinero.Application.Clients;
using ControlDeDinero.Domain;

namespace ControlDeDinero.Tests;

public class ClientServiceTests
{
    private readonly FakeClientRepository _repository = new();

    private ClientService NewService() => new(_repository);

    [Fact]
    public async Task Create_TrimsFieldsAndSaves()
    {
        var service = NewService();

        var result = await service.CreateAsync(new CreateClientRequest("  Ana  ", " 8888-0000 ", "  nota "));

        Assert.Equal("Ana", result.Client.Name);
        Assert.Equal("8888-0000", result.Client.Phone);
        Assert.Equal("nota", result.Client.Notes);
        Assert.True(result.Client.IsActive);
        Assert.NotEqual(Guid.Empty, result.Client.Id);
        Assert.Single(_repository.Clients);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task Create_BlankPhoneAndNotesBecomeNull()
    {
        var result = await NewService().CreateAsync(new CreateClientRequest("Ana", "   ", ""));

        Assert.Null(result.Client.Phone);
        Assert.Null(result.Client.Notes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_RejectsEmptyName(string? name)
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            NewService().CreateAsync(new CreateClientRequest(name, null, null)));

        Assert.Empty(_repository.Clients);
    }

    [Fact]
    public async Task Create_RejectsTooLongFields()
    {
        var service = NewService();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(new CreateClientRequest(new string('a', ClientService.MaxNameLength + 1), null, null)));
        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(new CreateClientRequest("Ana", new string('1', ClientService.MaxPhoneLength + 1), null)));
        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(new CreateClientRequest("Ana", null, new string('n', ClientService.MaxNotesLength + 1))));
    }

    [Fact]
    public async Task Create_ReportsPossibleDuplicatesButStillCreates()
    {
        var service = NewService();
        var first = await service.CreateAsync(new CreateClientRequest("Ana", null, null));

        var second = await service.CreateAsync(new CreateClientRequest("ana", null, null));

        Assert.Empty(first.PossibleDuplicates);
        var duplicate = Assert.Single(second.PossibleDuplicates);
        Assert.Equal(first.Client.Id, duplicate.Id);
        Assert.Equal(2, _repository.Clients.Count);
    }

    [Fact]
    public async Task Search_FiltersByNameAndHidesInactiveByDefault()
    {
        var service = NewService();
        await service.CreateAsync(new CreateClientRequest("Ana Mora", null, null));
        await service.CreateAsync(new CreateClientRequest("Luis", null, null));
        var inactive = await service.CreateAsync(new CreateClientRequest("Ana Vega", null, null));
        await service.DeactivateAsync(inactive.Client.Id);

        var active = await service.SearchAsync("ana");
        var all = await service.SearchAsync("ana", includeInactive: true);

        Assert.Equal("Ana Mora", Assert.Single(active).Name);
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task Update_ChangesFields()
    {
        var service = NewService();
        var created = await service.CreateAsync(new CreateClientRequest("Ana", "111", null));

        var updated = await service.UpdateAsync(created.Client.Id, new UpdateClientRequest("Ana María", "222", "nota"));

        Assert.NotNull(updated);
        Assert.Equal("Ana María", updated!.Name);
        Assert.Equal("222", updated.Phone);
        Assert.Equal("nota", updated.Notes);
    }

    [Fact]
    public async Task Update_ReturnsNullWhenClientDoesNotExist()
    {
        var result = await NewService().UpdateAsync(Guid.NewGuid(), new UpdateClientRequest("Ana", null, null));

        Assert.Null(result);
    }

    [Fact]
    public async Task Update_RejectsEmptyName()
    {
        var service = NewService();
        var created = await service.CreateAsync(new CreateClientRequest("Ana", null, null));

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateAsync(created.Client.Id, new UpdateClientRequest("  ", null, null)));
    }

    [Fact]
    public async Task DeactivateAndActivate_ToggleIsActive()
    {
        var service = NewService();
        var created = await service.CreateAsync(new CreateClientRequest("Ana", null, null));

        var deactivated = await service.DeactivateAsync(created.Client.Id);
        var activated = await service.ActivateAsync(created.Client.Id);

        Assert.False(deactivated!.IsActive);
        Assert.True(activated!.IsActive);
    }

    [Fact]
    public async Task Get_ReturnsNullWhenClientDoesNotExist()
    {
        Assert.Null(await NewService().GetAsync(Guid.NewGuid()));
    }
}
