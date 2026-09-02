using InovaBank.Domain.Enums;
using InovaBank.Domain.Events.Accounts;
using InovaBank.Infrastructure.Persistence.MongoDb;
using MassTransit;
using MongoDB.Driver;

namespace InovaBank.Worker.Consumers.Accounts;

public sealed class AccountClosedConsumer(MongoContext _mongoContext) : IConsumer<AccountClosedEvent>
{
    public async Task Consume(ConsumeContext<AccountClosedEvent> context)
    {
        var @event = context.Message;
        var accounts = _mongoContext.GetCollection<dynamic>("Accounts");

        var filter = Builders<dynamic>.Filter.And(
            Builders<dynamic>.Filter.Eq("AccountId", @event.AccountId),
            Builders<dynamic>.Filter.Lte("UpdatedAt", @event.ClosedAt)
        );

        var update = Builders<dynamic>.Update
            .Set("Status", AccountStatus.Encerrada.ToString())
            .Set("UpdatedAt", @event.ClosedAt);

        await accounts.UpdateOneAsync(filter, update);
    }
}
