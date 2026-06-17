using System;
using System.CommandLine;
using System.Net.Http;

using BlueBlaze.Application.Cli;
using BlueBlaze.Core;

var accessJwtOption = new Option<string?>("--access-jwt")
{
    Description = "Bearer トークン(アクセス JWT)。省略時は BLUEBLAZE_ACCESS_JWT 環境変数を使う。",
};

var serviceOption = new Option<string?>("--service")
{
    Description = "接続先 PDS のベース URL(既定値: https://bsky.social)。",
};

// CommandGenerator が生成する RootCommand は AtProtocolClient を構築済みであることを
// 前提にしているため、まず --access-jwt/--service だけを緩い設定で予備解析する。
var preParseRoot = new RootCommand
{
    TreatUnmatchedTokensAsErrors = false,
};

preParseRoot.Add(accessJwtOption);
preParseRoot.Add(serviceOption);

var preParseResult = preParseRoot.Parse(args);
var accessJwt = preParseResult.GetValue(accessJwtOption)
    ?? Environment.GetEnvironmentVariable("BLUEBLAZE_ACCESS_JWT");
var serviceRaw = preParseResult.GetValue(serviceOption);
var serviceUri = string.IsNullOrEmpty(serviceRaw) ? new Uri("https://bsky.social") : new Uri(serviceRaw);

// HttpClient は既定で disposeHandler=true のため、内部の CliAuthHandler も
// httpClient.Dispose() と一緒に破棄される。
#pragma warning disable CA2000
using var httpClient = new HttpClient(new CliAuthHandler(accessJwt))
{
    BaseAddress = serviceUri,
};
#pragma warning restore CA2000

var client = new AtProtocolClient(httpClient);

var rootCommand = global::BlueBlaze.Application.Cli.Generated.CliCommandTree.BuildRootCommand(client);
rootCommand.Add(accessJwtOption);
rootCommand.Add(serviceOption);

return await rootCommand.Parse(args).InvokeAsync().ConfigureAwait(false);
