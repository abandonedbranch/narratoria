using NarratoriaClient.Components;
using NarratoriaClient.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<ILogBuffer, LogBuffer>();
builder.Services.AddScoped<IClientStorageService, BrowserClientStorageService>();
builder.Services.AddScoped<IAppDataService, AppDataService>();
builder.Services.AddScoped<INarrationService, NarrationService>();

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

// Skip HTTPS redirection in dev/test to avoid redirect loops when only HTTP is bound (e.g., Playwright).
if (!(app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing")))
{
    app.UseHttpsRedirection();
}
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
