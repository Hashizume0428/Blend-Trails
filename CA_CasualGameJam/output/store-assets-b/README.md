# COLOR TRACE — B案 ストア素材候補

作成日：2026-10-24（日本時間）。採用デザイン：2D B / Toon Bold。
生成方式：内蔵 image_gen。採用済みのタイトル・ステージ画面を参照画像に使用。

青空色・クリーム色・太い濃紺の輪郭線を共通に、赤と青をなぞって紫へ混色するゲーム性を表しています。

| 候補 | アイコンの狙い | フィーチャーグラフィックの狙い |
|---|---|---|
| 01 混色シンボル | 3枚のタイルで赤＋青→紫を伝える | 左の大きなロゴと右の混色モチーフ |
| 02 盤面パズル | 小さな盤面でパズルのジャンルを伝える | 5列×4行の盤面で遊び方を伝える |
| 03 ライン強調 | 大きなラインで縮小時の視認性を重視 | 中央ロゴと左右から入る赤青の経路 |

## 納品ファイル

- `icons/`：指定の1024×1024 PNG、3点。各1,000,000バイト以下。
- `feature-graphics/`：1024×500、RGB 24-bit PNG、アルファチャンネルなし、3点。
- `google-play/icons/`：Google Play入稿用512×512、RGBA 32-bit PNG、3点。全画素不透明の背景を含みます。
- `index.html`：6案とアイコン縮小表示を確認できる比較ページ。
- `comparison.png`：3候補を横並びにした比較画像。
- `manifest.json`：各ファイルの実寸・バイト数・PNGチャンネル数・SHA-256。
- `generation-specs.json`：全6素材の生成プロンプト。
- `02-mini-board-icon-refinement.txt`：盤面アイコンの経路を整えた最終編集プロンプト。
- `generation-sources.json`：生成原本の保存先。
- `originals/`：生成原本。納品ZIPには含めません。

Google Playのストアアイコン規格は512×512の32-bit PNG、最大1024KBです。指定の1024×1024版はマスターとして残し、入稿用を別途作成しました。フィーチャーグラフィックは1024×500のJPEGまたは24-bit PNGで透過不可です。

## 参考と構成への反映

- [Flow Free — US Google Play](https://play.google.com/store/apps/details?id=com.bigduckgames.flow&hl=en_US&gl=US)：色と経路を大きく見せ、パズルの仕組みを短時間で伝える構成を参考にしています。
- [Royal Match — US Google Play](https://play.google.com/store/apps/details?id=com.dreamgames.royalmatch&hl=en_US&gl=US)：明るい配色と大きな主要モチーフによる視認性を参考にしています。
- [Toon Blast — Google Play](https://play.google.com/store/apps/details?id=net.peakgames.toonblast)：丸く太い文字と輪郭の強いカジュアルな表現を参考にしています。
- [Google Playプレビュー素材の公式規格](https://support.google.com/googleplay/android-developer/answer/9866151?hl=en-SG)
- [Google Playアイコンの公式デザイン仕様](https://developer.android.com/distribute/google-play/resources/icon-design-specifications)

構成上の参考にとどめ、他作品のロゴやキャラクターは使用していません。既存のゲーム画面・Unity素材は変更していません。

英語の代替テキスト例：

1. COLOR TRACE logo beside red and blue tile paths joining into purple on a bright sky-blue background.
2. COLOR TRACE logo beside a five-column, four-row puzzle board with red, blue and mixed purple paths.
3. COLOR TRACE logo between bold red and blue paths that meet on a purple tile.
