using System.Collections.Generic;

namespace BlueBlaze.LexiconGenerator.Core;

public sealed record ParseResult(
    LexiconDocumentWithInfo? Document,
    IReadOnlyList<Diagnostic> Diagnostics);
