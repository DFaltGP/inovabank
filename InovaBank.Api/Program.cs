using FluentValidation;
using InovaBank.Application.Behaviors;
using InovaBank.Infrastructure;
using InovaBank.Infrastructure.Persistence;
using Microsoft.OpenApi;
using Microsoft.EntityFrameworkCore;
using Serilog;
using InovaBank.Api.Middlewares;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using InovaBank.Infrastructure.Persistence.MongoDb;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "InovaBank.Api")
        .WriteTo.Console();
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddValidatorsFromAssembly(typeof(InovaBank.Application.AssemblyReference).Assembly);

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(InovaBank.Application.AssemblyReference).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "InovaBank API",
        Version = "v1",
        Description = "Plataforma Bancária com CQRS e Event-Driven Architecture."
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

builder.Services.Configure<MassTransitHostOptions>(options =>
{
    options.WaitUntilStarted = true;
});

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddDbContextCheck<InovaBankDbContext>(name: "postgres_ef", failureStatus: HealthStatus.Unhealthy, tags: ["ready"])
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!, name: "redis", failureStatus: HealthStatus.Degraded, tags: ["ready"])
    .AddMongoDb(clientFactory: sp => sp.GetRequiredService<MongoContext>().Client, name: "mongodb", failureStatus: HealthStatus.Degraded, tags: ["ready"]);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var dbContext = services.GetRequiredService<InovaBankDbContext>();
        logger.LogInformation("Verificando e aplicando migrações pendentes no banco de dados...");
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Migrações aplicadas com sucesso.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Ocorreu um erro ao aplicar as migrações no banco de dados.");
        throw;
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseSerilogRequestLogging();

app.UseExceptionHandler();

app.MapControllers();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();
