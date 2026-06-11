namespace LabViroMol.Modules.Shared.Kernel.Pagination;

public sealed record PagedResponse<T>(
    IReadOnlyCollection<T> Data,
    int CurrentPage,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => CurrentPage < TotalPages;
    public bool HasPreviousPage => CurrentPage > 1;
}
