namespace Content.Server._Moffstation.Access.Systems;

[ByRefEvent]
internal readonly record struct IdCardChangedEvent(string? FullName, string? JobTitle);
