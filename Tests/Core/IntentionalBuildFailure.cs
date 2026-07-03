// build-failure-analysis ワークフローの動作検証用に、意図的にコンパイルエラーを仕込んでいる。
// 検証が終わったらこのファイルごと削除すること。
namespace BlueBlaze.Core.Tests;

internal static class IntentionalBuildFailure
{
    private static readonly int Value = "this is not an int";

    internal static int GetValue()
    {
        return Value;
    }
}
