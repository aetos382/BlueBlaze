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

var generateTypeInfoOption = new Option<bool>("--generate-type-info")
{
    Description = "JsonTypeInfo ベースのデシリアライザーを生成する（AOT/トリミング対応）",
    DefaultValueFactory = _ => false
};

var targetFrameworkOption = new Option<string?>("--target-framework", ["-f"])
{
    Description = "生成コードのターゲットフレームワーク（例: net10.0、netstandard2.0）。BCL がサポートする場合、AOT 属性を自動的に出力します。"
};

var forceEmitAotAttributesOption = new Option<bool>("--force-emit-aot-attributes")
{
    Description = "AOT 属性（[RequiresDynamicCode] / [RequiresUnreferencedCode]）を強制的に出力します。BCL がサポートしないフレームワーク（netstandard2.0 など）でポリフィルを使用する場合に指定します。"
};

var disableNullableAnnotationsOption = new Option<bool>("--disable-nullable-annotations")
{
    Description = "nullable 参照型アノテーション（T?）を生成コードに出力しません。プロジェクトで <Nullable>enable</Nullable> を設定していない場合に指定してください。"
};

var emitMetadataAttributesOption = new Option<bool>("--emit-metadata-attributes")
{
    Description = "Lexicon スキーマのメタデータ情報を BlueBlaze.Core 属性として生成コードに出力します。"
};

var manifestOutputOption = new Option<FileInfo?>("--manifest-output")
{
    Description = "生成されたファイルのパス一覧を書き出すファイルのパス。MSBuild インクリメンタルビルド用。"
};

var rootCommand = new RootCommand("BlueBlaze Lexicon コードジェネレーター")
{
    inputArgument,
    outputOption,
    namespaceOption,
    generateTypeInfoOption,
    targetFrameworkOption,
    forceEmitAotAttributesOption,
    disableNullableAnnotationsOption,
    emitMetadataAttributesOption,
    manifestOutputOption
};

rootCommand.SetAction(async (parseResult, ct) =>
{
    var inputs = parseResult.GetRequiredValue(inputArgument);
    var outputDir = parseResult.GetRequiredValue(outputOption);
    var ns = parseResult.GetRequiredValue(namespaceOption);
    var manifestOutput = parseResult.GetValue(manifestOutputOption);

    var options = new BlueBlaze.LexiconGenerator.Core.GeneratorOptions
    {
        GenerateTypeInfo = parseResult.GetValue(generateTypeInfoOption),
        TargetFramework = parseResult.GetValue(targetFrameworkOption),
        ForceEmitAotAttributes = parseResult.GetValue(forceEmitAotAttributesOption),
        NullableAnnotationsEnabled = !parseResult.GetValue(disableNullableAnnotationsOption),
        EmitMetadataAttributes = parseResult.GetValue(emitMetadataAttributesOption)
    };

    var config = parseResult.InvocationConfiguration;

    return await GenerateHandler
        .RunAsync(inputs, outputDir, ns, options, manifestOutput, config.Error, ct)
        .ConfigureAwait(false);
});

return await rootCommand
    .Parse(args, new ParserConfiguration())
    .InvokeAsync()
    .ConfigureAwait(false);
