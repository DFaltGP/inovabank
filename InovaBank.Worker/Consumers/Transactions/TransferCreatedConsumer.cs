using InovaBank.Domain.Events.Transactions;
using InovaBank.Infrastructure.Persistence.MongoDb;
using MassTransit;
using MongoDB.Driver;

namespace InovaBank.Worker.Consumers.Transactions;

public sealed class TransferCreatedConsumer(MongoContext _mongoContext) : IConsumer<TransferCreatedEvent>
{
    public async Task Consume(ConsumeContext<TransferCreatedEvent> context)
    {
        var @event = context.Message;
        var statements = _mongoContext.GetCollection<dynamic>("Statements");
        var accounts = _mongoContext.GetCollection<dynamic>("Accounts");

        var debitDoc = new
        {
            @event.TransactionId,
            AccountId = @event.SourceAccountId,
            Amount = -@event.Amount,
            Type = "TransferenciaEnviada",
            @event.Description,
            @event.CreatedAt
        };

        var creditDoc = new
        {
            TransactionId = Guid.NewGuid(),
            AccountId = @event.DestinationAccountId,
            @event.Amount,
            Type = "TransferenciaRecebida",
            @event.Description,
            @event.CreatedAt
        };

        await statements.InsertManyAsync([debitDoc, creditDoc]);

        await UpdateAccountBalance(@event.SourceAccountId, -@event.Amount, @event.CreatedAt, accounts);
        await UpdateAccountBalance(@event.DestinationAccountId, @event.Amount, @event.CreatedAt, accounts);
    }

    private static Task UpdateAccountBalance(Guid accountId, decimal amount, DateTime updatedAt, IMongoCollection<dynamic> collection) =>
        collection.UpdateOneAsync(
            Builders<dynamic>.Filter.Eq("AccountId", accountId),
            Builders<dynamic>.Update
                .Inc("Balance", amount)
                .Set("UpdatedAt", updatedAt));
}
