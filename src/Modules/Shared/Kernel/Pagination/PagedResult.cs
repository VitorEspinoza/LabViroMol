namespace LabViroMol.Modules.Shared.Kernel.Pagination;

public static class PagedResult
{
    public static PagedResponse<T> From<T>(IEnumerable<T> source, int pageNumber, int pageSize)
    {
        var list = source.ToList();
        int total = list.Count;
        var items = list
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        return new PagedResponse<T>(items, pageNumber, pageSize, total);
    }

    public static PagedResponse<T> Create<T>(IReadOnlyCollection<T> items, int pageNumber, int pageSize, int totalCount)
        => new(items, pageNumber, pageSize, totalCount);
}
