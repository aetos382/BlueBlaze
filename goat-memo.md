# GOAT メモ

## PDS 起動
VSCode のコマンド パレットから `タスク: タスクの実行` → `PDS: Start` を実行しておく。

## PDS ホスト
[docker-compose.yml](.devcontainer/docker-compose.yml) で指定されているように、本環境では `http://pds:3000` である。

## 招待コード発行
`goat xrpc --admin-password <admin-password> procedure <pds-host> com.atproto.server.createInviteCode useCount:=1`

`<admin-password>` は `.env` の `PDS_ADMIN_PASSWORD`

もしくは `PDS_INVITE_REQUIRED=false` にすると招待コードが不要になる。

## アカウントを作る
`goat account create --pds-host <pds-host> --handle <handle> --password <password> --email <email> --invite-code <invite-code>`

ハンドルの有効なドメインは `.env` の `PDS_HOSTNAME` で指定する。ただし以下の TLD は使用不可（[handle.ts](https://github.com/bluesky-social/atproto/blob/ccac91b8b2f3045f06354e90858403edbf7ca4d0/packages/syntax/src/handle.ts#L9)）。
- `.local`
- `.arpa`
- `.invalid`
- `.localhost`
- `.internal`
- `.example`
- `.alt`
- `.onion`

`.test` は使える。

## ログイン

`goat account login --username <handle-or-did> --password <password> --pds-host <pds-host>`

成功すると何も出ない。

## ポスト

`goat bsky post <text>`

投稿に成功すると `at-uri` の他に `view post at: https://bsky.app/` から始まる URL も表示されるが、この PDS は `bsky.app` のネットワークに接続されていないため、当然ながら表示できない。現在の環境には AppView が存在しないので、これは後の課題とする。

ポスト内容は以下のコマンドで取得できる。`repo`、`collection`、`rkey` は `goat bsky post` コマンドの出力として得られる。

`goat xrpc --admin-password <admin-password> query @pds com.atproto.repo.getRecord repo==<repo> collection==<collection> rkey==<rkey>`
