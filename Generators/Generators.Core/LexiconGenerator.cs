using System.Text.Json;

namespace BlueBlaze.Generators.Core;

public sealed class LexiconGenerator
{
    public static LexiconDocumentWithInfo Parse(
        string text,
        string path)
    {
        var document = JsonSerializer.Deserialize(text, LexiconSerializerContext.Default.LexiconDocument)!;
        return new(path, document);
    }
}
