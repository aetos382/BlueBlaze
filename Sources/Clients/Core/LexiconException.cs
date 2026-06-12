using System;
using System.Net;
using System.Net.Http.Headers;

namespace BlueBlaze.Client.Core;

public sealed class LexiconException : Exception
{
    public LexiconException(
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

    public Uri RequestUri { get; }

    public HttpStatusCode StatusCode { get; }

    public HttpResponseHeaders ResponseHeaders { get; }

    public LexiconError? Error { get; }

    private static string CreateMessage(LexiconError? error)
    {
        if (error is null)
        {
            return "Unknown error.";
        }
        return !string.IsNullOrEmpty(error.Description)
            ? $"Error: {error.Error}, Description: {error.Description}"
            : $"Error: {error.Error}";
    }
}
