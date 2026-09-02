namespace InovaBank.Domain.Events.Accounts;

public sealed record AccountClosedEvent(
    Guid AccountId,
    DateTime ClosedAt);
