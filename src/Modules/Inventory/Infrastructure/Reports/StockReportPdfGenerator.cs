using System.Globalization;
using LabViroMol.Modules.Inventory.Application.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LabViroMol.Modules.Inventory.Infrastructure.Reports;

public sealed class StockReportPdfGenerator : IStockReportPdfGenerator
{
    private const string Primary = "#14532D";
    private const string PrimarySoft = "#DCFCE7";
    private const string Accent = "#0F766E";
    private const string Warning = "#B45309";
    private const string Danger = "#B91C1C";
    private const string Ink = "#111827";
    private const string Muted = "#6B7280";
    private const string Line = "#E5E7EB";
    private const string Surface = "#F8FAFC";
    private const string White = "#FFFFFF";

    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    public byte[] GenerateStockOutflowsByProject(StockOutflowsByProjectReport report)
    {
        var rows = report.Rows;
        var totalQuantity = rows.Sum(r => r.TotalQuantity);
        var totalMovements = rows.Sum(r => r.MovementsCount);
        var topProject = rows
            .GroupBy(r => r.ProjectTitle)
            .Select(g => new ChartItem(g.Key, g.Sum(r => r.TotalQuantity), FormatQuantity(g.Sum(r => r.TotalQuantity))))
            .OrderByDescending(x => x.Value)
            .FirstOrDefault();

        return Generate(new ReportDocument(
            report.GeneratedAtUtc,
            "Saidas de material por projeto",
            "Consumo por projeto",
            "Visao consolidada das baixas vinculadas a projetos, com ranking de consumo e rastreabilidade por material.",
            BuildCommonFilters(report.From, report.To, report.MaterialId, report.MaterialTypeId)
                .Append(("Projeto", report.ProjectId?.ToString() ?? "Todos")),
            [
                new Kpi("Quantidade total", FormatQuantity(totalQuantity), "Somatorio das saidas por projeto", Primary),
                new Kpi("Movimentacoes", totalMovements.ToString(Culture), "Registros de baixa considerados", Accent),
                new Kpi("Projetos", rows.Select(r => r.ProjectId).Distinct().Count().ToString(Culture), "Projetos com consumo no periodo", Warning),
                new Kpi("Maior consumo", topProject?.Label ?? "-", topProject?.FormattedValue ?? "Sem dados", Danger)
            ],
            [
                new ChartBlock(
                    "Top projetos por quantidade",
                    "Ranking dos maiores consumidores no periodo filtrado.",
                    LimitWithOthers(
                        rows.GroupBy(r => r.ProjectTitle)
                            .Select(g => new ChartItem(g.Key, g.Sum(r => r.TotalQuantity), FormatQuantity(g.Sum(r => r.TotalQuantity))))
                            .OrderByDescending(x => x.Value),
                        6),
                    Primary)
            ],
            ["Projeto", "Material", "Un.", "Quantidade", "Mov.", "Primeira saida", "Ultima saida"],
            rows.Select(r => new[]
            {
                r.ProjectTitle,
                r.MaterialName,
                r.Unit,
                FormatQuantity(r.TotalQuantity),
                r.MovementsCount.ToString(Culture),
                FormatDate(r.FirstMovementAt),
                FormatDate(r.LastMovementAt)
            }).ToList(),
            $"Total geral: {FormatQuantity(totalQuantity)}"));
    }

    public byte[] GenerateStockOutflowsByMonth(StockOutflowsByMonthReport report)
    {
        var rows = report.Rows;
        var totalQuantity = rows.Sum(r => r.TotalQuantity);
        var topMonth = rows
            .GroupBy(r => $"{r.Year:D4}-{r.Month:D2}")
            .Select(g => new ChartItem(g.Key, g.Sum(r => r.TotalQuantity), FormatQuantity(g.Sum(r => r.TotalQuantity))))
            .OrderByDescending(x => x.Value)
            .FirstOrDefault();

        return Generate(new ReportDocument(
            report.GeneratedAtUtc,
            "Saidas de material por mes",
            "Serie mensal",
            "Evolucao mensal das saidas, separando consumo de projeto e saidas excepcionais.",
            BuildCommonFilters(report.From, report.To, report.MaterialId, report.MaterialTypeId),
            [
                new Kpi("Quantidade total", FormatQuantity(totalQuantity), "Somatorio do periodo", Primary),
                new Kpi("Meses", rows.Select(r => new { r.Year, r.Month }).Distinct().Count().ToString(Culture), "Competencias com saidas", Accent),
                new Kpi("Materiais", rows.Select(r => r.MaterialId).Distinct().Count().ToString(Culture), "Itens movimentados", Warning),
                new Kpi("Pico mensal", topMonth?.Label ?? "-", topMonth?.FormattedValue ?? "Sem dados", Danger)
            ],
            [
                new ChartBlock(
                    "Quantidade por mes",
                    "Barras horizontais para leitura rapida dos periodos de maior saida.",
                    LimitWithOthers(
                        rows.GroupBy(r => $"{r.Year:D4}-{r.Month:D2}")
                            .Select(g => new ChartItem(g.Key, g.Sum(r => r.TotalQuantity), FormatQuantity(g.Sum(r => r.TotalQuantity))))
                            .OrderBy(x => x.Label),
                        12),
                    Accent),
                new ChartBlock(
                    "Top materiais",
                    "Materiais com maior quantidade de saida no periodo.",
                    LimitWithOthers(
                        rows.GroupBy(r => r.MaterialName)
                            .Select(g => new ChartItem(g.Key, g.Sum(r => r.TotalQuantity), FormatQuantity(g.Sum(r => r.TotalQuantity))))
                            .OrderByDescending(x => x.Value),
                        6),
                    Primary)
            ],
            ["Mes", "Material", "Un.", "Tipo", "Quantidade", "Mov."],
            rows.Select(r => new[]
            {
                $"{r.Year:D4}-{r.Month:D2}",
                r.MaterialName,
                r.Unit,
                r.OutflowType,
                FormatQuantity(r.TotalQuantity),
                r.MovementsCount.ToString(Culture)
            }).ToList(),
            $"Total geral: {FormatQuantity(totalQuantity)}"));
    }

