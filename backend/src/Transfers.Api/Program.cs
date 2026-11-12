using System.Text.Json.Serialization;
using Transfers.Api.Http;
using Transfers.Application;
using Transfers.Infrastructure;
using Transfers.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks().AddDbContextCheck<TransfersDbContext>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.SwaggerDoc("v1", new()
{
    Title = "Transferências Financeiras API",
    Version = "v1",
    Description = "Transferências imediatas e agendadas entre contas, com cheque especial e limites por hora."
}));

var app = builder.Build();

switch (args.FirstOrDefault())
{
    case "migrate":
        await app.Services.MigrateDatabaseAsync();
        return;
    case "seed":
        await app.Services.SeedDatabaseAsync();
        return;
}

await app.Services.InitializeDatabaseAsync();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSwagger();
app.UseSwaggerUI(options => options.DocumentTitle = "Transferências Financeiras API");
app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();

public partial class Program;
