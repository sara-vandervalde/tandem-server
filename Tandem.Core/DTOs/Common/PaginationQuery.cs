using System.ComponentModel.DataAnnotations;

namespace Tandem.Core.DTOs.Common;

// Page and page size from the query string (?page=2&pageSize=20). Reused by every paged list
public class PaginationQuery
{
    [Range(1, int.MaxValue, ErrorMessage = "מספר העמוד חייב להיות 1 ומעלה")]
    public int Page { get; init; } = 1;

    [Range(1, 100, ErrorMessage = "גודל העמוד חייב להיות בין 1 ל-100")]
    public int PageSize { get; init; } = 20;
}
