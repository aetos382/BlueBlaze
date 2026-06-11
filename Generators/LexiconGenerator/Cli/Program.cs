using System.CommandLine;
using System.IO;

using BlueBlaze.LexiconGenerator.Cli;

var inputArgument = new Argument<FileInfo[]>("input")
{
    Description = "Lexicon JSON ファイル、ディレクトリ、またはグロブパターンのパス",
    Arity = ArgumentArity.OneOrMore,
    CustomParser = InputResolver.Resolve
};

var outputOption = new Option<DirectoryInfo>("--output", ["-o"])
{
    Description = "生成されたコードを出力するディレクトリのパス",
    Required = true
};

var namespaceOption = new Option<string>("--namespace", ["-n"])
{
    Description = "生成されたコードの名前空間",
    Required = true
};

var rootCommand = new RootCommand("BlueBlaze Lexicon コードジェネレーター")
{
    inputArgument,
    outputOption,
    namespaceOption
};

rootCommand.SetAction(async (parseResult, ct) =>
{
    var inputs = parseResult.GetRequiredValue(inputArgument);
    var outputDir = parseResult.GetRequiredValue(outputOption);
    var ns = parseResult.GetRequiredValue(namespaceOption);

    var config = parseResult.InvocationConfiguration;

    return await GenerateHandler
        .RunAsync(inputs, outputDir, ns, config.Error, ct)
        .ConfigureAwait(false);
});

return await rootCommand
    .Parse(args, new ParserConfiguration())
    .InvokeAsync()
    .ConfigureAwait(false);
