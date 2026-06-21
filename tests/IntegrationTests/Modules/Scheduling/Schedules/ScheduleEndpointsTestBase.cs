using LabViroMol.Modules.Scheduling.IntegrationTests;

namespace LabViroMol.Modules.Scheduling.IntegrationTests.Schedules;

public abstract class ScheduleEndpointsTestBase : BaseIntegrationTest
{
    protected const string BaseRoute = "/api/scheduling/schedules";
    protected const string PublicBaseRoute = "/api/scheduling/public/schedules";

    protected ScheduleEndpointsTestBase(IntegrationTestWebAppFactory factory) : base(factory) { }

    protected Task<Guid> SeedPendingAsync(Guid? equipmentId = null)
        => ScheduleDataSeeder.SeedPendingAsync(DbContext, equipmentId);

    protected Task<Guid> SeedScheduledAsync(Guid? equipmentId = null, Guid? approvedBy = null)
        => ScheduleDataSeeder.SeedScheduledAsync(DbContext, equipmentId, approvedBy);
}
