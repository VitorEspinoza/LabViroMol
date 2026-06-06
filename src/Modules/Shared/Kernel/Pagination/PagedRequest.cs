namespace LabViroMol.Modules.Shared.Kernel.Pagination;

public record PagedRequest(
    int Page = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDirection = "asc");
