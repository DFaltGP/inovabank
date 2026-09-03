using InovaBank.Domain.Interfaces;
using InovaBank.Infrastructure.Persistence;
using InovaBank.Infrastructure.Persistence.MongoDb;
using InovaBank.Infrastructure.Persistence.Repositories;
using InovaBank.Infrastructure.Services.Cache;
using InovaBank.Infrastructure.Services.ReceitaWs;
using InovaBank.Infrastructure.Services.Storage;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using StackExchange.Redis;

namespace InovaBank.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<InovaBankDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres")));

        services.AddMassTransit(x =>
        {
            x.AddEntityFrameworkOutbox<InovaBankDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
                o.DisableInboxCleanupService();
            });

            x.AddConfigureEndpointsCallback((context, name, cfg) =>
            {
                cfg.UseEntityFrameworkOutbox<InovaBankDbContext>(context);
            });

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(configuration.GetConnectionString("RabbitMq"));
                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "InovaBank:";
        });

        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")!));

        services.AddScoped<ICacheService, RedisCacheService>();

        services.AddHttpClient<IReceitaWsService, ReceitaWsService>(client =>
        {
            client.BaseAddress = new Uri("https://receitaws.com.br/v1/cnpj/");
        })
        .AddPolicyHandler(
            HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutRejectedException>()
                .Or<TaskCanceledException>()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 3,
                    durationOfBreak: TimeSpan.FromSeconds(15))
        )
        .AddPolicyHandler(
            HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromSeconds(1))
        );

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<InovaBankDbContext>());

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IAccountReadRepository, AccountReadRepository>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        services.AddSingleton<MongoContext>();

        return services;
    }
}
