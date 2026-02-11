using Microsoft.Extensions.DependencyInjection;
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
            services.AddSingleton<T>();
            services.AddKeyedSingleton<ISceneService>(sceneId, (sp, _) => sp.GetRequiredService<T>());
            return services;
        }

        public IServiceCollection AddEventSceneService<TEventSource, TEvent>(
            Id<IScene> sceneId,
            Func<TEventSource, Event<TEvent>> eventSelector,
            Func<TEvent, Result> handler)
            where TEventSource : notnull
        {
            services.AddKeyedSingleton<ISceneService>(sceneId, (sp, _) =>
            {
                var factory = sp.GetRequiredService<EventQueueFactory>();
                var queue = factory.Create(eventSelector(sp.GetRequiredService<TEventSource>()), handler);
                return new EventSceneService<TEvent>(queue);
            });

            return services;
        }

        public IServiceCollection AddEventSceneService<TEventSource, THandler, TEvent>(
            Id<IScene> sceneId,
            Func<TEventSource, Event<TEvent>> eventSelector,
            Func<THandler, TEvent, Result> handler)
            where TEventSource : notnull
            where THandler : notnull
        {
            services.AddKeyedSingleton<ISceneService>(sceneId, (sp, _) =>
            {
                var eventSource = sp.GetRequiredService<TEventSource>();
                var handlerService = sp.GetRequiredService<THandler>();
                var factory = sp.GetRequiredService<EventQueueFactory>();
                var queue = factory.Create(eventSelector(eventSource), item => handler(handlerService, item));
                return new EventSceneService<TEvent>(queue);
            });

            return services;
        }
    }
}
