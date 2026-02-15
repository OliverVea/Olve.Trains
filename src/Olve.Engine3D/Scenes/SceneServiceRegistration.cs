using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Olve.Engine3D.Systems;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.Scenes;

public static class SceneServiceRegistration
{
    private static int ResolvePriority(IServiceProvider sp, ISceneServiceType[]? after, ISceneServiceType[]? before)
    {
        if (after is { Length: > 0 } && before is { Length: > 0 })
        {
            var afterPriority = after.Select(t => t.ResolvePriority(sp)).Max() + 1;
            var beforePriority = before.Select(t => t.ResolvePriority(sp)).Min() - 1;

            if (afterPriority > beforePriority)
            {
                var logger = sp.GetRequiredService<ILoggerFactory>()
                    .CreateLogger(nameof(SceneServiceRegistration));
                logger.LogWarning(
                    "Event scene service priority conflict: 'after' resolved to {AfterPriority} but 'before' resolved to {BeforePriority}",
                    afterPriority, beforePriority);
            }

            return (afterPriority + beforePriority) / 2;
        }

        if (after is { Length: > 0 })
        {
            return after.Select(t => t.ResolvePriority(sp)).Max() + 1;
        }

        if (before is { Length: > 0 })
        {
            return before.Select(t => t.ResolvePriority(sp)).Min() - 1;
        }

        return 0;
    }

    extension(IServiceCollection services)
    {
        public IServiceCollection AddSceneService<T>(Id<IScene> sceneId)
            where T : class, ISceneService
        {
            services.TryAddScoped<T>();
            services.AddKeyedScoped<ISceneService>(sceneId, (sp, _) => sp.GetRequiredService<T>());
            return services;
        }

        public IServiceCollection AddEventSceneService<TEventSource, TEvent>(
            Id<IScene> sceneId,
            Func<TEventSource, Event<TEvent>> eventSelector,
            Action<TEvent> handler,
            ISceneServiceType[]? after = null, ISceneServiceType[]? before = null)
            where TEventSource : notnull
        {
            return services.AddEventSceneService(sceneId, eventSelector, e =>
            {
                handler(e);
                return Result.Success();
            }, after: after, before: before);
        }

        public IServiceCollection AddEventSceneService<TEventSource, THandler, TEvent>(
            Id<IScene> sceneId,
            Func<TEventSource, Event<TEvent>> eventSelector,
            Action<THandler, TEvent> handler,
            ISceneServiceType[]? after = null, ISceneServiceType[]? before = null)
            where TEventSource : notnull
            where THandler : notnull
        {
            return services.AddEventSceneService<TEventSource, THandler, TEvent>(sceneId, eventSelector,
                (h, e) =>
                {
                    handler(h, e);
                    return Result.Success();
                }, after: after, before: before);
        }

        public IServiceCollection AddEventSceneService<TEventSource, TEvent>(
            Id<IScene> sceneId,
            Func<TEventSource, Event<TEvent>> eventSelector,
            Func<TEvent, Result> handler, bool propagateFailedUpdate = false,
            ISceneServiceType[]? after = null, ISceneServiceType[]? before = null)
            where TEventSource : notnull
        {
            services.AddKeyedScoped<ISceneService>(sceneId, (sp, _) =>
            {
                var factory = sp.GetRequiredService<EventQueueFactory>();
                var queue = factory.Create(eventSelector(sp.GetRequiredService<TEventSource>()), handler);
                return new EventSceneService<TEvent>(queue, propagateFailedUpdate, ResolvePriority(sp, after, before));
            });

            return services;
        }

        public IServiceCollection AddEventSceneService<TEventSource, THandler, TEvent>(
            Id<IScene> sceneId,
            Func<TEventSource, Event<TEvent>> eventSelector,
            Func<THandler, TEvent, Result> handler, bool propagateFailedUpdate = false,
            ISceneServiceType[]? after = null, ISceneServiceType[]? before = null)
            where TEventSource : notnull
            where THandler : notnull
        {
            services.AddKeyedScoped<ISceneService>(sceneId, (sp, _) =>
            {
                var eventSource = sp.GetRequiredService<TEventSource>();
                var handlerService = sp.GetRequiredService<THandler>();
                var factory = sp.GetRequiredService<EventQueueFactory>();
                var queue = factory.Create(eventSelector(eventSource), item => handler(handlerService, item));
                return new EventSceneService<TEvent>(queue, propagateFailedUpdate, ResolvePriority(sp, after, before));
            });

            return services;
        }

        public IServiceCollection AddEventSceneService<TEventSource, THandler1, THandler2, TEvent>(
            Id<IScene> sceneId,
            Func<TEventSource, Event<TEvent>> eventSelector,
            Func<THandler1, THandler2, TEvent, Result> handler, bool propagateFailedUpdate = false,
            ISceneServiceType[]? after = null, ISceneServiceType[]? before = null)
            where TEventSource : notnull
            where THandler1 : notnull
            where THandler2 : notnull
        {
            services.AddKeyedScoped<ISceneService>(sceneId, (sp, _) =>
            {
                var eventSource = sp.GetRequiredService<TEventSource>();
                var handler1 = sp.GetRequiredService<THandler1>();
                var handler2 = sp.GetRequiredService<THandler2>();
                var factory = sp.GetRequiredService<EventQueueFactory>();
                var queue = factory.Create(eventSelector(eventSource), item => handler(handler1, handler2, item));
                return new EventSceneService<TEvent>(queue, propagateFailedUpdate, ResolvePriority(sp, after, before));
            });

            return services;
        }
    }
}
