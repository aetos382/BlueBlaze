using System.Collections.Generic;

namespace BlueBlaze.LexiconGenerator.Core;

public sealed record GenerateResult(
    IReadOnlyList<GeneratedSourceFile> Files,
    IReadOnlyList<Diagnostic> Diagnostics);
