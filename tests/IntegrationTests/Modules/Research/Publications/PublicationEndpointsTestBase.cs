using System;
using System.Threading.Tasks;
using LabViroMol.Modules.Research.IntegrationTests;

namespace LabViroMol.Modules.Research.IntegrationTests.Publications;

public abstract class PublicationEndpointsTestBase : BaseIntegrationTest
{
    protected const string BaseRoute = "/api/research/publications";

    protected PublicationEndpointsTestBase(ResearchIntegrationTestWebAppFactory factory) : base(factory) { }

    protected Task<Guid> SeedPublicationAsync()
        => PublicationDataSeeder.SeedPublicationAsync(DbContext);
}