using System;

namespace BlueBlaze.Core;

public sealed class LexiconSubscriptionException : LexiconException
{
    public LexiconSubscriptionException()
    {
    }

    public LexiconSubscriptionException(string message)
        : base(message)
    {
    }

    public LexiconSubscriptionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>op=-1 フレームのエラー。</summary>
    public LexiconSubscriptionException(LexiconError error)
        : base(CreateMessage(error))
    {
        this.Error = error;
    }

    /// <summary>op=-1 フレームのエラー情報。接続確立失敗時は null。</summary>
    public LexiconError? Error { get; }

    private static string CreateMessage(LexiconError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return !string.IsNullOrEmpty(error.Message)
            ? $"Error: {error.Error}, Message: {error.Message}"
            : $"Error: {error.Error}";
    }
}
