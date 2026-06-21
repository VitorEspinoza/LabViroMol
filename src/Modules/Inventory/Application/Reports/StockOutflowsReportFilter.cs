namespace LabViroMol.Modules.Inventory.Application.Reports;

public record StockOutflowsReportFilter(
    DateTime? From = null,
    DateTime? To = null,
    Guid? MaterialId = null,
    Guid? MaterialTypeId = null,
    Guid? ProjectId = null);
