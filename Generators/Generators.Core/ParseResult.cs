using System.Collections.Generic;

namespace BlueBlaze.Generators.Core;

public sealed record ParseResult(
    LexiconDocumentWithInfo? Document,
    IReadOnlyList<Diagnostic> Diagnostics);
