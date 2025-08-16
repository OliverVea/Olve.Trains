using Olve.Engine3D.DebugServer;
using Olve.Logging;

var builder = WebApplication.CreateBuilder(args);
    
builder.Services.AddOlveEngine3DDebugServer<InMemoryLoggingManager>();

var app = builder.Build();

app.ConfigureOlveEngine3DDebugServer();

app.Run();