using System;
using System.IO;
using System.Text;
using System.Text.Json;

using BlueBlaze.Core;

namespace BlueBlaze.Application.Cli;

/// <summary>
/// CommandGenerator が生成する CLI コマンドハンドラから呼ばれる、結果表示用の薄いヘルパー。
/// </summary>
internal static class CliOutput
{
    internal static void WriteJson(JsonDocument document)
    {
        // JsonElement.WriteTo は型のリフレクションを使わない低レベル API なので、
        // NativeAOT でも安全にインデント付き JSON を出力できる。
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            document.RootElement.WriteTo(writer);
        }

        Console.WriteLine(Encoding.UTF8.GetString(stream.ToArray()));
    }

    internal static void WriteError(LexiconException ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
    }
}
