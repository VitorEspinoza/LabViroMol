using LabViroMol.Modules.Inventory.Application.Reports;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace LabViroMol.Modules.Inventory.Presentation.Reports;

internal sealed class StockReportRequest
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public Guid? MaterialId { get; init; }
    public Guid? MaterialTypeId { get; init; }
    public Guid? ProjectId { get; init; }

    public StockReportFilter ToFilter()
    {
        return new StockReportFilter(
            NormalizeDateTime(From),
            NormalizeDateTime(To),
            MaterialId,
            MaterialTypeId,
            ProjectId);
    }

    private static DateTime? NormalizeDateTime(DateTime? value) =>
        value switch
        {
            null => null,
            { Kind: DateTimeKind.Utc } dt => dt,
            { Kind: DateTimeKind.Local } dt => dt.ToUniversalTime(),
            { Kind: DateTimeKind.Unspecified } dt => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
            _ => value
        };
}

internal sealed class CriticalStockBalanceRequest
{
    public Guid? MaterialId { get; init; }
    public Guid? MaterialTypeId { get; init; }
    public bool? OnlyCritical { get; init; }

    public CriticalStockBalanceFilter ToFilter()
    {
        return new CriticalStockBalanceFilter(MaterialId, MaterialTypeId, OnlyCritical ?? true);
    }
}

internal sealed class MaterialAuditMovementsRequest
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public Guid? MaterialId { get; init; }
    public Guid? MaterialTypeId { get; init; }
    public string? TransactionType { get; init; }
    public int? Limit { get; init; }

    public MaterialAuditMovementsFilter ToFilter()
    {
        return new MaterialAuditMovementsFilter(
            NormalizeDateTime(From),
            NormalizeDateTime(To),
            MaterialId,
            MaterialTypeId,
            TransactionType,
            Limit);
    }

    private static DateTime? NormalizeDateTime(DateTime? value) =>
        value switch
        {
            null => null,
            { Kind: DateTimeKind.Utc } dt => dt,
            { Kind: DateTimeKind.Local } dt => dt.ToUniversalTime(),
            { Kind: DateTimeKind.Unspecified } dt => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
            _ => value
        };
}

internal sealed class StockMovementsByUserRequest
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public Guid? MaterialId { get; init; }
    public string? TransactionType { get; init; }

    public StockMovementsByUserFilter ToFilter()
    {
        return new StockMovementsByUserFilter(
            NormalizeDateTime(From),
            NormalizeDateTime(To),
            MaterialId,
            TransactionType);
    }

    private static DateTime? NormalizeDateTime(DateTime? value) =>
        value switch
        {
            null => null,
            { Kind: DateTimeKind.Utc } dt => dt,
            { Kind: DateTimeKind.Local } dt => dt.ToUniversalTime(),
            { Kind: DateTimeKind.Unspecified } dt => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
            _ => value
        };
}

internal sealed class IdleStockRequest
{
    public Guid? MaterialTypeId { get; init; }
    public DateTime? Since { get; init; }

    public IdleStockFilter ToFilter()
    {
        return new IdleStockFilter(MaterialTypeId, NormalizeDateTime(Since));
    }

    private static DateTime? NormalizeDateTime(DateTime? value) =>
        value switch
        {
            null => null,
            { Kind: DateTimeKind.Utc } dt => dt,
            { Kind: DateTimeKind.Local } dt => dt.ToUniversalTime(),
            { Kind: DateTimeKind.Unspecified } dt => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
            _ => value
        };
}

internal sealed class OrderStatusCycleRequest
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int? StaleDays { get; init; }

    public OrderStatusCycleFilter ToFilter()
    {
        return new OrderStatusCycleFilter(
            NormalizeDateTime(From),
            NormalizeDateTime(To),
            StaleDays);
    }

    private static DateTime? NormalizeDateTime(DateTime? value) =>
        value switch
        {
            null => null,
            { Kind: DateTimeKind.Utc } dt => dt,
            { Kind: DateTimeKind.Local } dt => dt.ToUniversalTime(),
            { Kind: DateTimeKind.Unspecified } dt => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
            _ => value
        };
}

internal sealed class StockByMaterialTypeRequest
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }

    public StockByMaterialTypeFilter ToFilter()
    {
        return new StockByMaterialTypeFilter(NormalizeDateTime(From), NormalizeDateTime(To));
    }

    private static DateTime? NormalizeDateTime(DateTime? value) =>
        value switch
        {
            null => null,
            { Kind: DateTimeKind.Utc } dt => dt,
            { Kind: DateTimeKind.Local } dt => dt.ToUniversalTime(),
            { Kind: DateTimeKind.Unspecified } dt => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
            _ => value
        };
}

