using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Olve.Engine3D.Systems;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.Scenes;

public static class SceneServiceRegistration
{
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
            Func<TEvent, Result> handler, bool propagateFailedUpdate = false)
            where TEventSource : notnull
        {
            services.AddKeyedScoped<ISceneService>(sceneId, (sp, _) =>
            {
                var factory = sp.GetRequiredService<EventQueueFactory>();
                var queue = factory.Create(eventSelector(sp.GetRequiredService<TEventSource>()), handler);
                return new EventSceneService<TEvent>(queue, propagateFailedUpdate);
            });

            return services;
        }

        public IServiceCollection AddEventSceneService<TEventSource, THandler, TEvent>(
            Id<IScene> sceneId,
            Func<TEventSource, Event<TEvent>> eventSelector,
            Func<THandler, TEvent, Result> handler, bool propagateFailedUpdate = false)
            where TEventSource : notnull
            where THandler : notnull
        {
            services.AddKeyedScoped<ISceneService>(sceneId, (sp, _) =>
            {
                var eventSource = sp.GetRequiredService<TEventSource>();
                var handlerService = sp.GetRequiredService<THandler>();
                var factory = sp.GetRequiredService<EventQueueFactory>();
                var queue = factory.Create(eventSelector(eventSource), item => handler(handlerService, item));
                return new EventSceneService<TEvent>(queue, propagateFailedUpdate);
            });

            return services;
        }

        public IServiceCollection AddEventSceneService<TEventSource, THandler1, THandler2, TEvent>(
            Id<IScene> sceneId,
            Func<TEventSource, Event<TEvent>> eventSelector,
            Func<THandler1, THandler2, TEvent, Result> handler, bool propagateFailedUpdate = false)
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
                return new EventSceneService<TEvent>(queue, propagateFailedUpdate);
            });

            return services;
        }
    }
}
