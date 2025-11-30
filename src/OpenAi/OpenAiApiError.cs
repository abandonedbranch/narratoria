namespace Narratoria.OpenAi;

public enum OpenAiApiErrorClass
{
    NetworkTimeout,
    HttpError,
    DecodeError
}

public sealed record OpenAiApiError(OpenAiApiErrorClass ErrorClass, string Message, int? StatusCode = null, string? Details = null);

public sealed class OpenAiApiException(OpenAiApiError error, Exception? innerException = null) : Exception(error.Message, innerException)
{
    public OpenAiApiError Error { get; } = error;
}
