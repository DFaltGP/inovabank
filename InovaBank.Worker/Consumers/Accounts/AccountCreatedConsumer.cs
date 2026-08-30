using InovaBank.Domain.Events.Accounts;
using InovaBank.Infrastructure.Persistence.MongoDb;
using MassTransit;
using MongoDB.Driver;

namespace InovaBank.Worker.Consumers.Accounts;

public sealed class AccountCreatedConsumer(MongoContext _mongoContext) : IConsumer<AccountCreatedEvent>
{
    public async Task Consume(ConsumeContext<AccountCreatedEvent> context)
    {
        var @event = context.Message;
        var accounts = _mongoContext.GetCollection<dynamic>("Accounts");

        var filter = Builders<dynamic>.Filter.Eq("AccountId", @event.AccountId);
        var update = Builders<dynamic>.Update
            .SetOnInsert("AccountId", @event.AccountId)
            .SetOnInsert("Cnpj", @event.Cnpj)
            .SetOnInsert("RazaoSocial", @event.RazaoSocial)
            .SetOnInsert("Agencia", @event.Agencia)
            .SetOnInsert("ImagemDocumentoPath", @event.ImagemDocumentoPath)
            .SetOnInsert("Balance", 0m)
            .SetOnInsert("CreatedAt", @event.CreatedAt)
            .Set("Status", "Ativa")
            .Set("UpdatedAt", @event.CreatedAt);

        await accounts.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
    }
}
