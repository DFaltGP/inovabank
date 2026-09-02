namespace InovaBank.Domain.Common;

public static class AccountMapper
{
    public static AccountResponse ToResponse(this Domain.Entities.Account a) =>
        new(a.Id, a.Cnpj, a.RazaoSocial, a.Agencia, a.Balance, a.Status.ToString(), a.ImagemDocumentoPath);
}
