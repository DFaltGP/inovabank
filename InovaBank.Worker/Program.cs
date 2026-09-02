using InovaBank.Infrastructure.Persistence.MongoDb;
using InovaBank.Worker.Consumers.Transactions;
using InovaBank.Worker.Consumers.Accounts;
using MassTransit;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<MongoContext>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<AccountCreatedConsumer>();
    x.AddConsumer<AccountClosedConsumer>();
    x.AddConsumer<AccountStatusChangedConsumer>();

    x.AddConsumer<TransactionCreatedConsumer>();
    x.AddConsumer<TransferCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMq"));
        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
host.Run();
