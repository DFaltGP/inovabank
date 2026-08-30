namespace InovaBank.Domain.Events.Accounts;

public sealed record AccountCreatedEvent(
    Guid AccountId,
    string Cnpj,
    string Agencia,
    string RazaoSocial,
    string ImagemDocumentoPath,
    DateTime CreatedAt);