    public byte[] GenerateStockOutflowTotals(StockOutflowTotalsReport report)
    {
        var rows = report.Rows;
        var totalQuantity = rows.Sum(r => r.TotalQuantity);
        var exceptionQuantity = rows.Sum(r => r.ExceptionOutQuantity);
        var exceptionShare = totalQuantity == 0 ? 0 : Math.Round(exceptionQuantity / totalQuantity * 100, 2);

        return Generate(new ReportDocument(
            report.GeneratedAtUtc,
            "Saidas totais por material",
            "Ranking geral de saidas",
            "Consolidado por material, com separacao entre consumo de projeto e baixas excepcionais.",
            BuildCommonFilters(report.From, report.To, report.MaterialId, report.MaterialTypeId),
            [
                new Kpi("Quantidade total", FormatQuantity(totalQuantity), "Projeto + excecao", Primary),
                new Kpi("Materiais", rows.Count.ToString(Culture), "Itens no relatorio", Accent),
                new Kpi("Baixas excepcionais", FormatQuantity(exceptionQuantity), $"{exceptionShare:0.##}% do total", Warning),
                new Kpi("Maior participacao", rows.OrderByDescending(r => r.ParticipationPercent).FirstOrDefault()?.MaterialName ?? "-", rows.Count == 0 ? "Sem dados" : $"{rows.Max(r => r.ParticipationPercent):0.##}%", Danger)
            ],
            [
                new ChartBlock(
                    "Top materiais por saida total",
                    "Materiais com maior peso no consumo total.",
                    LimitWithOthers(
                        rows.OrderByDescending(r => r.TotalQuantity)
                            .Select(r => new ChartItem(r.MaterialName, r.TotalQuantity, $"{FormatQuantity(r.TotalQuantity)} {r.Unit}")),
                        8),
                    Primary),
                new ChartBlock(
                    "Projeto vs excecao",
                    "Composicao das saidas no periodo filtrado.",
                    [
                        new ChartItem("Consumo de projeto", rows.Sum(r => r.ProjectConsumptionQuantity), FormatQuantity(rows.Sum(r => r.ProjectConsumptionQuantity))),
                        new ChartItem("Baixa excepcional", exceptionQuantity, FormatQuantity(exceptionQuantity))
                    ],
                    Warning)
            ],
            ["Material", "Tipo material", "Un.", "Projeto", "Excecao", "Total", "%"],
            rows.Select(r => new[]
            {
                r.MaterialName,
                r.MaterialTypeName,
                r.Unit,
                FormatQuantity(r.ProjectConsumptionQuantity),
                FormatQuantity(r.ExceptionOutQuantity),
                FormatQuantity(r.TotalQuantity),
                r.ParticipationPercent.ToString("0.##", Culture)
            }).ToList(),
            $"Total geral: {FormatQuantity(totalQuantity)}"));
    }

    public byte[] GenerateStockInflowsByOrderMaterialMonth(StockInflowsByOrderMaterialMonthReport report)
    {
        var rows = report.Rows;
        var totalQuantity = rows.Sum(r => r.TotalQuantity);
        var orderReceiptQuantity = rows
            .Where(r => r.InflowType.Equals("OrderReceipt", StringComparison.OrdinalIgnoreCase))
            .Sum(r => r.TotalQuantity);

        return Generate(new ReportDocument(
            report.GeneratedAtUtc,
            "Entradas por pedido, material e mes",
            "Recebimentos e entradas excepcionais",
            "Controle das entradas de estoque por origem, material e competencia.",
            BuildCommonFilters(report.From, report.To, report.MaterialId, report.MaterialTypeId),
            [
                new Kpi("Quantidade total", FormatQuantity(totalQuantity), "Entradas no periodo", Primary),
                new Kpi("Por pedido", FormatQuantity(orderReceiptQuantity), "Recebimentos de pedidos", Accent),
                new Kpi("Excepcionais", FormatQuantity(totalQuantity - orderReceiptQuantity), "Entradas manuais", Warning),
                new Kpi("Materiais", rows.Select(r => r.MaterialId).Distinct().Count().ToString(Culture), "Itens com entrada", Danger)
            ],
            [
                new ChartBlock(
                    "Entradas por mes",
                    "Volume recebido por competencia.",
                    LimitWithOthers(
                        rows.GroupBy(r => $"{r.Year:D4}-{r.Month:D2}")
                            .Select(g => new ChartItem(g.Key, g.Sum(r => r.TotalQuantity), FormatQuantity(g.Sum(r => r.TotalQuantity))))
                            .OrderBy(x => x.Label),
                        12),
                    Accent),
                new ChartBlock(
                    "Origem das entradas",
                    "Comparativo entre pedido e lancamentos excepcionais.",
                    rows.GroupBy(r => r.InflowType)
                        .Select(g => new ChartItem(TranslateTransactionType(g.Key), g.Sum(r => r.TotalQuantity), FormatQuantity(g.Sum(r => r.TotalQuantity))))
                        .OrderByDescending(x => x.Value)
                        .ToList(),
                    Primary)
            ],
            ["Mes", "Pedido", "Material", "Un.", "Tipo", "Quantidade", "Mov."],
            rows.Select(r => new[]
            {
                $"{r.Year:D4}-{r.Month:D2}",
                r.OrderId?.ToString() ?? "Sem pedido",
                r.MaterialName,
                r.Unit,
                TranslateTransactionType(r.InflowType),
                FormatQuantity(r.TotalQuantity),
                r.MovementsCount.ToString(Culture)
            }).ToList(),
            $"Total geral: {FormatQuantity(totalQuantity)}"));
    }

