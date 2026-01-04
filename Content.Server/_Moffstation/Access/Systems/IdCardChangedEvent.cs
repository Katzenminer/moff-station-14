namespace Content.Server._Moffstation.Access.Systems;

public sealed class IdCardChangedEvent(string? fullName, string? jobTitle) : EntityEventArgs
{
    public string? NewFullName { get; } = fullName;
    public string? NewJobTitle { get; } = jobTitle;
}

