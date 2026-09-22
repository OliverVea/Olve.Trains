using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.Scenes;

namespace Olve.Trains;

public class DisplayNameResolvingLoggerFactory(
    ILoggerFactory inner,
    DisplayNameResolver displayNameResolver) : ILoggerFactory
{
    public ILogger CreateLogger(string categoryName)
    {
        var innerLogger = inner.CreateLogger(categoryName);
        return new DisplayNameResolvingLogger(innerLogger, displayNameResolver);
    }

    public void AddProvider(ILoggerProvider provider) => inner.AddProvider(provider);
    public void Dispose() => inner.Dispose();
}

public class DisplayNameResolvingLogger(ILogger inner, DisplayNameResolver displayNameResolver) : ILogger
{
    public void Log<TState>(LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);

        if (state is IReadOnlyList<KeyValuePair<string, object?>> properties)
        {
            foreach (var kvp in properties)
            {
                if (displayNameResolver.TryResolve(kvp.Value, out var name))
                {
                    message = message.Replace(kvp.Value!.ToString()!, $"{kvp.Value} ({name})");
                }
            }
        }

        inner.Log(logLevel, eventId, state, exception, (_, _) => message);
    }

    public bool IsEnabled(LogLevel logLevel) => inner.IsEnabled(logLevel);

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => inner.BeginScope(state);
}

public class DisplayNameResolver(SceneScopeAccessor sceneScopeAccessor)
{
    public bool TryResolve(object? value, [MaybeNullWhen(false)] out string name)
    {
        name = value switch
        {
            Id<GuiNode> guiNodeId => GetService<GuiElementService>(service => service.TryGetElement(guiNodeId, out var guiElement) ? guiElement.Name : null),
            _ => null
        };

        return name != null;
    }

    private string? GetService<TService>(Func<TService, string?> nameResolver)
    {
        foreach (var sp in sceneScopeAccessor.ActiveScopeProviders)
        {
            var service = sp.GetService<TService>();
            if (service != null)
            {
                var result = nameResolver(service);
                if (result != null) return result;
            }
        }

        return null;
    }
}