internal static class InventoryReportEndpoints
{
    public static void MapInventoryReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/reports")
            .WithTags("Inventory-Reports")
            .RequireAuthorization();

        group.MapGet("/stock-outflows/by-project.pdf", async (
            [AsParameters] StockReportRequest request,
            IStockReportQueries queries,
            IStockReportPdfGenerator pdfGenerator,
            ICurrentUser currentUser,
            CancellationToken ct) =>
            await GeneratePdfAsync(
                currentUser,
                request.ToFilter(),
                queries.GetStockOutflowsByProjectAsync,
                pdfGenerator.GenerateStockOutflowsByProject,
                "stock-outflows-by-project.pdf",
                ct))
            .Produces<FileContentHttpResult>(StatusCodes.Status200OK, "application/pdf");

        group.MapGet("/stock-outflows/by-month.pdf", async (
            [AsParameters] StockReportRequest request,
            IStockReportQueries queries,
            IStockReportPdfGenerator pdfGenerator,
            ICurrentUser currentUser,
            CancellationToken ct) =>
            await GeneratePdfAsync(
                currentUser,
                request.ToFilter(),
                queries.GetStockOutflowsByMonthAsync,
                pdfGenerator.GenerateStockOutflowsByMonth,
                "stock-outflows-by-month.pdf",
                ct))
            .Produces<FileContentHttpResult>(StatusCodes.Status200OK, "application/pdf");

        group.MapGet("/stock-outflows/totals.pdf", async (
            [AsParameters] StockReportRequest request,
            IStockReportQueries queries,
            IStockReportPdfGenerator pdfGenerator,
            ICurrentUser currentUser,
            CancellationToken ct) =>
            await GeneratePdfAsync(
                currentUser,
                request.ToFilter(),
                queries.GetStockOutflowTotalsAsync,
                pdfGenerator.GenerateStockOutflowTotals,
                "stock-outflow-totals.pdf",
                ct))
            .Produces<FileContentHttpResult>(StatusCodes.Status200OK, "application/pdf");

        group.MapGet("/stock-inflows/by-order-material-month.pdf", async (
            [AsParameters] StockReportRequest request,
            IStockReportQueries queries,
            IStockReportPdfGenerator pdfGenerator,
            ICurrentUser currentUser,
            CancellationToken ct) =>
            await GeneratePdfAsync(
                currentUser,
                request.ToFilter(),
                queries.GetStockInflowsByOrderMaterialMonthAsync,
                pdfGenerator.GenerateStockInflowsByOrderMaterialMonth,
                "stock-inflows-by-order-material-month.pdf",
                ct))
            .Produces<FileContentHttpResult>(StatusCodes.Status200OK, "application/pdf");

        group.MapGet("/critical-stock-balance.pdf", async (
            [AsParameters] CriticalStockBalanceRequest request,
            IStockReportQueries queries,
            IStockReportPdfGenerator pdfGenerator,
            ICurrentUser currentUser,
            CancellationToken ct) =>
        {
            if (!CanViewStockReports(currentUser))
                return Results.Forbid();

            var filter = request.ToFilter();
            var validationError = filter.Validate();
            if (validationError is not null)
                return Results.BadRequest(validationError);

            var report = await queries.GetCriticalStockBalanceAsync(filter, ct);
            var pdf = pdfGenerator.GenerateCriticalStockBalance(report);

            return Results.File(pdf, "application/pdf", "critical-stock-balance.pdf");
        }).Produces<FileContentHttpResult>(StatusCodes.Status200OK, "application/pdf");

        group.MapGet("/material-audit-movements.pdf", async (
            [AsParameters] MaterialAuditMovementsRequest request,
            IStockReportQueries queries,
            IStockReportPdfGenerator pdfGenerator,
            ICurrentUser currentUser,
            CancellationToken ct) =>
        {
            if (!CanViewStockReports(currentUser))
                return Results.Forbid();

            var filter = request.ToFilter();
            var validationError = filter.Validate();
            if (validationError is not null)
                return Results.BadRequest(validationError);

            var report = await queries.GetMaterialAuditMovementsAsync(filter, ct);
            var pdf = pdfGenerator.GenerateMaterialAuditMovements(report);

            return Results.File(pdf, "application/pdf", "material-audit-movements.pdf");
        }).Produces<FileContentHttpResult>(StatusCodes.Status200OK, "application/pdf");

