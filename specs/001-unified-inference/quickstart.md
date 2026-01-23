# Quickstart: UnifiedInference (Hugging Face Only)

## Goals
- Deliver a .NET class library under `src/lib/UnifiedInference` (assembly `inference.dll`).
- Use the tryAGI/HuggingFace client to call HF Inference API for text and image; gate other modalities unless HF exposes them.

## Scaffold

```bash
cd /Users/djlawhead/Developer/forkedagain/projects/narratoria
mkdir -p src/lib
cd src/lib
dotnet new classlib -n UnifiedInference
```

Edit `src/lib/UnifiedInference.csproj` to enforce target and assembly name:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <AssemblyName>inference</AssemblyName>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="TryAGI.HuggingFace" Version="0.4.0" />
  </ItemGroup>
</Project>
```

## Structure

```
src/lib/
  UnifiedInference.csproj
  Abstractions/
  Core/
  Providers/HuggingFace/
  Factory/

tests/
  UnifiedInference.Tests/
```

## HF Client Setup

```csharp
using TryAGI.HuggingFace;

var http = new HttpClient();
// HF_TOKEN must be a personal/org token with inference access (set env var HF_TOKEN)
var hf = new HuggingFaceClient(new HuggingFaceSettings
{
    ApiKey = Environment.GetEnvironmentVariable("HF_TOKEN")!,
    BaseUrl = "https://api-inference.huggingface.co/models"
});

// Unified client wraps tryAGI/HuggingFace; only HF provider is exposed
var client = new UnifiedInferenceClient(hf, http);
```

## Text Generation

```csharp
var caps = await client.GetCapabilitiesAsync("mistralai/Mistral-7B-Instruct", ct);
if (!caps.supportsText) throw new NotSupportedException();

var text = await client.GenerateTextAsync(
    new TextRequest(
        modelId: "mistralai/Mistral-7B-Instruct",
        prompt: "Give me three bullet facts about the Pacific Ocean",
        stream: false,
        settings: new GenerationSettings { temperature = 0.7, top_p = 0.9, max_new_tokens = 256 }
    ),
    ct
);
Console.WriteLine(text.Text);
```

## Image Generation (Diffusion)

```csharp
var imgCaps = await client.GetCapabilitiesAsync("stabilityai/stable-diffusion-2", ct);
if (!imgCaps.supportsImage) throw new NotSupportedException();

var img = await client.GenerateImageAsync(
    new ImageRequest(
        modelId: "stabilityai/stable-diffusion-2",
        prompt: "a watercolor fox in a forest",
        negativePrompt: "blurry",
        height: 768,
        width: 768,
        settings: new GenerationSettings { guidance_scale = 7.5f, num_inference_steps = 30 }
    ),
    ct
);
File.WriteAllBytes("out-hf.png", img.Bytes!);
```

## Notes
- Audio/Video/Music remain gated by capabilities; throw `NotSupportedException` until tryAGI/HuggingFace adds stable endpoints.
- Use `ProviderOverrides` to pass `use_cache=false` or `wait_for_model=true` when needed.
- Honor `CancellationToken` on all calls; retries on HF 503 should respect `Retry-After` with backoff.
- Performance sanity targets: warm-path text p50 < 2s, warm-path image p50 < 15s; avoid extra retries beyond 503 backoff.
- Configure HF_TOKEN in your environment before running (e.g., `export HF_TOKEN=...`).

## Build

```bash
cd /Users/djlawhead/Developer/forkedagain/projects/narratoria/src/lib
dotnet build -c Debug
ls bin/Debug/*/inference.dll
```
