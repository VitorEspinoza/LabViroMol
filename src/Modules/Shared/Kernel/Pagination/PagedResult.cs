namespace LabViroMol.Modules.Shared.Kernel.Pagination;

public static class PagedResult
{
    public static PagedResponse<T> From<T>(IEnumerable<T> source, int page, int pageSize)
    {
        var list = source.ToList();
        int total = list.Count;
        var items = list
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        return new PagedResponse<T>(items, page, pageSize, total);
    }
}
