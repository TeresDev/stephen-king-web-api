namespace Investec.SK.Application.Queries;

/// <summary>
/// A validated paging and sorting request. Paging is measured in rows. Every violation is
/// reported, not just the first, so a caller fixes their whole query in one round trip.
/// </summary>
public sealed record PagingRequest(
    int Page,
    int PageSize,
    SortKey Sort,
    SortDirection Direction,
    int? Year)
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const int MinYear = 1900;
    public const int MaxYear = 2100;

    public string SortName => Sort.ToWireName();

    public string DirectionName => Direction == SortDirection.Desc ? "desc" : "asc";

    /// <summary>
    /// Rows to skip before this page starts. Computed in <see cref="long"/> and clamped: an
    /// enormous <c>page</c> multiplied by <c>pageSize</c> would overflow a 32-bit product and
    /// wrap negative, which <see cref="Enumerable.Skip{T}"/> treats as zero — silently returning
    /// page 1's rows instead of the empty page the caller actually asked for. Once the true skip
    /// exceeds <see cref="int.MaxValue"/> the page is beyond any possible result set, so clamping
    /// to <see cref="int.MaxValue"/> skips everything.
    /// </summary>
    public int Skip
    {
        get
        {
            var skip = (long)(Page - 1) * PageSize;
            return skip > int.MaxValue ? int.MaxValue : (int)skip;
        }
    }

    public static bool TryCreate(
        int? page,
        int? pageSize,
        string? sort,
        string? dir,
        int? year,
        IReadOnlyCollection<SortKey> allowedKeys,
        out PagingRequest request,
        out IReadOnlyList<ValidationError> errors)
    {
        var found = new List<ValidationError>();

        var resolvedPage = page ?? DefaultPage;
        if (resolvedPage < 1)
        {
            found.Add(new ValidationError("page", "must be greater than or equal to 1"));
        }

        var resolvedPageSize = pageSize ?? DefaultPageSize;
        if (resolvedPageSize < 1 || resolvedPageSize > MaxPageSize)
        {
            found.Add(new ValidationError("pageSize", $"must be between 1 and {MaxPageSize}"));
        }

        var resolvedSort = allowedKeys.First();
        if (!string.IsNullOrWhiteSpace(sort))
        {
            if (SortKeyNames.TryParse(sort, out var parsed) && allowedKeys.Contains(parsed))
            {
                resolvedSort = parsed;
            }
            else
            {
                var allowed = string.Join(", ", allowedKeys.Select(k => k.ToWireName()));
                found.Add(new ValidationError("sort", $"must be one of: {allowed}"));
            }
        }

        var resolvedDirection = resolvedSort == SortKey.Pages ? SortDirection.Desc : SortDirection.Asc;
        if (!string.IsNullOrWhiteSpace(dir))
        {
            switch (dir.Trim().ToLowerInvariant())
            {
                case "asc": resolvedDirection = SortDirection.Asc; break;
                case "desc": resolvedDirection = SortDirection.Desc; break;
                default: found.Add(new ValidationError("dir", "must be one of: asc, desc")); break;
            }
        }

        if (year is not null && (year < MinYear || year > MaxYear))
        {
            found.Add(new ValidationError("year", $"must be between {MinYear} and {MaxYear}"));
        }

        errors = found;
        request = new PagingRequest(resolvedPage, resolvedPageSize, resolvedSort, resolvedDirection, year);
        return found.Count == 0;
    }
}
