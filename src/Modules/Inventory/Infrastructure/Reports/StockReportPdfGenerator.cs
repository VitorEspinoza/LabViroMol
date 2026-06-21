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
    private const string AccentSoft = "#CCFBF1";
    private const string Warning = "#B45309";
    private const string WarningSoft = "#FEF3C7";
    private const string Danger = "#B91C1C";
    private const string DangerSoft = "#FEE2E2";
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
                    rows.GroupBy(r => r.ProjectTitle)
                        .Select(g => new ChartItem(g.Key, g.Sum(r => r.TotalQuantity), FormatQuantity(g.Sum(r => r.TotalQuantity))))
                        .OrderByDescending(x => x.Value)
                        .Take(6)
                        .ToList(),
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
                    rows.GroupBy(r => $"{r.Year:D4}-{r.Month:D2}")
                        .Select(g => new ChartItem(g.Key, g.Sum(r => r.TotalQuantity), FormatQuantity(g.Sum(r => r.TotalQuantity))))
                        .OrderBy(x => x.Label)
                        .Take(12)
                        .ToList(),
                    Accent),
                new ChartBlock(
                    "Top materiais",
                    "Materiais com maior quantidade de saida no periodo.",
                    rows.GroupBy(r => r.MaterialName)
                        .Select(g => new ChartItem(g.Key, g.Sum(r => r.TotalQuantity), FormatQuantity(g.Sum(r => r.TotalQuantity))))
                        .OrderByDescending(x => x.Value)
                        .Take(6)
                        .ToList(),
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
                    rows.OrderByDescending(r => r.TotalQuantity)
                        .Take(8)
                        .Select(r => new ChartItem(r.MaterialName, r.TotalQuantity, $"{FormatQuantity(r.TotalQuantity)} {r.Unit}"))
                        .ToList(),
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
                    rows.GroupBy(r => $"{r.Year:D4}-{r.Month:D2}")
                        .Select(g => new ChartItem(g.Key, g.Sum(r => r.TotalQuantity), FormatQuantity(g.Sum(r => r.TotalQuantity))))
                        .OrderBy(x => x.Label)
                        .Take(12)
                        .ToList(),
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
                    rows.OrderBy(r => r.Difference)
                        .Take(8)
                        .Select(r => new ChartItem(r.MaterialName, Math.Abs(Math.Min(r.Difference, 0)), $"Dif. {FormatQuantity(r.Difference)}"))
                        .ToList(),
                    Danger),
                new ChartBlock(
                    "Estoque atual vs minimo",
                    "Razao entre saldo atual e estoque minimo para materiais mais criticos.",
                    rows.OrderBy(r => r.StockQuantity - r.MinStock)
                        .Take(8)
                        .Select(r =>
                        {
                            var ratio = r.MinStock == 0 ? 100 : Math.Round(r.StockQuantity / r.MinStock * 100, 2);
                            return new ChartItem(r.MaterialName, Math.Min(ratio, 100), $"{FormatQuantity(r.StockQuantity)} / {FormatQuantity(r.MinStock)}");
                        })
                        .ToList(),
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

        return Generate(new ReportDocument(
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
                    rows.GroupBy(r => r.MaterialName)
                        .Select(g => new ChartItem(g.Key, g.Sum(r => r.Quantity), FormatQuantity(g.Sum(r => r.Quantity))))
                        .OrderByDescending(x => x.Value)
                        .Take(6)
                        .ToList(),
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
                ShortId(r.TransactedByUserId),
                r.ProjectId.HasValue ? ShortId(r.ProjectId.Value) : "-",
                r.OrderId.HasValue ? ShortId(r.OrderId.Value) : "-",
                r.Justification ?? "-"
            }).ToList(),
            $"Movimentacoes listadas: {rows.Count}"));
    }

    public byte[] GenerateManualStockAdjustments(ManualStockAdjustmentsReport report)
    {
        var rows = report.Rows;
        var totalQuantity = rows.Sum(r => r.Quantity);

        return Generate(new ReportDocument(
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
                    rows.GroupBy(r => r.MaterialName)
                        .Select(g => new ChartItem(g.Key, g.Sum(r => r.Quantity), FormatQuantity(g.Sum(r => r.Quantity))))
                        .OrderByDescending(x => x.Value)
                        .Take(6)
                        .ToList(),
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
                ShortId(r.TransactedByUserId),
                r.Justification ?? "-"
            }).ToList(),
            $"Ajustes listados: {rows.Count}"));
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
                    column.Item().AlignRight().Text($"Gerado em UTC: {FormatDate(DateTime.UtcNow)}").FontSize(7).FontColor(PrimarySoft);
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

                column.Item().Element(tableContainer => ComposeTable(tableContainer, report.Headers, report.Rows));
            });
    }

    private static void ComposeTable(IContainer container, string[] headers, IReadOnlyList<string[]> rows)
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

                foreach (var value in row)
                {
                    table.Cell().Element(cell => BodyCell(cell, background)).Text(Compact(value, 58)).FontSize(7);
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

    private sealed record ReportDocument(
        string Title,
        string Subtitle,
        string Description,
        IEnumerable<(string Label, string Value)> Filters,
        IReadOnlyList<Kpi> Kpis,
        IReadOnlyList<ChartBlock> Charts,
        string[] Headers,
        IReadOnlyList<string[]> Rows,
        string Summary);

    private sealed record Kpi(string Label, string Value, string Hint, string Color);

    private sealed record ChartBlock(
        string Title,
        string Description,
        IReadOnlyList<ChartItem> Items,
        string Color);

    private sealed record ChartItem(string Label, decimal Value, string FormattedValue);
}
