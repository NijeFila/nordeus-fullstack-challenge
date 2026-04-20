using NordeusChallenge.Api.Services;

namespace NordeusChallenge.Api.Endpoints;

public static class RunEndpoints
{
    public static void MapRunEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/run/config", (RunConfigService service) => Results.Ok(service.GetRunConfig()))
           .WithName("GetRunConfig");
    }
}
