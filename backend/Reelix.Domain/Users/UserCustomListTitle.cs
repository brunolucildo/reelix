namespace Reelix.Domain.Users;

public sealed class UserCustomListTitle
{
    private UserCustomListTitle()
    {
    }

    private UserCustomListTitle(
        Guid listId,
        Guid titleId,
        string contentType,
        int position,
        DateTimeOffset addedAt)
    {
        ListId = listId;
        TitleId = titleId;
        ContentType = contentType;
        Position = position;
        AddedAt = addedAt;
    }

    public Guid ListId { get; private set; }

    public Guid TitleId { get; private set; }

    public string ContentType { get; private set; } = string.Empty;

    public int Position { get; private set; }

    public DateTimeOffset AddedAt { get; private set; }

    public static UserCustomListTitle Create(
        Guid listId,
        Guid titleId,
        string contentType,
        int position,
        DateTimeOffset? addedAt = null)
    {
        if (listId == Guid.Empty || titleId == Guid.Empty ||
            contentType is not ("movie" or "series") || position < 0)
        {
            throw new ArgumentException("Informe lista, título, tipo de conteúdo e posição válidos.");
        }

        return new UserCustomListTitle(
            listId,
            titleId,
            contentType,
            position,
            addedAt ?? DateTimeOffset.UtcNow);
    }

    public void MoveTo(int position)
    {
        if (position < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(position), "A posição não pode ser negativa.");
        }

        Position = position;
    }
}
