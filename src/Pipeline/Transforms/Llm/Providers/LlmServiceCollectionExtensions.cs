using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Narratoria.Pipeline.Transforms.Llm.Providers;

public static class LlmServiceCollectionExtensions
{
    public static IServiceCollection AddNarratoriaOpenAiTextGeneration(
        this IServiceCollection services,
        OpenAiProviderOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.TryAddSingleton(options);
        services.TryAddSingleton<ITextGenerationService, OpenAiTextGenerationService>();

        return services;
    }

    public static IServiceCollection AddNarratoriaHuggingFaceTextGeneration(
        this IServiceCollection services,
        HuggingFaceProviderOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.TryAddSingleton(options);
        services.AddHttpClient<HuggingFaceTextGenerationService>();
        services.TryAddSingleton<ITextGenerationService>(sp => sp.GetRequiredService<HuggingFaceTextGenerationService>());

        return services;
    }
}
