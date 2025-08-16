using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Olve.Engine3D.DebugServer.Commands;
using Olve.Logging;
using Olve.MinimalApi;

namespace Olve.Engine3D.DebugServer;

public static class ApplicationConfiguration
{
    public static WebApplication ConfigureOlveEngine3DDebugServer(this WebApplication app)
    {
        app.UseWebSockets();
        app.MapOpenApi();

        app.Map("/ws", async context =>
            await context.RequestServices
                .GetRequiredService<WebSocketManager>()
                .AcceptSocketAsync(context)
        );

        app.MapPost("/run-command", (
                    [FromBody] RunCommandRequest request,
                    IHandler<RunCommandRequest> handler,
                    CancellationToken ct) => handler.RunAsync(request, ct))
            .WithResultMapping()
            .WithName("RunCommand")
            .WithValidation<RunCommandRequest, RunCommandValidator>()
            .WithOpenApi();

        app.MapPost("/logs", (
                [FromBody] GetLogsRequest request,
                IHandler<GetLogsRequest, GetLogsResponse> handler,
                CancellationToken ct) => handler.HandleAsync(request, ct))
            .WithResultMapping<GetLogsResponse>()
            .WithName("GetLogs")
            .WithValidation<GetLogsRequest, GetLogsValidator>()
            .WithOpenApi();

        app.MapGet("/health", () => TypedResults.Ok("healthy"))
            .Produces<string>()
            .WithName("GetHealth")
            .WithOpenApi();
        
        var embeddedProvider = new ManifestEmbeddedFileProvider(
            typeof(ApplicationConfiguration).Assembly,
            "SvelteDist"
        );

        app.UseDefaultFiles(new DefaultFilesOptions {
            FileProvider = embeddedProvider,
            RequestPath  = ""
        });
        
        app.UseStaticFiles(new StaticFileOptions {
            FileProvider = embeddedProvider,
            RequestPath  = "" 
        });

        // optional SPA fallback for client-routing
        app.Use(async (ctx, next) =>
        {
            await next();
            if (ctx.Response.StatusCode == 404 && !System.IO.Path.HasExtension(ctx.Request.Path.Value!))
            {
                ctx.Request.Path = "/index.html";
                await next();
            }
        });

        return app;
    }
}
