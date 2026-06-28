using System;
using System.Net;
using System.Net.Http.Headers;

namespace BlueBlaze.Core;

public sealed class LexiconHttpException : LexiconException
{
    public LexiconHttpException()
    {
    }

    public LexiconHttpException(string message)
        : base(message)
    {
    }

    public LexiconHttpException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public LexiconHttpException(
        Uri requestUri,
        HttpStatusCode statusCode,
        HttpResponseHeaders responseHeaders,
        LexiconError? error)
        : base(CreateMessage(error))
    {
        this.RequestUri = requestUri;
        this.StatusCode = statusCode;
        this.ResponseHeaders = responseHeaders;
        this.Error = error;
    }

    public Uri? RequestUri { get; }

    public HttpStatusCode? StatusCode { get; }

    public HttpResponseHeaders? ResponseHeaders { get; }

    public LexiconError? Error { get; }

    private static string CreateMessage(LexiconError? error)
    {
        if (error is null)
        {
            return "Lexicon error";
        }

        return !string.IsNullOrEmpty(error.Message)
            ? $"Error: {error.Error}, Message: {error.Message}"
            : $"Error: {error.Error}";
    }
}
