namespace LabViroMol.Modules.AdminBff.Application.Dashboard.ViewModels;

public record AdminDashboardMaterialAlertViewModel(
    Guid Id,
    string Name,
    string Location,
    decimal StockQuantity,
    decimal MinStock,
    string Unit);
