using System.Net;
using System.Net.Http.Json;
using LabViroMol.IntegrationTests.Shared;
using LabViroMol.Modules.Identity.Application.Users.CreateUser;
using LabViroMol.Modules.Identity.Contracts;
using LabViroMol.Modules.Research.Domain.Positions;
using LabViroMol.Modules.Research.Domain.Researchers;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.IntegrationTests.CrossModule;

public class IdentityToResearchTests : BaseCrossModuleTest
{
    public IdentityToResearchTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        AuthenticateAs([]);
    }

    private async Task<Guid> SeedPositionAsync()
    {
        var position = Position.Create("Pesquisador Visitante", "Cargo de teste").Data!;
        await ResearchDbContext.Positions.AddAsync(position);
        await ResearchDbContext.SaveChangesAsync();
        return position.Id.Value;
    }

    [Fact]
    public async Task ShouldCreateResearcherInResearchModule_WhenUserIsCreatedWithResearchData_AfterOutboxDrain()
    {
        var positionId = await SeedPositionAsync();
        var email = $"researcher.{Guid.NewGuid():N}@test.com";

        var command = new CreateUserCommand(
            new UserInfo(
                FirstName: "Maria",
                LastName: "Oliveira",
                PhoneNumber: null,
                EmergencyContactName: null,
                EmergencyContactNumber: null,
                ResearchData: new ResearchRegistrationData(
                    PositionId: positionId,
                    DegreeLevel: nameof(DegreeLevel.Doctorate),
                    FieldOfStudy: "Virologia Molecular",
                    LattesUrl: "http://lattes.cnpq.br/0000000000000000",
                    CitationName: "OLIVEIRA, M.",
                    DisplayName: "Maria Oliveira")),
            email,
            RoleIds: []);

        var response = await Client.PostAsJsonAsync("/api/identity/users", command);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var researcherExistsBeforeDrain = await ResearchDbContext.Researchers.AsNoTracking().AnyAsync();
        Assert.False(researcherExistsBeforeDrain);

        await OutboxDrainer.DrainAsync(Factory.Services);

        var createdUser = await IdentityDbContext.DomainUsers.AsNoTracking()
            .FirstAsync(u => u.Email.Value == email.ToLowerInvariant());

        var researcher = await ResearchDbContext.Researchers.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == ResearcherId.From(createdUser.Id.Value));

        Assert.NotNull(researcher);
        Assert.Equal("Maria", researcher!.Name.FirstName);
        Assert.Equal("Oliveira", researcher.Name.LastName);
        Assert.Equal("Virologia Molecular", researcher.AcademicBackground.FieldOfStudy);
        Assert.Equal(DegreeLevel.Doctorate, researcher.AcademicBackground.DegreeLevel);
    }

    [Fact]
    public async Task ShouldNotCreateResearcher_WhenUserIsCreatedWithoutResearchData()
    {
        var email = $"plain.{Guid.NewGuid():N}@test.com";

        var command = new CreateUserCommand(
            new UserInfo(
                FirstName: "Carlos",
                LastName: "Mendes",
                PhoneNumber: null,
                EmergencyContactName: null,
                EmergencyContactNumber: null,
                ResearchData: null),
            email,
            RoleIds: []);

        var response = await Client.PostAsJsonAsync("/api/identity/users", command);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        await OutboxDrainer.DrainAsync(Factory.Services);

        var createdUser = await IdentityDbContext.DomainUsers.AsNoTracking()
            .FirstAsync(u => u.Email.Value == email.ToLowerInvariant());

        var researcherExists = await ResearchDbContext.Researchers.AsNoTracking()
            .AnyAsync(r => r.Id == ResearcherId.From(createdUser.Id.Value));

        Assert.False(researcherExists);
    }
}