        group.MapGet("/manual-stock-adjustments.pdf", async (
            [AsParameters] StockReportRequest request,
            IStockReportQueries queries,
            IStockReportPdfGenerator pdfGenerator,
            ICurrentUser currentUser,
            CancellationToken ct) =>
            await GeneratePdfAsync(
                currentUser,
                request.ToFilter(),
                queries.GetManualStockAdjustmentsAsync,
                pdfGenerator.GenerateManualStockAdjustments,
                "manual-stock-adjustments.pdf",
                ct))
            .Produces<FileContentHttpResult>(StatusCodes.Status200OK, "application/pdf");

        group.MapGet("/stock-movements/by-user.pdf", async (
            [AsParameters] StockMovementsByUserRequest request,
            IStockReportQueries queries,
            IStockReportPdfGenerator pdfGenerator,
            ICurrentUser currentUser,
            CancellationToken ct) =>
        {
            if (!CanViewStockReports(currentUser))
                return Results.Forbid();

            var filter = request.ToFilter();
            var validationError = filter.Validate();
            if (validationError is not null)
                return Results.BadRequest(validationError);

            var report = await queries.GetStockMovementsByUserAsync(filter, ct);
            var pdf = pdfGenerator.GenerateStockMovementsByUser(report);

            return Results.File(pdf, "application/pdf", "stock-movements-by-user.pdf");
        }).Produces<FileContentHttpResult>(StatusCodes.Status200OK, "application/pdf");

        group.MapGet("/materials/idle-stock.pdf", async (
            [AsParameters] IdleStockRequest request,
            IStockReportQueries queries,
            IStockReportPdfGenerator pdfGenerator,
            ICurrentUser currentUser,
            CancellationToken ct) =>
        {
            if (!CanViewStockReports(currentUser))
                return Results.Forbid();

            var filter = request.ToFilter();
            var validationError = filter.Validate();
            if (validationError is not null)
                return Results.BadRequest(validationError);

            var report = await queries.GetIdleStockAsync(filter, ct);
            var pdf = pdfGenerator.GenerateIdleStock(report);

            return Results.File(pdf, "application/pdf", "idle-stock.pdf");
        }).Produces<FileContentHttpResult>(StatusCodes.Status200OK, "application/pdf");

        group.MapGet("/orders/status-cycle.pdf", async (
            [AsParameters] OrderStatusCycleRequest request,
            IStockReportQueries queries,
            IStockReportPdfGenerator pdfGenerator,
            ICurrentUser currentUser,
            CancellationToken ct) =>
        {
            if (!CanViewStockReports(currentUser))
                return Results.Forbid();

            var filter = request.ToFilter();
            var validationError = filter.Validate();
            if (validationError is not null)
                return Results.BadRequest(validationError);

            var report = await queries.GetOrderStatusCycleAsync(filter, ct);
            var pdf = pdfGenerator.GenerateOrderStatusCycle(report);

            return Results.File(pdf, "application/pdf", "orders-status-cycle.pdf");
        }).Produces<FileContentHttpResult>(StatusCodes.Status200OK, "application/pdf");

        group.MapGet("/stock/by-material-type.pdf", async (
            [AsParameters] StockByMaterialTypeRequest request,
            IStockReportQueries queries,
            IStockReportPdfGenerator pdfGenerator,
            ICurrentUser currentUser,
            CancellationToken ct) =>
        {
            if (!CanViewStockReports(currentUser))
                return Results.Forbid();

            var filter = request.ToFilter();
            var validationError = filter.Validate();
            if (validationError is not null)
                return Results.BadRequest(validationError);

            var report = await queries.GetStockByMaterialTypeAsync(filter, ct);
            var pdf = pdfGenerator.GenerateStockByMaterialType(report);

            return Results.File(pdf, "application/pdf", "stock-by-material-type.pdf");
        }).Produces<FileContentHttpResult>(StatusCodes.Status200OK, "application/pdf");
    }

    private static async Task<IResult> GeneratePdfAsync<TReport>(
        ICurrentUser currentUser,
        StockReportFilter filter,
        Func<StockReportFilter, CancellationToken, Task<TReport>> getReport,
        Func<TReport, byte[]> generatePdf,
        string fileName,
        CancellationToken ct)
    {
        if (!CanViewStockReports(currentUser))
            return Results.Forbid();

        var validationError = filter.Validate();
        if (validationError is not null)
            return Results.BadRequest(validationError);

        var report = await getReport(filter, ct);
        var pdf = generatePdf(report);

        return Results.File(pdf, "application/pdf", fileName);
    }

    private static bool CanViewStockReports(ICurrentUser currentUser)
    {
        return currentUser.Permissions.Contains(Permissions.Inventory.StockView) ||
               currentUser.Permissions.Contains(Permissions.Inventory.StockManage);
    }
}
