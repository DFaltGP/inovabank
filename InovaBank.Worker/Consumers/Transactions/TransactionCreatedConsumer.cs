using InovaBank.Domain.Events.Transactions;
using InovaBank.Infrastructure.Persistence.MongoDb;
using MassTransit;
using MongoDB.Driver;

namespace InovaBank.Worker.Consumers.Transactions;

public sealed class TransactionCreatedConsumer(MongoContext _mongoContext) : IConsumer<TransactionCreatedEvent>
{
    public async Task Consume(ConsumeContext<TransactionCreatedEvent> context)
    {
        var @event = context.Message;

        var statements = _mongoContext.GetCollection<dynamic>("Statements");
        var accounts = _mongoContext.GetCollection<dynamic>("Accounts");

        await statements.InsertOneAsync(new
        {
            @event.TransactionId,
            @event.AccountId,
            @event.Amount,
            @event.Type,
            @event.Description,
            @event.CreatedAt
        });

        var filter = Builders<dynamic>.Filter.Eq("AccountId", @event.AccountId);
        var update = Builders<dynamic>.Update
            .Inc("Balance", @event.Amount)
            .Set("UpdatedAt", @event.CreatedAt);

        await accounts.UpdateOneAsync(filter, update);
    }
}
