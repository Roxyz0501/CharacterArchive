# 共通カスタムリポジトリ取り込み前チェックリスト

この文書はローカル準備用です。GitHub、Release、共通repo.jsonへの外部操作はまだ行っていません。

## 確定メタデータ

- InternalName: `CharacterArchive`
- Name: `Character Archive`
- AssemblyVersion: `0.6.0.0`
- DalamudApiLevel: `15`
- Author: `Roxyz0501`
- 必須依存プラグイン: なし
- Release ZIP名: `CharacterArchive-0.6.0.0.zip`

Punchline、Description、Tagsを含む機械可読案は`distribution/CharacterArchive.metadata.json`にあります。RepoUrl、DownloadLinkInstall、DownloadLinkUpdateは共通リポジトリ統合時に実在URLを注入します。

## 人間によるコード・権利確認

- [ ] Roxyz0501が全ソースをレビューする
- [ ] `LICENSE`と`THIRD_PARTY_NOTICES.md`の内容を確認する
- [ ] BetterPlaytimeを参考にした範囲がNOTICEの説明と一致することを確認する
- [ ] 未許諾の画像、アイコン、コード、キャラクターデータが含まれていないことを確認する
- [ ] AI支援の開示内容を確認する

## FFXIV実機確認

- [ ] プラグインのロード、リロード、アンロードで例外が発生しない
- [ ] `/chararchive`でUIを開閉できる
- [ ] 初回起動時、ゲーム言語→Dalamud UI言語→Englishの順で対応言語が選ばれて保存される
- [ ] 設定の言語選択肢が日本語／English／Deutsch／Français／한국어／简体中文／繁體中文の7つで、Autoがない
- [ ] 保存後の再起動とキャラクター切替で手動選択が上書きされない
- [ ] 全タブ、設定、通知、エラー、ツールチップが7言語へ即時切り替わり、CSVの日本語4列スキーマは言語変更で変化しない
- [ ] 韓国語・簡体字・繁体字・欧文アクセントのグリフと、ドイツ語・フランス語長文の折返しを実ゲームで確認する
- [ ] ログイン時のキャラクター情報と全ジョブが正しい
- [ ] ptime自動取得が既定OFFである
- [ ] ptime自動取得を有効にした場合、ログインごとに1回だけ標準コマンドが実行される
- [ ] コマンド禁止中は実行せず、解除後の設定秒数を待つ
- [ ] ptime手動取得の10秒制限が動作する
- [ ] Lodestone自動検索が既定OFFである
- [ ] 個別検索とまとめて検索の結果、顔画像、1秒間隔が正しい
- [ ] CSVの列、BOM、エスケープ、任意保存先が正しい
- [ ] 支援タブの通常・ホバー・選択中表示と文字コントラストを確認する
- [ ] Ko-fiボタンを押したときだけ`https://ko-fi.com/roxyz0501`が開く
- [ ] 支援しなくても全機能を利用できる

## ビルドとパッケージ

```powershell
dotnet restore .\CharacterArchive.slnx --locked-mode
dotnet build .\CharacterArchive.slnx -c Release --no-restore
dotnet run --project .\CharacterArchive.Tests\CharacterArchive.Tests.csproj -c Release --no-build
powershell -ExecutionPolicy Bypass -File .\scripts\pack-release.ps1 -NoBuild
```

`scripts/pack-release.ps1`は`artifacts/CharacterArchive-0.6.0.0.zip`を生成し、内容が次の4ファイルだけであることを検証します。言語リソースはDLLへ埋め込まれます。

- `CharacterArchive.dll`
- `CharacterArchive.json`
- `LICENSE`
- `THIRD_PARTY_NOTICES.md`

共通側の自動化では、プラグインディレクトリをカレントにして上記4コマンドを実行し、`artifacts/*.zip`と`distribution/CharacterArchive.metadata.json`を収集します。

## 共通リポジトリ統合時の作業

- [ ] `RepoUrl`へ本プラグイン専用GitHubリポジトリの実在URLを設定する
- [ ] `DownloadLinkInstall`と`DownloadLinkUpdate`へ、個別GitHub Releases上の検証済み同一バージョンZIPを指す実在URLを設定する
- [ ] 共通repo.jsonの1エントリとしてメタデータを取り込む
- [ ] 新版ごとにAssemblyVersion、ZIP名、URL、Changelogを同期する
- [ ] 共通リポジトリ側で提供するアイコンがある場合、Roxyz0501が権利を持つ64～512px画像だけを使用する

## 秘密情報・個人データ

- [ ] APIキー、トークン、Cookie、Webhook、秘密鍵、`.env`がない
- [ ] 実在キャラクター名、Content ID、Lodestone ID、CSV、設定JSON、顔画像がない
- [ ] `bin`、`obj`、IDEキャッシュ、ユーザー固有の絶対パスをGitへ追加していない
- [ ] Release ZIP内にPDB、deps.json、テスト成果物、ローカルパスがない
