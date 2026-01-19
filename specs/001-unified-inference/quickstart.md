# Quickstart: UnifiedInference Client

## Goals
- Create a .NET class library under `src/lib` named `UnifiedInference` with output assembly `inference.dll`.
- Implement unified API for OpenAI, Ollama, and Hugging Face.

## Scaffold

```bash
cd /Users/djlawhead/Developer/forkedagain/projects/narratoria
mkdir -p src/lib
cd src/lib
dotnet new classlib -n UnifiedInference
```

Edit `src/lib/UnifiedInference.csproj`:

- Set target framework to net10.0 (or net8.0 if net10.0 unavailable locally):
- Enforce assembly name:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <AssemblyName>inference</AssemblyName>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

## Structure

```
src/lib/
  UnifiedInference.csproj
  Abstractions/
  Core/
  Providers/OpenAI/
  Providers/Ollama/
  Providers/HuggingFace/
  Factory/
```

## DI Usage Example (Conceptual)

```csharp
var openAiClient = new OpenAIClient("api-key");
var httpClient = new HttpClient();
IUnifiedInferenceClient client = new UnifiedInferenceClient(
    openAiClient: openAiClient,
    ollamaTransport: new OllamaHttpTransport(httpClient, baseUrl: "http://localhost:11434"),
    huggingFaceHttp: httpClient
);

var caps = await client.GetCapabilitiesAsync(InferenceProvider.OpenAI, "gpt-4o", ct);
if (caps.supportsText)
{
    var text = await client.GenerateTextAsync(
        new TextRequest { provider = InferenceProvider.OpenAI, modelId = "gpt-4o", prompt = "Hello" },
        new GenerationSettings { temperature = 0.7, top_p = 0.9, max_tokens = 256 },
        ct
    );
}
```

## Build

```bash
cd /Users/djlawhead/Developer/forkedagain/projects/narratoria/src/lib
dotnet build -c Debug
ls bin/Debug/*/inference.dll
```
