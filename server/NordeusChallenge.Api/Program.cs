using System.Text.Json;
using System.Text.Json.Serialization;
using NordeusChallenge.Api.Endpoints;
using NordeusChallenge.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddSingleton<RunConfigService>();
builder.Services.AddSingleton<BattleService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapRunEndpoints();
app.MapBattleEndpoints();

app.Run();
