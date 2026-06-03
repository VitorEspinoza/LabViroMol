using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Research.IntegrationTests.Researchers;
using LabViroMol.Modules.Research.IntegrationTests.Partners;
using LabViroMol.Modules.Research.Presentation.Projects;
using Xunit;

namespace LabViroMol.Modules.Research.IntegrationTests.Projects;

public class CreateProjectTests : ProjectEndpointsTestBase
{
    public CreateProjectTests(ResearchIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn201_WhenRequestIsValid()
    {
        var partnerId = await PartnerDataSeeder.SeedPartnerAsync(DbContext);
        var (researcherId, _) = await ResearcherDataSeeder.SeedResearcherAsync(DbContext);

        var response = await Client.PostAsJsonAsync(
            BaseRoute,
            new CreateProjectRequest(researcherId, "Projeto de Pesquisa", "Descrição detalhada", partnerId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn400_WhenTitleIsEmpty()
    {
        var partnerId = await PartnerDataSeeder.SeedPartnerAsync(DbContext);
        var (researcherId, _) = await ResearcherDataSeeder.SeedResearcherAsync(DbContext);

        var response = await Client.PostAsJsonAsync(
            BaseRoute,
            new CreateProjectRequest(researcherId, "", "Descrição válida", partnerId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public class GetProjectTests : ProjectEndpointsTestBase
{
    public GetProjectTests(ResearchIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn200WithItems_WhenGettingAll()
    {
        await SeedProjectAsync();

        var response = await Client.GetAsync(BaseRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn200_WhenProjectExists()
    {
        var (projectId, _, _) = await SeedProjectAsync();

        var response = await Client.GetAsync($"{BaseRoute}/{projectId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenProjectDoesNotExist()
    {
        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class StartProjectTests : ProjectEndpointsTestBase
{
    public StartProjectTests(ResearchIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn204_WhenPlannedAndByLead()
    {
        var (projectId, leadId, _) = await SeedProjectAsync();

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{projectId}/start",
            new ResearcherIdRequest(leadId));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn422_WhenAlreadyInProgress()
    {
        var (projectId, leadId, _) = await SeedStartedProjectAsync();

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{projectId}/start",
            new ResearcherIdRequest(leadId));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}

public class CompleteProjectTests : ProjectEndpointsTestBase
{
    public CompleteProjectTests(ResearchIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn204_WhenInProgress()
    {
        var (projectId, leadId, _) = await SeedStartedProjectAsync();

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{projectId}/complete",
            new ResearcherIdRequest(leadId));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn422_WhenPlanned()
    {
        var (projectId, leadId, _) = await SeedProjectAsync();

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{projectId}/complete",
            new ResearcherIdRequest(leadId));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}

public class CancelProjectTests : ProjectEndpointsTestBase
{
    public CancelProjectTests(ResearchIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn204_WhenPlanned()
    {
        var (projectId, leadId, _) = await SeedProjectAsync();

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{projectId}/cancel",
            new ResearcherIdRequest(leadId));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn422_WhenCompleted()
    {
        var (projectId, leadId, _) = await SeedStartedProjectAsync();
        await Client.PostAsJsonAsync($"{BaseRoute}/{projectId}/complete", new ResearcherIdRequest(leadId));

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{projectId}/cancel",
            new ResearcherIdRequest(leadId));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}

public class AddMemberTests : ProjectEndpointsTestBase
{
    public AddMemberTests(ResearchIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn204_WhenRequestIsValid()
    {
        var (projectId, leadId, _) = await SeedProjectAsync();
        var (newMemberId, _) = await ResearcherDataSeeder.SeedResearcherAsync(DbContext);

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{projectId}/members",
            new AddProjectMemberRequest(newMemberId, ProjectRole.Collaborator.Value, leadId));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenProjectDoesNotExist()
    {
        var (leadId, _) = await ResearcherDataSeeder.SeedResearcherAsync(DbContext);
        var (newMemberId, _) = await ResearcherDataSeeder.SeedResearcherAsync(DbContext);

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{Guid.NewGuid()}/members",
            new AddProjectMemberRequest(newMemberId, ProjectRole.Collaborator.Value, leadId));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class TransferLeadershipTests : ProjectEndpointsTestBase
{
    public TransferLeadershipTests(ResearchIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn204_WhenRequestIsValid()
    {
        var (projectId, leadId, _) = await SeedProjectAsync();
        var (memberId, _) = await ResearcherDataSeeder.SeedResearcherAsync(DbContext);

        await Client.PostAsJsonAsync($"{BaseRoute}/{projectId}/members",
            new AddProjectMemberRequest(memberId, ProjectRole.Collaborator.Value, leadId));

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{projectId}/transfer-leadership",
            new TransferLeadershipRequest(memberId, leadId));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenProjectDoesNotExist()
    {
        var (leadId, _) = await ResearcherDataSeeder.SeedResearcherAsync(DbContext);
        var (memberId, _) = await ResearcherDataSeeder.SeedResearcherAsync(DbContext);

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{Guid.NewGuid()}/transfer-leadership",
            new TransferLeadershipRequest(memberId, leadId));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class ChangeMemberRoleTests : ProjectEndpointsTestBase
{
    public ChangeMemberRoleTests(ResearchIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn204_WhenRequestIsValid()
    {
        var (projectId, leadId, _) = await SeedProjectAsync();
        var (memberId, _) = await ResearcherDataSeeder.SeedResearcherAsync(DbContext);

        await Client.PostAsJsonAsync($"{BaseRoute}/{projectId}/members",
            new AddProjectMemberRequest(memberId, ProjectRole.Collaborator.Value, leadId));

        var response = await Client.PutAsJsonAsync(
            $"{BaseRoute}/{projectId}/members/{memberId}/role",
            new ChangeMemberRoleRequest(ProjectRole.Manager.Value, leadId));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenProjectDoesNotExist()
    {
        var (leadId, _) = await ResearcherDataSeeder.SeedResearcherAsync(DbContext);
        var (memberId, _) = await ResearcherDataSeeder.SeedResearcherAsync(DbContext);

        var response = await Client.PutAsJsonAsync(
            $"{BaseRoute}/{Guid.NewGuid()}/members/{memberId}/role",
            new ChangeMemberRoleRequest(ProjectRole.Manager.Value, leadId));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class RemoveMemberTests : ProjectEndpointsTestBase
{
    public RemoveMemberTests(ResearchIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn204_WhenRequestIsValid()
    {
        var (projectId, leadId, _) = await SeedProjectAsync();
        var (memberId, _) = await ResearcherDataSeeder.SeedResearcherAsync(DbContext);

        await Client.PostAsJsonAsync($"{BaseRoute}/{projectId}/members",
            new AddProjectMemberRequest(memberId, ProjectRole.Collaborator.Value, leadId));

        var response = await Client.DeleteAsync($"{BaseRoute}/{projectId}/members/{memberId}?requestedById={leadId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenProjectDoesNotExist()
    {
        var (leadId, _) = await ResearcherDataSeeder.SeedResearcherAsync(DbContext);
        var (memberId, _) = await ResearcherDataSeeder.SeedResearcherAsync(DbContext);

        var response = await Client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}/members/{memberId}?requestedById={leadId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn422_WhenTargetIsResearchLead()
    {
        var (projectId, leadId, _) = await SeedProjectAsync();

        var response = await Client.DeleteAsync($"{BaseRoute}/{projectId}/members/{leadId}?requestedById={leadId}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}

public class UpdateProjectTests : ProjectEndpointsTestBase
{
    public UpdateProjectTests(ResearchIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn204_WhenRequestIsValid()
    {
        var (projectId, leadId, _) = await SeedProjectAsync();

        var response = await Client.PutAsJsonAsync(
            $"{BaseRoute}/{projectId}",
            new UpdateProjectRequest("Titulo Atualizado", "Descricao atualizada do projeto", leadId));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenProjectDoesNotExist()
    {
        var (leadId, _) = await ResearcherDataSeeder.SeedResearcherAsync(DbContext);

        var response = await Client.PutAsJsonAsync(
            $"{BaseRoute}/{Guid.NewGuid()}",
            new UpdateProjectRequest("Titulo Atualizado", "Descricao atualizada", leadId));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn400_WhenTitleIsEmpty()
    {
        var (projectId, leadId, _) = await SeedProjectAsync();

        var response = await Client.PutAsJsonAsync(
            $"{BaseRoute}/{projectId}",
            new UpdateProjectRequest("", "Descricao atualizada", leadId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}