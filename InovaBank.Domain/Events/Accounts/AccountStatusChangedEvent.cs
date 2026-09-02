namespace InovaBank.Domain.Events.Accounts;

public sealed record AccountStatusChangedEvent(
    Guid AccountId,
    string Status,
    DateTime UpdatedAt);
