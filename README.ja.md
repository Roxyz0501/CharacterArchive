# Character Archive

[English](README.md)

Character Archiveは、Roxyz0501が作成するXIVLauncher / Dalamud向けキャラクター情報管理プラグインです。ログインしたキャラクター、ジョブ、累計プレイ時間、Lodestoneプロフィール、FFXIV設定フォルダとの対応をローカルへ保存し、ゲーム内UIとCSVで確認できます。

## 主な機能

- キャラクター名、ホーム／現在ワールド、Content ID、種族、部族、性別、カレントジョブ／レベル、全クラス・ジョブのレベルを記録
- Content IDから対応する`FFXIV_CHR...`設定フォルダを読み取り専用で特定
- 標準`/playtime`応答を「12日 3時間 45分」または`00291:45:00`で表示
- Lodestone IDと公開プロフィールの顔画像を個別または頻度制御したまとめ検索で取得
- キャラクター名、サーバー名、Lodestone ID、設定フォルダ名をUTF-8 BOM付きCSVとして任意の場所へ出力
- 明示的に押した場合だけRoxyz0501のKo-fiを開く任意支援ボタン

## 導入方法

ソース、Issues、ライセンス、Releaseは本プラグインの[専用GitHubリポジトリ](https://github.com/Roxyz0501/CharacterArchive)で管理し、配信はRoxyz0501の共通Dalamudカスタムリポジトリを使用します。

`https://raw.githubusercontent.com/Roxyz0501/DalamudPluginRepo/main/repo.json`

Dalamud設定 → Experimental → Custom Plugin Repositoriesへ上記URLを追加し、プラグイン一覧から導入します。ローカル確認ではReleaseビルド後のDLLまたは格納フォルダをDev Plugin Locationsへ追加します。

## 利用方法・コマンド

キャラクターへログインすると情報が追加・更新されます。`/chararchive`でメインUIを開閉します。概要、ジョブ、キャラファイル紐づけ、設定、支援をタブで表示し、Lodestone検索、ptime再取得、CSV出力は対応する操作または明示設定時だけ実行します。

## 設定

- **言語：** 日本語、English、Deutsch、Français、한국어、简体中文、繁體中文から選択します。Auto設定はありません。
- **ログイン時にptimeを自動取得する：** 既定OFF。有効時だけコマンド解禁後、ログインごとに標準`/playtime`を1回実行します。
- **コマンド使用可能後の待機秒数：** 1～60秒、既定1秒。
- **プレイ時間の表示形式：** ゲーム風表示または累計時間表示。
- **ログイン時にLodestone IDを自動取得する：** 既定OFF。
- **CSV出力先：** 保存ダイアログの初期フォルダ。最終保存先は毎回選択できます。

手動ptime取得は10秒制限です。通常のゲーム内テキストコマンド経路だけを使い、独自パケットの生成・直接送信・ポーリング・自動再送は行いません。

### 初回の言語決定

有効な保存済み言語がない場合だけ、公開APIから言語を1回決定して保存します。最初にゲームクライアント言語（`IClientState.ClientLanguage`）、次にDalamud UI言語（`IDalamudPluginInterface.UiLanguage`）を確認し、どちらも取得不能・未対応ならEnglishを選びます。現行SDKにはこのプラグインが利用できる公開ランチャー言語APIがないため、非公開ファイルや設定は読みません。保存後は起動時やキャラクター切替時に上書きしません。旧English／日本語のenum番号を維持し、欠落、旧Auto、不正値だけを一度解決します。

変更は即時反映・永続保存されます。プラグイン所有のUI、状態・エラー、支援、ptime表示を7言語化しています。CSVはデータ交換仕様として、当初定義した日本語4列ヘッダー（`キャラクター名,サーバー名,ロドストID,設定ファイル名`）をUI言語にかかわらず維持します。キャラクター名やゲーム由来の種族・ジョブ・ワールド名はゲームデータが提供する表記を使い、提供されない言語を独自翻訳で補いません。

## 必要環境・依存関係

- Windows版FINAL FANTASY XIV、XIVLauncher、Dalamud API 15
- ソースからビルドする場合のみ.NET 10
- 必須依存プラグインなし

DalamudとFFXIVClientStructsは実行環境から提供されます。[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)も参照してください。

## データとプライバシー

Dalamud設定へContent ID、キャラクター名、ワールド、Lodestone ID、ジョブレベル、ptime、FFXIV設定フォルダ名／パス、検出日時、UI言語を保存します。

- 設定：`%APPDATA%\XIVLauncher\pluginConfigs\CharacterArchive.json`
- 顔画像キャッシュ・既定CSVフォルダ：`%APPDATA%\XIVLauncher\pluginConfigs\CharacterArchive\`
- CSV：ユーザーが明示的に選んだ場所

FFXIV設定フォルダは読み取るだけです。Lodestone HTTPS通信は検索操作時または自動検索を有効にした場合だけ行い、公式`*.finalfantasyxiv.com`へキャラクター名とホームワールドを送信して公開Lodestone ID・顔画像を取得します。Content ID、ローカルパス、ジョブ、ptime、CSVは送信しません。まとめ検索は直列・1秒間隔、タイムアウト20秒、画像は公式HTTPSホスト・5MB以下です。

プラグイン自身はKo-fiへ通信しません。支援ボタンを押した場合だけ既定ブラウザで`https://ko-fi.com/roxyz0501`を開きます。

## 既知の制約

- Lodestoneのメンテナンス、検索結果、公開状態、HTML変更によりIDや顔画像を取得できない場合があります。
- `/playtime`は分単位のため、時間表示の秒部分は常に`00`です。
- FFXIV更新で実行環境の構造が変わると、対応更新までptime取得が動作しない場合があります。
- 韓国語・簡体字・繁体字の表示可否は使用中のDalamudフォントアトラスにも依存するため、公開前に実ゲームでの目視確認が必要です。
- 利用者自身でFFXIV、XIVLauncher、Dalamudの最新規約・方針を確認してください。

## トラブルシューティング

- 顔画像がない場合は個別／まとめ検索を再実行し、公開プロフィールを確認してください。
- ログイン直後に`/playtime`が使えない場合は待機秒数を増やすか、ログイン完了後に手動再取得してください。
- 表示言語が意図と異なる場合は設定で7言語のいずれかを選択してください。以後その値を維持します。
- 設定フォルダがない場合は、そのWindows／FFXIV環境に対象キャラクターのフォルダが存在するか確認してください。

## アンインストール・データ削除

Dalamudからアンインストールします。保存データも消す場合はゲームとDalamudを終了し、上記の設定JSONとキャッシュフォルダを削除してください。別の場所へ出力したCSVは個別に削除します。

## ビルド・パッケージ

```powershell
dotnet restore .\CharacterArchive.slnx --locked-mode
dotnet build .\CharacterArchive.slnx -c Release --no-restore
dotnet run --project .\CharacterArchive.Tests\CharacterArchive.Tests.csproj -c Release --no-build
powershell -ExecutionPolicy Bypass -File .\scripts\pack-release.ps1 -NoBuild
```

次の未公開成果物は`artifacts\CharacterArchive-0.6.0.0.zip`です。公開時に最終Release URLをメタデータへ設定してください。[PUBLICATION_CHECKLIST.md](PUBLICATION_CHECKLIST.md)も参照してください。

## 第三者参照・帰属

- **BetterPlaytime** — 作者Infi、[リポジトリ](https://github.com/caitlyn-gg/BetterPlaytime)、MIT License。標準`/playtime`をテキストコマンド経路で実行し、`UIModule.PrintPlayTime`応答から分単位ptimeを取得する考え方を参考にしています。該当箇所は`GameChatCommandService.cs`と`Plugin.cs`のptime応答処理です。
- **FFXIVClientStructs** — aersおよびコントリビューター、[リポジトリ](https://github.com/aers/FFXIVClientStructs)、MIT License。実行環境提供の`UIModule`、`RaptureShellModule`、`Utf8String`等の公開型・APIを利用し、バイナリやソースは同梱しません。

必要な表示は[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)に収録しています。第三者の成果をRoxyz0501の作成物として扱いません。

## ライセンス・免責・AI支援

[MIT License](LICENSE)で提供します。FINAL FANTASY XIV © SQUARE ENIX CO., LTD. 本プロジェクトはSQUARE ENIX、XIVLauncher、Dalamudの公式製品ではありません。無保証であり、利用は自己責任です。

設計・実装・文書作成にはOpenAI CodexによるAI支援を使用しています。公開前にRoxyz0501によるコードレビューとFFXIV実機確認が必要です。

## 任意支援

[Ko-fi: Roxyz0501](https://ko-fi.com/roxyz0501)から任意で支援できます。支援は必須ではなく、機能解放にも関係しません。
