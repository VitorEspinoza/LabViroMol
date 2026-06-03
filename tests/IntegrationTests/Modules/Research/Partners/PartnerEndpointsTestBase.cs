using System;
using System.Threading.Tasks;
using LabViroMol.Modules.Research.IntegrationTests;

namespace LabViroMol.Modules.Research.IntegrationTests.Partners;

public abstract class PartnerEndpointsTestBase : BaseIntegrationTest
{
    protected const string BaseRoute = "/api/research/partners";

    protected PartnerEndpointsTestBase(ResearchIntegrationTestWebAppFactory factory) : base(factory) { }

    protected Task<Guid> SeedPartnerAsync()
        => PartnerDataSeeder.SeedPartnerAsync(DbContext);
}