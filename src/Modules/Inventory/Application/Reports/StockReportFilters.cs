using LabViroMol.Modules.Inventory.Domain.Materials;

namespace LabViroMol.Modules.Inventory.Application.Reports;

public sealed record StockReportFilter(
    DateTime? From,
    DateTime? To,
    Guid? MaterialId,
    Guid? MaterialTypeId,
    Guid? ProjectId)
{
    private const int MaxRangeDays = 366;

    public string? Validate()
    {
        if (!From.HasValue || !To.HasValue)
            return "Informe data inicial e data final para gerar relatorios transacionais.";

        if (From.HasValue && To.HasValue && From.Value > To.Value)
            return "A data inicial deve ser menor ou igual a data final.";

        if ((To.Value - From.Value).TotalDays > MaxRangeDays)
            return $"O periodo do relatorio deve ter no maximo {MaxRangeDays} dias.";

        if (MaterialId == Guid.Empty)
            return "O material informado e invalido.";

        if (MaterialTypeId == Guid.Empty)
            return "O tipo de material informado e invalido.";

        if (ProjectId == Guid.Empty)
            return "O projeto informado e invalido.";

        return null;
    }
}

public sealed record CriticalStockBalanceFilter(
    Guid? MaterialId,
    Guid? MaterialTypeId,
    bool OnlyCritical = true)
{
    public string? Validate()
    {
        if (MaterialId == Guid.Empty)
            return "O material informado e invalido.";

        if (MaterialTypeId == Guid.Empty)
            return "O tipo de material informado e invalido.";

        return null;
    }
}

public sealed record MaterialAuditMovementsFilter(
    DateTime? From,
    DateTime? To,
    Guid? MaterialId,
    Guid? MaterialTypeId,
    string? TransactionType,
    int? Limit)
{
    private const int MaxRangeDays = 366;

    public string? Validate()
    {
        if (From.HasValue && To.HasValue && From.Value > To.Value)
            return "A data inicial deve ser menor ou igual a data final.";

        if (From.HasValue && To.HasValue && (To.Value - From.Value).TotalDays > MaxRangeDays)
            return $"O periodo do relatorio deve ter no maximo {MaxRangeDays} dias.";

        if (MaterialId == Guid.Empty)
            return "O material informado e invalido.";

        if (MaterialTypeId == Guid.Empty)
            return "O tipo de material informado e invalido.";

        if (Limit is <= 0)
            return "O limite deve ser maior que zero.";

        if (!string.IsNullOrWhiteSpace(TransactionType) &&
            !Enum.TryParse<TransactionType>(TransactionType, true, out _))
        {
            return "O tipo de movimentacao informado e invalido.";
        }

        return null;
    }

    public int EffectiveLimit => Math.Clamp(Limit ?? 500, 1, 1000);
}
