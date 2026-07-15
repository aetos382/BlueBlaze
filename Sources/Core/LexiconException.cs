using System;

namespace BlueBlaze.Core;

public class LexiconException : Exception
{
    public LexiconException()
        : base("Lexicon error")
    {
    }

    public LexiconException(string message)
        : base(message)
    {
    }

    public LexiconException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
