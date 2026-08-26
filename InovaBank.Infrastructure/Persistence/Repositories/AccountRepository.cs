using InovaBank.Domain.Entities;
using InovaBank.Domain.Interfaces;
using InovaBank.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace InovaBank.Infrastructure.Persistence.Repositories;

public sealed class AccountRepository(InovaBankDbContext context) : IAccountRepository
{
    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken ct) =>
        await context.Accounts.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<Account?> GetByIdForUpdateAsync(Guid id, CancellationToken ct)
    {
        return await context.Accounts
            .FromSqlInterpolated($"SELECT * FROM \"Accounts\" WHERE \"Id\" = {id} FOR UPDATE")
            .Include(a => a.Transactions)
            .FirstOrDefaultAsync(ct);
    }

    public async Task AddAsync(Account account, CancellationToken ct) =>
        await context.Accounts.AddAsync(account, ct);

    public async Task<bool> ExistsCnpjAsync(Cnpj cnpj, CancellationToken ct) =>
        await context.Accounts.AnyAsync(a => a.Cnpj == cnpj, ct);

    public async Task UpdateAsync(Account account, CancellationToken ct)
    {
        context.Accounts.Update(account);
        await Task.CompletedTask;
    }

    public async Task<Account?> GetByCnpjAsync(Cnpj cnpj, CancellationToken ct) =>
        await context.Accounts.FirstOrDefaultAsync(a => a.Cnpj == cnpj, ct);
}
