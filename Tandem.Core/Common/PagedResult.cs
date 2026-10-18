namespace Tandem.Core.Common;

// One page of a list, plus what the client needs to draw the paging controls
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];

    public int Page { get; init; }

    public int PageSize { get; init; }

    // Total number of matching rows in the database, not just on this page
    public int TotalCount { get; init; }

    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
