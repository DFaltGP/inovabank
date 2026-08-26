using InovaBank.Domain.Entities;
using InovaBank.Domain.ValueObjects;

namespace InovaBank.Domain.Interfaces;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Account?> GetByIdForUpdateAsync(Guid id, CancellationToken ct)
    Task<Account?> GetByCnpjAsync(Cnpj cnpj, CancellationToken ct);
    Task AddAsync(Account account, CancellationToken ct);
    Task UpdateAsync(Account account, CancellationToken ct);
    Task<bool> ExistsCnpjAsync(Cnpj cnpj, CancellationToken ct);
}
