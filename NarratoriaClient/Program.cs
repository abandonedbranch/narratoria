using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using NarratoriaClient.Components;
using NarratoriaClient.Services;
using NarratoriaClient.Services.Pipeline;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<ILogBuffer, LogBuffer>();
builder.Services.AddScoped<IClientStorageService, BrowserClientStorageService>();
builder.Services.AddScoped<IAppDataService, AppDataService>();
builder.Services.AddScoped<INarrationService, NarrationService>();
builder.Services.AddScoped<INarrationPipeline, NarrationPipeline>();
builder.Services.AddScoped<INarrationPipelineStage, CommandHandlerStage>();
builder.Services.AddScoped<INarrationPipelineStage, InputPreprocessorStage>();
builder.Services.AddScoped<INarrationPipelineStage, PlayerMessageRecorderStage>();
builder.Services.AddScoped<INarrationPipelineStage, PromptAssemblerStage>();
builder.Services.AddScoped<INarrationPipelineStage, ModelRouterStage>();
builder.Services.AddScoped<INarrationPipelineStage, LlmClientStage>();
builder.Services.AddScoped<INarrationPipelineStage, PostProcessorStage>();
builder.Services.AddScoped<INarrationPipelineStage, MemoryManagerStage>();

if (builder.Configuration.GetValue<bool>("UseFakeChatService"))
{
    builder.Services.AddSingleton<IOpenAiChatService, FakeOpenAiChatService>();
}
else
{
    builder.Services.AddHttpClient<IOpenAiChatService, OpenAiChatService>();
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// Skip HTTPS redirection in dev/test to avoid redirect loops when only HTTP is bound in local test runs.
if (!(app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing")))
{
    app.UseHttpsRedirection();
}
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
