namespace TryAGI.HuggingFace;

public sealed class HuggingFaceSettings
{
    public string? ApiKey { get; set; }
    public string? BaseUrl { get; set; }
}

public sealed class HuggingFaceClient
{
    public HuggingFaceSettings Settings { get; }

    public HuggingFaceClient(HuggingFaceSettings settings)
    {
        Settings = settings;
    }
}
