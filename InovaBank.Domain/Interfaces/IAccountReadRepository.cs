using InovaBank.Domain.Common;
using InovaBank.Domain.Primitives;
using InovaBank.Domain.Queries.ReadModels;

namespace InovaBank.Domain.Interfaces;

public interface IAccountReadRepository
{
    Task<AccountResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AccountResponse?> GetByCnpjAsync(string cnpj, CancellationToken ct = default);
    Task<BalanceReadModel?> GetBalanceAsync(Guid accountId, CancellationToken ct = default);
    Task<PagedResult<StatementReadModel>> GetStatementAsync(Guid accountId, DateTime? start, DateTime? end, string? type, int skip, int take, CancellationToken ct = default);
}
