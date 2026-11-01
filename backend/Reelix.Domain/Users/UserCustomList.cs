namespace Reelix.Domain.Users;

public sealed class UserCustomList
{
    private UserCustomList()
    {
    }

    private UserCustomList(
        Guid id,
        Guid accountId,
        string name,
        string? description,
        DateTimeOffset createdAt)
    {
        Id = id;
        AccountId = accountId;
        Name = name;
        Description = description;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid AccountId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static UserCustomList Create(
        Guid accountId,
        string name,
        string? description,
        DateTimeOffset? createdAt = null)
    {
        if (accountId == Guid.Empty)
        {
            throw new ArgumentException("Informe uma conta válida.", nameof(accountId));
        }

        var (normalizedName, normalizedDescription) = Normalize(name, description);
        return new UserCustomList(
            Guid.NewGuid(),
            accountId,
            normalizedName,
            normalizedDescription,
            createdAt ?? DateTimeOffset.UtcNow);
    }

    public void Update(string name, string? description, DateTimeOffset? updatedAt = null)
    {
        var (normalizedName, normalizedDescription) = Normalize(name, description);
        Name = normalizedName;
        Description = normalizedDescription;
        UpdatedAt = updatedAt ?? DateTimeOffset.UtcNow;
    }

    private static (string Name, string? Description) Normalize(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 80)
        {
            throw new ArgumentException("O nome da lista deve ter entre 1 e 80 caracteres.", nameof(name));
        }

        var normalizedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (normalizedDescription?.Length > 500)
        {
            throw new ArgumentException("A descrição da lista deve ter no máximo 500 caracteres.", nameof(description));
        }

        return (name.Trim(), normalizedDescription);
    }
}