    public byte[] GenerateCriticalStockBalance(CriticalStockBalanceReport report)
    {
        var rows = report.Rows;
        var criticalCount = rows.Count(r => r.StockQuantity <= r.MinStock);
        var largestGap = rows.OrderBy(r => r.Difference).FirstOrDefault();

        return Generate(new ReportDocument(
            report.GeneratedAtUtc,
            "Saldo critico de estoque",
            "Estoque atual vs minimo",
            "Mapa de risco para reposicao, destacando materiais abaixo ou no limite minimo.",
            [
                ("Material", report.MaterialId?.ToString() ?? "Todos"),
                ("Tipo de material", report.MaterialTypeId?.ToString() ?? "Todos"),
                ("Apenas criticos", report.OnlyCritical ? "Sim" : "Nao")
            ],
            [
                new Kpi("Materiais listados", rows.Count.ToString(Culture), "Itens considerados", Primary),
                new Kpi("Criticos", criticalCount.ToString(Culture), "Atual menor ou igual ao minimo", Danger),
                new Kpi("Menor saldo", largestGap?.MaterialName ?? "-", largestGap is null ? "Sem dados" : FormatQuantity(largestGap.Difference), Warning),
                new Kpi("Tipos", rows.Select(r => r.MaterialTypeName).Distinct().Count().ToString(Culture), "Categorias impactadas", Accent)
            ],
            [
                new ChartBlock(
                    "Maior deficit de estoque",
                    "Quanto mais negativa a diferenca, maior a prioridade de reposicao.",
                    LimitWithOthers(
                        rows.OrderBy(r => r.Difference)
                            .Select(r => new ChartItem(r.MaterialName, Math.Abs(Math.Min(r.Difference, 0)), $"Dif. {FormatQuantity(r.Difference)}")),
                        8),
                    Danger),
                new ChartBlock(
                    "Estoque atual vs minimo",
                    "Razao entre saldo atual e estoque minimo para materiais mais criticos.",
                    LimitWithOthers(
                        rows.OrderBy(r => r.StockQuantity - r.MinStock)
                            .Select(r =>
                            {
                                var ratio = r.MinStock == 0 ? 100 : Math.Round(r.StockQuantity / r.MinStock * 100, 2);
                                return new ChartItem(r.MaterialName, Math.Min(ratio, 100), $"{FormatQuantity(r.StockQuantity)} / {FormatQuantity(r.MinStock)}");
                            }),
                        8),
                    Warning)
            ],
            ["Material", "Tipo", "Local", "Un.", "Atual", "Minimo", "Diferenca"],
            rows.Select(r => new[]
            {
                r.MaterialName,
                r.MaterialTypeName,
                r.Location,
                r.Unit,
                FormatQuantity(r.StockQuantity),
                FormatQuantity(r.MinStock),
                FormatQuantity(r.Difference)
            }).ToList(),
            $"Materiais listados: {rows.Count}"));
    }

    public byte[] GenerateMaterialAuditMovements(MaterialAuditMovementsReport report)
    {
        var rows = report.Rows;
        var totalQuantity = rows.Sum(r => r.Quantity);
        var monthlySummary = BuildAuditMonthlySummary(rows);

        return Generate(new ReportDocument(
            report.GeneratedAtUtc,
            "Movimentacoes auditaveis por material",
            "Trilha de auditoria",
            "Registro das movimentacoes com usuario, origem, data e justificativa operacional.",
            BuildCommonFilters(report.From, report.To, report.MaterialId, report.MaterialTypeId)
                .Append(("Tipo de transacao", report.TransactionType ?? "Todos"))
                .Append(("Limite", report.Limit.ToString(Culture))),
            [
                new Kpi("Movimentacoes", rows.Count.ToString(Culture), "Registros listados", Primary),
                new Kpi("Quantidade", FormatQuantity(totalQuantity), "Somatorio bruto", Accent),
                new Kpi("Usuarios", rows.Select(r => r.TransactedByUserId).Distinct().Count().ToString(Culture), "Responsaveis distintos", Warning),
                new Kpi("Materiais", rows.Select(r => r.MaterialId).Distinct().Count().ToString(Culture), "Itens movimentados", Danger)
            ],
            [
                new ChartBlock(
                    "Movimentacoes por tipo",
                    "Distribuicao das operacoes auditaveis.",
                    rows.GroupBy(r => TranslateTransactionType(r.TransactionType))
                        .Select(g => new ChartItem(g.Key, g.Count(), g.Count().ToString(Culture)))
                        .OrderByDescending(x => x.Value)
                        .ToList(),
                    Primary),
                new ChartBlock(
                    "Top materiais por quantidade",
                    "Itens com maior volume movimentado.",
                    LimitWithOthers(
                        rows.GroupBy(r => r.MaterialName)
                            .Select(g => new ChartItem(g.Key, g.Sum(r => r.Quantity), FormatQuantity(g.Sum(r => r.Quantity))))
                            .OrderByDescending(x => x.Value),
                        6),
                    Accent)
            ],
            ["Data", "Material", "Tipo material", "Tipo", "Qtd.", "Usuario", "Projeto", "Pedido", "Justificativa"],
            rows.Select(r => new[]
            {
                FormatDate(r.TransactedAt),
                r.MaterialName,
                r.MaterialTypeName,
                TranslateTransactionType(r.TransactionType),
                FormatQuantity(r.Quantity),
                r.TransactedByUserName,
                r.ProjectTitle ?? "-",
                r.OrderId.HasValue ? ShortId(r.OrderId.Value) : "-",
                r.Justification ?? "-"
            }).ToList(),
            $"Movimentacoes listadas: {rows.Count}",
            WrapColumnIndex: 8,
            MonthlySummary: monthlySummary));
    }

