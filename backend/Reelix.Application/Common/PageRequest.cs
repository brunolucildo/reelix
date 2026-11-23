namespace Reelix.Application.Common;

/// <summary>
/// A validated page request. Every paginated flow validates through <see cref="Validate"/> (or
/// <see cref="ValidatePage"/> for flows without a page size) with its named <see cref="PagePolicy"/>.
/// </summary>
public readonly record struct PageRequest(int Page, int PageSize)
{
    /// <exception cref="ArgumentException">The page or page size is outside the policy limits.</exception>
    public static PageRequest Validate(int page, int pageSize, PagePolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        if (policy.Check(page, pageSize) is { } violation)
        {
            throw violation;
        }

        return new PageRequest(page, pageSize);
    }

    /// <exception cref="ArgumentException">The page is outside the policy limits.</exception>
    public static int ValidatePage(int page, PagePolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        if (policy.Check(page, pageSize: null) is { } violation)
        {
            throw violation;
        }

        return page;
    }
}
