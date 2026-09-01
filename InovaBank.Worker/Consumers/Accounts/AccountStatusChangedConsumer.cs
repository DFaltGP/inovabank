using InovaBank.Domain.Events.Accounts;
using InovaBank.Infrastructure.Persistence.MongoDb;
using MassTransit;
using MongoDB.Driver;

namespace InovaBank.Worker.Consumers.Accounts;

public sealed class AccountStatusChangedConsumer(MongoContext _mongoContext) : IConsumer<AccountStatusChangedEvent>
{
    public async Task Consume(ConsumeContext<AccountStatusChangedEvent> context)
    {
        var @event = context.Message;
        var balances = _mongoContext.GetCollection<dynamic>("Balances");

        var filter = Builders<dynamic>.Filter.And(
            Builders<dynamic>.Filter.Eq("AccountId", @event.AccountId),
            Builders<dynamic>.Filter.Lte("UpdatedAt", @event.UpdatedAt)
        );

        var update = Builders<dynamic>.Update
            .Set("Status", @event.Status)
            .Set("UpdatedAt", @event.UpdatedAt);

        await balances.UpdateOneAsync(filter, update);
    }
}
