namespace InovaBank.Domain.Common;

public record AccountResponse(
    Guid Id,
    string Cnpj,
    string RazaoSocial,
    string Agencia,
    decimal Balance,
    string Status,
    string ImagemDocumentoPath);
