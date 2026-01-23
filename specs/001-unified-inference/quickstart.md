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
using UnifiedInference;
using UnifiedInference.Abstractions;
using UnifiedInference.Factory;

var http = new HttpClient();
var client = InferenceClientFactory.Create(
  apiKey: Environment.GetEnvironmentVariable("HF_TOKEN")!,
  httpClient: http
);
```

## Text Generation

```csharp
var caps = await client.GetCapabilitiesAsync("mistralai/Mistral-7B-Instruct", ct);
if (!caps.SupportsText) throw new NotSupportedException();

var text = await client.GenerateTextAsync(
  new TextRequest
  {
    ModelId = "mistralai/Mistral-7B-Instruct",
    Prompt = "Give me three bullet facts about the Pacific Ocean",
    Settings = new GenerationSettings { Temperature = 0.7f, TopP = 0.9f, MaxNewTokens = 256 }
  },
  ct
);
Console.WriteLine(text.Text);
```

## Image Generation (Diffusion)

```csharp
var imgCaps = await client.GetCapabilitiesAsync("stabilityai/stable-diffusion-2", ct);
if (!imgCaps.SupportsImage) throw new NotSupportedException();

var img = await client.GenerateImageAsync(
  new ImageRequest
  {
    ModelId = "stabilityai/stable-diffusion-2",
    Prompt = "a watercolor fox in a forest",
    NegativePrompt = "blurry",
    Height = 768,
    Width = 768,
    Settings = new GenerationSettings { GuidanceScale = 7.5f, NumInferenceSteps = 30 }
  },
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
