using System;
using System.Net;
using System.Net.Http.Headers;

namespace BlueBlaze.Core;

public sealed class LexiconException : Exception
{
    public LexiconException()
        : base("Lexicon error")
    {
    }

    public LexiconException(string message)
        : base(message)
    {
    }

    public LexiconException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }

    public LexiconException(
        Uri requestUri,
        HttpStatusCode statusCode,
        HttpResponseHeaders responseHeaders,
        LexiconError? error,
        Exception? innerException = null)
        : base(CreateMessage(error), innerException)
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

        return !string.IsNullOrEmpty(error.Description)
            ? $"Error: {error.Error}, Description: {error.Description}"
            : $"Error: {error.Error}";
    }
}
