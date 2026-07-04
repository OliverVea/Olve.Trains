using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Olve.Trains.AssetPipeline.Options;

public static class ServiceExtensions
{
    public static IServiceCollection AddAssetPipelineConfiguration(this IServiceCollection services,
        IConfiguration configuration)
    {
        AddOptions<AssetOptions>(services, configuration);
        AddOptions<BuildOptions>(services, configuration);
        AddOptions<LayoutOptions>(services, configuration);
        AddOptions<FontOptions>(services, configuration);
        AddOptions<MeshOptions>(services, configuration);
        AddOptions<ShaderOptions>(services, configuration);
        AddOptions<TerrainOptions>(services, configuration);
        AddOptions<TextureOptions>(services, configuration);
        AddOptions<TextureAtlasOptions>(services, configuration);

        return services;
    }

    private static void AddOptions<T>(IServiceCollection services, IConfiguration configuration)
        where T : class, IAssetOptions, new()
    {
        var defaultInstance = new T();
        var section = configuration.GetSection(defaultInstance.SectionName);

        if (section.Exists())
        {
            services.AddOptions<T>()
                .Bind(section, o => o.ErrorOnUnknownConfiguration = true)
                .ValidateDataAnnotations()
                .ValidateOnStart();
        }
        else
        {
            services.AddOptions<T>()
                .Configure(options =>
                {
                    foreach (var prop in typeof(T).GetProperties())
                    {
                        if (prop.CanRead && prop.CanWrite)
                        {
                            var value = prop.GetValue(defaultInstance);
                            prop.SetValue(options, value);
                        }
                    }
                });
        }
    }
}