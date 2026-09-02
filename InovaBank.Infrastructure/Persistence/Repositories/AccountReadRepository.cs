using InovaBank.Domain.Common;
using InovaBank.Domain.Enums;
using InovaBank.Domain.Interfaces;
using InovaBank.Domain.Primitives;
using InovaBank.Domain.Queries.ReadModels;
using InovaBank.Infrastructure.Persistence.MongoDb;
using MongoDB.Bson;
using MongoDB.Driver;

namespace InovaBank.Infrastructure.Persistence.Repositories;

public sealed class AccountReadRepository(MongoContext _context) : IAccountReadRepository
{
    private readonly IMongoCollection<BsonDocument> _accounts = _context.GetCollection<BsonDocument>("Accounts");
    private readonly IMongoCollection<BsonDocument> _statements = _context.GetCollection<BsonDocument>("Statements");

    public async Task<AccountResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("AccountId", id);
        var doc = await _accounts.Find(filter).FirstOrDefaultAsync(ct);

        return doc is null ? null : MapToResponse(doc);
    }

    public async Task<AccountResponse?> GetByCnpjAsync(string cnpj, CancellationToken ct = default)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("Cnpj", cnpj);
        var doc = await _accounts.Find(filter).FirstOrDefaultAsync(ct);

        return doc is null ? null : MapToResponse(doc);
    }

    public async Task<BalanceReadModel?> GetBalanceAsync(Guid accountId, CancellationToken ct = default)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("AccountId", accountId);

        var projection = Builders<BsonDocument>.Projection
            .Include("AccountId")
            .Include("Balance");

        var doc = await _accounts.Find(filter)
            .Project(projection)
            .FirstOrDefaultAsync(ct);

        if (doc is null)
            return null;

        return new BalanceReadModel(
            accountId,
            doc["Balance"].ToDecimal());
    }

    public async Task<PagedResult<StatementReadModel>> GetStatementAsync(
        Guid accountId, DateTime? start, DateTime? end, string? type, int skip, int take, CancellationToken ct = default)
    {
        var builder = Builders<BsonDocument>.Filter;
        var filter = builder.Eq("AccountId", accountId);

        if (start.HasValue) filter &= builder.Gte("CreatedAt", start.Value);
        if (end.HasValue) filter &= builder.Lte("CreatedAt", end.Value);
        if (!string.IsNullOrEmpty(type) && Enum.TryParse<StatementType>(type, true, out var parsedType))
        {
            switch (parsedType)
            {
                case StatementType.Transferencia:
                    filter &= builder.In("Type",
                    [
                        "TransferenciaRecebida",
                        "TransferenciaEnviada"
                    ]);
                    break;

                case StatementType.Deposito:
                case StatementType.Saque:
                    filter &= builder.Eq("Type", parsedType.ToString());
                    break;
            }
        }

        var totalCount = await _statements.CountDocumentsAsync(filter, cancellationToken: ct);

        var docs = await _statements.Find(filter)
            .SortByDescending(x => x["CreatedAt"])
            .Skip(skip)
            .Limit(take)
            .ToListAsync(ct);

        var items = docs.Select(d => new StatementReadModel(
            d["TransactionId"].AsGuid,
            d["Amount"].ToDecimal(),
            d["Type"].AsString,
            d["Description"].AsString,
            d["CreatedAt"].ToUniversalTime()));

        return new PagedResult<StatementReadModel>(items, (skip / take) + 1, take, totalCount);
    }

    private static AccountResponse MapToResponse(BsonDocument doc) =>
        new(
            doc["AccountId"].AsGuid,
            doc["Cnpj"].AsString,
            doc["RazaoSocial"].AsString,
            doc["Agencia"].AsString,
            doc["Balance"].ToDecimal(),
            doc["Status"].AsString,
            doc["ImagemDocumentoPath"].AsString
        );
}