    private static MonthlySummarySection? BuildAuditMonthlySummary(IReadOnlyList<MaterialAuditMovementRow> rows)
    {
        var monthlyGroups = rows
            .GroupBy(r => new { r.TransactedAt.Year, r.TransactedAt.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Label = $"{g.Key.Year:D4}-{g.Key.Month:D2}",
                Count = g.Count(),
                Quantity = g.Sum(r => r.Quantity)
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToList();

        if (monthlyGroups.Count == 0)
            return null;

        var avgMovements = (decimal)rows.Count / monthlyGroups.Count;
        var avgQuantity = rows.Sum(r => r.Quantity) / monthlyGroups.Count;
        var busiestMonth = monthlyGroups.OrderByDescending(x => x.Count).First();

        return new MonthlySummarySection(
            [
                new Kpi("Meses no periodo", monthlyGroups.Count.ToString(Culture), "Competencias com movimentacao", Primary),
                new Kpi("Media de movimentacoes/mes", avgMovements.ToString("0.##", Culture), "Media no periodo filtrado", Accent),
                new Kpi("Mes com mais movimentos", busiestMonth.Label, $"{busiestMonth.Count} movimentacoes", Warning),
                new Kpi("Quantidade media/mes", FormatQuantity(avgQuantity), "Somatorio dividido pelos meses", Danger)
            ],
            [
                new ChartBlock(
                    "Movimentacoes por mes",
                    "Contagem de registros auditaveis por competencia.",
                    LimitWithOthers(
                        monthlyGroups.Select(x => new ChartItem(x.Label, x.Count, x.Count.ToString(Culture))),
                        12),
                    Primary),
                new ChartBlock(
                    "Quantidade por mes",
                    "Volume movimentado por competencia.",
                    LimitWithOthers(
                        monthlyGroups.Select(x => new ChartItem(x.Label, x.Quantity, FormatQuantity(x.Quantity))),
                        12),
                    Accent)
            ]);
    }

    public byte[] GenerateManualStockAdjustments(ManualStockAdjustmentsReport report)
    {
        var rows = report.Rows;
        var totalQuantity = rows.Sum(r => r.Quantity);

        return Generate(new ReportDocument(
            report.GeneratedAtUtc,
            "Ajustes manuais de estoque",
            "Auditoria de excecoes",
            "Foco em ExceptionIn e ExceptionOut para revisao de lancamentos manuais.",
            BuildCommonFilters(report.From, report.To, report.MaterialId, report.MaterialTypeId),
            [
                new Kpi("Ajustes", rows.Count.ToString(Culture), "Lancamentos listados", Primary),
                new Kpi("Quantidade", FormatQuantity(totalQuantity), "Somatorio bruto", Accent),
                new Kpi("Entradas", rows.Count(r => r.AdjustmentType.Equals("ExceptionIn", StringComparison.OrdinalIgnoreCase)).ToString(Culture), "Ajustes positivos", Warning),
                new Kpi("Saidas", rows.Count(r => r.AdjustmentType.Equals("ExceptionOut", StringComparison.OrdinalIgnoreCase)).ToString(Culture), "Ajustes negativos", Danger)
            ],
            [
                new ChartBlock(
                    "Ajustes por tipo",
                    "Separacao entre entradas e saidas manuais.",
                    rows.GroupBy(r => TranslateTransactionType(r.AdjustmentType))
                        .Select(g => new ChartItem(g.Key, g.Sum(r => r.Quantity), FormatQuantity(g.Sum(r => r.Quantity))))
                        .OrderByDescending(x => x.Value)
                        .ToList(),
                    Warning),
                new ChartBlock(
                    "Top materiais ajustados",
                    "Materiais com maior volume de ajuste manual.",
                    LimitWithOthers(
                        rows.GroupBy(r => r.MaterialName)
                            .Select(g => new ChartItem(g.Key, g.Sum(r => r.Quantity), FormatQuantity(g.Sum(r => r.Quantity))))
                            .OrderByDescending(x => x.Value),
                        6),
                    Primary)
            ],
            ["Data", "Material", "Tipo material", "Un.", "Ajuste", "Qtd.", "Usuario", "Justificativa"],
            rows.Select(r => new[]
            {
                FormatDate(r.TransactedAt),
                r.MaterialName,
                r.MaterialTypeName,
                r.Unit,
                TranslateTransactionType(r.AdjustmentType),
                FormatQuantity(r.Quantity),
                r.TransactedByUserName,
                r.Justification ?? "-"
            }).ToList(),
            $"Ajustes listados: {rows.Count}",
            WrapColumnIndex: 7));
    }

    public byte[] GenerateStockMovementsByUser(StockMovementsByUserReport report)
    {
        var rows = report.Rows;
        var totalQuantity = rows.Sum(r => r.TotalQuantity);
        var topUser = rows
            .GroupBy(r => r.UserName)
            .Select(g => new ChartItem(g.Key, g.Sum(r => r.TotalQuantity), FormatQuantity(g.Sum(r => r.TotalQuantity))))
            .OrderByDescending(x => x.Value)
            .FirstOrDefault();

        return Generate(new ReportDocument(
            report.GeneratedAtUtc,
            "Consumo e ajustes por usuario responsavel",
            "Prestacao de contas",
            "Movimentacoes de estoque agrupadas por responsavel, com quebra por tipo de transacao.",
            [
                ("De", report.From.HasValue ? FormatDate(report.From.Value) : "Inicio"),
                ("Ate", report.To.HasValue ? FormatDate(report.To.Value) : "Agora"),
                ("Material", report.MaterialId?.ToString() ?? "Todos"),
                ("Tipo de transacao", report.TransactionType ?? "Todos")
            ],
            [
                new Kpi("Quantidade total", FormatQuantity(totalQuantity), "Somatorio das movimentacoes", Primary),
                new Kpi("Movimentacoes", rows.Sum(r => r.MovementsCount).ToString(Culture), "Registros considerados", Accent),
                new Kpi("Usuarios", rows.Select(r => r.UserId).Distinct().Count().ToString(Culture), "Responsaveis distintos", Warning),
                new Kpi("Maior responsavel", topUser?.Label ?? "-", topUser?.FormattedValue ?? "Sem dados", Danger)
            ],
            [
                new ChartBlock(
                    "Top usuarios por quantidade",
                    "Ranking dos responsaveis com maior volume movimentado.",
                    LimitWithOthers(
                        rows.GroupBy(r => r.UserName)
                            .Select(g => new ChartItem(g.Key, g.Sum(r => r.TotalQuantity), FormatQuantity(g.Sum(r => r.TotalQuantity))))
                            .OrderByDescending(x => x.Value),
                        8),
                    Primary),
                new ChartBlock(
                    "Movimentacoes por tipo",
                    "Distribuicao das operacoes consideradas no periodo.",
                    rows.GroupBy(r => TranslateTransactionType(r.TransactionType))
                        .Select(g => new ChartItem(g.Key, g.Sum(r => r.TotalQuantity), FormatQuantity(g.Sum(r => r.TotalQuantity))))
                        .OrderByDescending(x => x.Value)
                        .ToList(),
                    Accent)
            ],
            ["Usuario", "Tipo", "Quantidade", "Movimentacoes"],
            rows.Select(r => new[]
            {
                r.UserName,
                TranslateTransactionType(r.TransactionType),
                FormatQuantity(r.TotalQuantity),
                r.MovementsCount.ToString(Culture)
            }).ToList(),
            $"Total geral: {FormatQuantity(totalQuantity)}"));
    }

    public byte[] GenerateIdleStock(IdleStockReport report)
    {
        var rows = report.Rows;
        var totalStock = rows.Sum(r => r.StockQuantity);
        var neverMoved = rows.Count(r => r.LastMovementAt is null);

        return Generate(new ReportDocument(
            report.GeneratedAtUtc,
            "Materiais sem movimentacao",
            "Estoque parado",
            "Materiais com saldo positivo sem movimentacao recente, candidatos a revisao ou descarte.",
            [
                ("Tipo de material", report.MaterialTypeId?.ToString() ?? "Todos"),
                ("Sem movimentacao desde", FormatDate(report.Since))
            ],
            [
                new Kpi("Materiais parados", rows.Count.ToString(Culture), "Itens sem movimentacao recente", Primary),
                new Kpi("Estoque parado", FormatQuantity(totalStock), "Quantidade somada dos itens", Accent),
                new Kpi("Nunca movimentados", neverMoved.ToString(Culture), "Sem nenhuma transacao registrada", Warning),
                new Kpi("Tipos afetados", rows.Select(r => r.MaterialTypeName).Distinct().Count().ToString(Culture), "Categorias com itens parados", Danger)
            ],
            [
                new ChartBlock(
                    "Maior estoque parado",
                    "Materiais com maior quantidade sem movimentacao.",
                    LimitWithOthers(
                        rows.OrderByDescending(r => r.StockQuantity)
                            .Select(r => new ChartItem(r.MaterialName, r.StockQuantity, $"{FormatQuantity(r.StockQuantity)} {r.Unit}")),
                        8),
                    Warning),
                new ChartBlock(
                    "Parados por tipo de material",
                    "Distribuicao do estoque parado por categoria.",
                    LimitWithOthers(
                        rows.GroupBy(r => r.MaterialTypeName)
                            .Select(g => new ChartItem(g.Key, g.Sum(r => r.StockQuantity), FormatQuantity(g.Sum(r => r.StockQuantity))))
                            .OrderByDescending(x => x.Value),
                        8),
                    Primary)
            ],
            ["Material", "Tipo material", "Local", "Un.", "Estoque atual", "Ultima movimentacao"],
            rows.Select(r => new[]
            {
                r.MaterialName,
                r.MaterialTypeName,
                r.Location,
                r.Unit,
                FormatQuantity(r.StockQuantity),
                r.LastMovementAt.HasValue ? FormatDate(r.LastMovementAt.Value) : "Nunca movimentado"
            }).ToList(),
            $"Materiais listados: {rows.Count}"));
    }

    public byte[] GenerateOrderStatusCycle(OrderStatusCycleReport report)
    {
        var statusCounts = report.StatusCounts;
        var staleOrders = report.StaleOrders;
        var totalOrders = statusCounts.Sum(s => s.Count);

        return Generate(new ReportDocument(
            report.GeneratedAtUtc,
            "Pedidos por status e tempo de ciclo",
            "Gestao de compras e reposicao",
            "Contagem por status, tempo medio de ciclo e pedidos parados alem do limite configurado.",
            [
                ("De", report.From.HasValue ? FormatDate(report.From.Value) : "Inicio"),
                ("Ate", report.To.HasValue ? FormatDate(report.To.Value) : "Agora"),
                ("Dias para considerar parado", report.StaleDays.ToString(Culture))
            ],
            [
                new Kpi("Pedidos", totalOrders.ToString(Culture), "Total no periodo filtrado", Primary),
                new Kpi("Parados", staleOrders.Count.ToString(Culture), $"Alem de {report.StaleDays} dias no mesmo status", Danger),
                new Kpi("Pendente -> Processando", report.AveragePendingToProcessingHours.HasValue ? $"{report.AveragePendingToProcessingHours:0.##}h" : "-", "Tempo medio de ciclo", Accent),
                new Kpi("Processando -> Concluido", report.AverageProcessingToCompletedHours.HasValue ? $"{report.AverageProcessingToCompletedHours:0.##}h" : "-", "Tempo medio de ciclo", Warning)
            ],
            [
                new ChartBlock(
                    "Pedidos por status",
                    "Distribuicao dos pedidos considerados no periodo.",
                    statusCounts
                        .Select(s => new ChartItem(TranslateOrderStatus(s.Status), s.Count, s.Count.ToString(Culture)))
                        .OrderByDescending(x => x.Value)
                        .ToList(),
                    Primary),
                new ChartBlock(
                    "Pedidos parados ha mais tempo",
                    "Maior tempo no status atual entre os pedidos parados.",
                    LimitWithOthers(
                        staleOrders
                            .OrderByDescending(o => o.DaysInStatus)
                            .Select(o => new ChartItem(o.MaterialName, o.DaysInStatus, $"{o.DaysInStatus} dias")),
                        8),
                    Danger)
            ],
            ["Pedido", "Material", "Status", "Desde", "Dias parado"],
            staleOrders.Select(o => new[]
            {
                ShortId(o.OrderId),
                o.MaterialName,
                TranslateOrderStatus(o.Status),
                FormatDate(o.LastTransitionAt),
                o.DaysInStatus.ToString(Culture)
            }).ToList(),
            $"Pedidos parados: {staleOrders.Count}"));
    }

    public byte[] GenerateStockByMaterialType(StockByMaterialTypeReport report)
    {
        var rows = report.Rows;
        var totalInflow = rows.Sum(r => r.InflowQuantity);
        var totalOutflow = rows.Sum(r => r.OutflowQuantity);
        var totalStock = rows.Sum(r => r.CurrentStockQuantity);

        return Generate(new ReportDocument(
            report.GeneratedAtUtc,
            "Resumo agregado por tipo de material",
            "Visao gerencial por categoria",
            "Totais de entrada, saida e saldo atual agrupados por tipo de material.",
            [
                ("De", report.From.HasValue ? FormatDate(report.From.Value) : "Inicio"),
                ("Ate", report.To.HasValue ? FormatDate(report.To.Value) : "Agora")
            ],
            [
                new Kpi("Saldo atual total", FormatQuantity(totalStock), "Soma de todos os tipos", Primary),
                new Kpi("Entradas", FormatQuantity(totalInflow), "Recebimentos e entradas excepcionais", Accent),
                new Kpi("Saidas", FormatQuantity(totalOutflow), "Consumo de projeto e baixas excepcionais", Warning),
                new Kpi("Tipos", rows.Count.ToString(Culture), "Categorias consideradas", Danger)
            ],
            [
                new ChartBlock(
                    "Saldo atual por tipo",
                    "Tipos de material com maior saldo em estoque.",
                    LimitWithOthers(
                        rows.OrderByDescending(r => r.CurrentStockQuantity)
                            .Select(r => new ChartItem(r.MaterialTypeName, r.CurrentStockQuantity, FormatQuantity(r.CurrentStockQuantity))),
                        8),
                    Primary),
                new ChartBlock(
                    "Entrada vs saida por tipo",
                    "Comparativo de movimentacao no periodo filtrado.",
                    LimitWithOthers(
                        rows.OrderByDescending(r => r.InflowQuantity + r.OutflowQuantity)
                            .Select(r => new ChartItem(r.MaterialTypeName, r.InflowQuantity + r.OutflowQuantity, $"E: {FormatQuantity(r.InflowQuantity)} / S: {FormatQuantity(r.OutflowQuantity)}")),
                        8),
                    Accent)
            ],
            ["Tipo de material", "Materiais", "Entradas", "Saidas", "Saldo periodo", "Saldo atual"],
            rows.Select(r => new[]
            {
                r.MaterialTypeName,
                r.MaterialsCount.ToString(Culture),
                FormatQuantity(r.InflowQuantity),
                FormatQuantity(r.OutflowQuantity),
                FormatQuantity(r.NetQuantity),
                FormatQuantity(r.CurrentStockQuantity)
            }).ToList(),
            $"Tipos listados: {rows.Count}"));
    }

    private static byte[] Generate(ReportDocument report)
    {
        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(22);
                page.DefaultTextStyle(text => text.FontSize(8).FontColor(Ink));

                page.Header().Element(container => ComposeHeader(container, report));

                page.Content().PaddingTop(12).Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Element(container => ComposeKpis(container, report.Kpis));
                    column.Item().Element(container => ComposeFilters(container, report.Filters.ToList()));
                    column.Item().Element(container => ComposeCharts(container, report.Charts));

                    if (report.MonthlySummary is not null)
                        column.Item().Element(container => ComposeMonthlySummary(container, report.MonthlySummary));

                    column.Item().Element(container => ComposeTableSection(container, report));
                });

                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, ReportDocument report)
    {
        container
            .Background(Primary)
            .Padding(14)
            .Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Spacing(3);
                    column.Item().Text(report.Title).FontSize(18).Bold().FontColor(White);
                    column.Item().Text(report.Subtitle).FontSize(10).SemiBold().FontColor(PrimarySoft);
                    column.Item().Text(report.Description).FontSize(8).FontColor(PrimarySoft);
                });

                row.ConstantItem(170).AlignRight().Column(column =>
                {
                    column.Spacing(3);
                    column.Item().AlignRight().Text("LabViroMol").FontSize(14).Bold().FontColor(White);
                    column.Item().AlignRight().Text("Relatorio de estoque").FontSize(8).FontColor(PrimarySoft);
                    column.Item().AlignRight().Text($"Gerado em UTC: {FormatDate(report.GeneratedAtUtc)}").FontSize(7).FontColor(PrimarySoft);
                });
            });
    }

    private static void ComposeKpis(IContainer container, IReadOnlyList<Kpi> kpis)
    {
        container.Row(row =>
        {
            row.Spacing(8);

            foreach (var kpi in kpis)
            {
                row.RelativeItem()
                    .Border(0.7f)
                    .BorderColor(Line)
                    .Background(White)
                    .Padding(9)
                    .Column(column =>
                    {
                        column.Spacing(4);
                        column.Item().Row(kpiRow =>
                        {
                            kpiRow.ConstantItem(6).Height(28).Background(kpi.Color);
                            kpiRow.RelativeItem().PaddingLeft(6).Column(kpiColumn =>
                            {
                                kpiColumn.Item().Text(kpi.Label).FontSize(7).FontColor(Muted).SemiBold();
                                kpiColumn.Item().Text(kpi.Value).FontSize(13).Bold().FontColor(Ink);
                            });
                        });
                        column.Item().Text(kpi.Hint).FontSize(7).FontColor(Muted);
                    });
            }
        });
    }

    private static void ComposeFilters(IContainer container, IReadOnlyList<(string Label, string Value)> filters)
    {
        container
            .Border(0.7f)
            .BorderColor(Line)
            .Background(Surface)
            .Padding(8)
            .Column(column =>
            {
                column.Spacing(5);
                column.Item().Text("Filtros aplicados").FontSize(8).SemiBold().FontColor(Primary);
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    foreach (var filter in filters)
                    {
                        table.Cell().Padding(3).Element(cell =>
                        {
                            cell.Background(White)
                                .Border(0.5f)
                                .BorderColor(Line)
                                .Padding(5)
                                .Text(text =>
                                {
                                    text.Span($"{filter.Label}: ").SemiBold().FontColor(Muted);
                                    text.Span(Compact(filter.Value, 44)).FontColor(Ink);
                                });
                        });
                    }
                });
            });
    }

    private static void ComposeCharts(IContainer container, IReadOnlyList<ChartBlock> charts)
    {
        container.Row(row =>
        {
            row.Spacing(10);

            foreach (var chart in charts.Take(2))
            {
                row.RelativeItem().Element(chartContainer => ComposeChart(chartContainer, chart));
            }
        });
    }

    private static void ComposeMonthlySummary(IContainer container, MonthlySummarySection summary)
    {
        container.Column(column =>
        {
            column.Spacing(8);
            column.Item().Text("Resumo mensal").FontSize(11).Bold().FontColor(Ink);
            column.Item().Element(kpiContainer => ComposeKpis(kpiContainer, summary.Kpis));
            column.Item().Element(chartsContainer => ComposeCharts(chartsContainer, summary.Charts));
        });
    }

    private static void ComposeChart(IContainer container, ChartBlock chart)
    {
        var items = chart.Items.Where(i => i.Value > 0).ToList();
        var max = items.Count == 0 ? 1 : items.Max(i => i.Value);

        container
            .Border(0.7f)
            .BorderColor(Line)
            .Background(White)
            .Padding(10)
            .Column(column =>
            {
                column.Spacing(6);
                column.Item().Text(chart.Title).FontSize(10).Bold().FontColor(Ink);
                column.Item().Text(chart.Description).FontSize(7).FontColor(Muted);

                if (items.Count == 0)
                {
                    column.Item().PaddingTop(8).Background(Surface).Padding(10)
                        .Text("Sem dados suficientes para gerar grafico.").FontSize(8).FontColor(Muted);
                    return;
                }

                foreach (var item in items)
                {
                    var percent = max == 0 ? 0 : Math.Max(0.04m, item.Value / max);
                    column.Item().Column(itemColumn =>
                    {
                        itemColumn.Spacing(2);
                        itemColumn.Item().Row(labelRow =>
                        {
                            labelRow.RelativeItem().Text(Compact(item.Label, 46)).FontSize(7).SemiBold().FontColor(Ink);
                            labelRow.ConstantItem(70).AlignRight().Text(item.FormattedValue).FontSize(7).FontColor(Muted);
                        });
                        itemColumn.Item().Row(barRow =>
                        {
                            var filled = Math.Clamp((float)percent, 0.04f, 1f);
                            var remaining = Math.Max(0.01f, 1f - filled);

                            barRow.RelativeItem(filled).Height(8).Background(chart.Color);
                            barRow.RelativeItem(remaining).Height(8).Background(Surface);
                        });
                    });
                }
            });
    }

    private static void ComposeTableSection(IContainer container, ReportDocument report)
    {
        container
            .Border(0.7f)
            .BorderColor(Line)
            .Background(White)
            .Padding(10)
            .Column(column =>
            {
                column.Spacing(7);
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Detalhamento").FontSize(11).Bold().FontColor(Ink);
                    row.ConstantItem(220).AlignRight().Text(report.Summary).FontSize(8).SemiBold().FontColor(Primary);
                });

                if (report.Rows.Count == 0)
                {
                    column.Item().Background(Surface).Padding(14)
                        .Text("Nenhum dado encontrado para os filtros informados.").FontSize(9).FontColor(Muted);
                    return;
                }

                column.Item().Element(tableContainer => ComposeTable(tableContainer, report.Headers, report.Rows, report.WrapColumnIndex));
            });
    }

    private static void ComposeTable(IContainer container, string[] headers, IReadOnlyList<string[]> rows, int? wrapColumnIndex = null)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                foreach (var _ in headers)
                    columns.RelativeColumn();
            });

            table.Header(header =>
            {
                foreach (var title in headers)
                {
                    header.Cell().Element(HeaderCell).Text(title).SemiBold().FontColor(White);
                }
            });

            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                var background = index % 2 == 0 ? White : Surface;

                for (var columnIndex = 0; columnIndex < row.Length; columnIndex++)
                {
                    var value = row[columnIndex];
                    var cellText = columnIndex == wrapColumnIndex ? value : Compact(value, 58);
                    table.Cell().Element(cell => BodyCell(cell, background)).Text(cellText).FontSize(7);
                }
            }
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.PaddingTop(8).Row(row =>
        {
            row.RelativeItem().Text("LabViroMol | Relatorios de estoque").FontSize(7).FontColor(Muted);
            row.ConstantItem(80).AlignRight().Text(text =>
            {
                text.DefaultTextStyle(style => style.FontSize(7).FontColor(Muted));
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        });
    }

    private static IContainer HeaderCell(IContainer container)
    {
        return container
            .Background(Primary)
            .BorderRight(0.4f)
            .BorderColor(PrimarySoft)
            .PaddingVertical(5)
            .PaddingHorizontal(4);
    }

    private static IContainer BodyCell(IContainer container, string background)
    {
        return container
            .Background(background)
            .BorderBottom(0.4f)
            .BorderColor(Line)
            .PaddingVertical(4)
            .PaddingHorizontal(4);
    }

    private static IEnumerable<(string Label, string Value)> BuildCommonFilters(
        DateTime? from,
        DateTime? to,
        Guid? materialId,
        Guid? materialTypeId)
    {
        yield return ("De", from.HasValue ? FormatDate(from.Value) : "Inicio");
        yield return ("Ate", to.HasValue ? FormatDate(to.Value) : "Agora");
        yield return ("Material", materialId?.ToString() ?? "Todos");
        yield return ("Tipo de material", materialTypeId?.ToString() ?? "Todos");
    }

    private static List<ChartItem> LimitWithOthers(IEnumerable<ChartItem> orderedItems, int take)
    {
        var items = orderedItems.ToList();
        if (items.Count <= take)
            return items;

        var visible = items.Take(take).ToList();
        var remaining = items.Skip(take).ToList();
        var remainingSum = remaining.Sum(i => i.Value);
        visible.Add(new ChartItem($"Outros ({remaining.Count} itens)", remainingSum, FormatQuantity(remainingSum)));
        return visible;
    }

    private static string FormatQuantity(decimal value)
    {
        return value.ToString("0.####", Culture);
    }

    private static string FormatDate(DateTime value)
    {
        return value.ToString("yyyy-MM-dd HH:mm:ss", Culture);
    }

    private static string ShortId(Guid id)
    {
        return id.ToString("N")[..8];
    }

    private static string Compact(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
            return value;

        return $"{value[..Math.Max(0, maxLength - 1)]}.";
    }

    private static string TranslateTransactionType(string value)
    {
        return value switch
        {
            "ProjectConsumption" => "Consumo projeto",
            "ExceptionOut" => "Saida excepcional",
            "OrderReceipt" => "Recebimento pedido",
            "ExceptionIn" => "Entrada excepcional",
            _ => value
        };
    }

    private static string TranslateOrderStatus(string value)
    {
        return value switch
        {
            "Pending" => "Pendente",
            "Processing" => "Processando",
            "Completed" => "Concluido",
            "Canceled" => "Cancelado",
            _ => value
        };
    }

    private sealed record ReportDocument(
        DateTime GeneratedAtUtc,
        string Title,
        string Subtitle,
        string Description,
        IEnumerable<(string Label, string Value)> Filters,
        IReadOnlyList<Kpi> Kpis,
        IReadOnlyList<ChartBlock> Charts,
        string[] Headers,
        IReadOnlyList<string[]> Rows,
        string Summary,
        int? WrapColumnIndex = null,
        MonthlySummarySection? MonthlySummary = null);

    private sealed record Kpi(string Label, string Value, string Hint, string Color);

    private sealed record MonthlySummarySection(IReadOnlyList<Kpi> Kpis, IReadOnlyList<ChartBlock> Charts);

    private sealed record ChartBlock(
        string Title,
        string Description,
        IReadOnlyList<ChartItem> Items,
        string Color);

    private sealed record ChartItem(string Label, decimal Value, string FormattedValue);
}
