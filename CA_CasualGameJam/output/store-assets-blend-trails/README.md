# Blend Trails: Color Puzzle — ストア素材候補

作成：2026-10-25（Asia/Tokyo）。内蔵image_genで制作。最終サイズへの縮小・PNG圧縮はSharpで実施。

現在の正式名とToon Boldデザイン（青空、クリーム色、濃紺の太い輪郭、丸いタイル）を継承しています。2色をなぞり、共有するマス・経路で混色するゲーム性を表現しました。

| 案 | アイコン | フィーチャーグラフィック |
|---|---|---|
| A / Sunshine Mix | 赤＋黄→オレンジの3タイル。タイトルとの統一を重視 | 左に大きなロゴ、右に混色モチーフ |
| B / Puzzle Cross | 赤＋青→紫。盤面の一部を簡略化 | 左にロゴ、右にStage3の5列×4行と経路 |
| C / Fresh Trails | 青＋黄→緑。共有する太い経路を強調 | 上中央にロゴ、下中央に交差する経路 |
| C追加 / Orange | ユーザーが指定したCアイコンの構図を保ち、赤＋黄→オレンジへ変更 | — |

初回3案に加えて、ユーザー指定の色替えアイコンを追加しています。C追加は`04-fresh-trails-orange`のファイルです。

## ファイル

- `icons/`：1024×1024、32-bit PNG、各1,000,000バイト以下。全4点。
- `feature-graphics/`：1024×500、24-bit RGB PNG、アルファチャンネルなし。全3点。
- `google-play/icons/`：Google Play入稿用の512×512、32-bit PNG。全4点。背景は全画素不透明。
- `index.html`：原寸へのリンクと48/64/96pxの縮小比較。追加アイコンも掲載。
- `comparison.png`：初回3案の一覧。
- `blend-trails-store-assets.zip`：全11点の納品画像、比較ページ、検証記録、プロンプトを同梱。
- `manifest.json`：寸法、容量、チャンネル数、SHA-256の検証記録。
- `generation-specs.json`：初回6素材の生成プロンプト全文。
- `orange-variant-spec.json`：追加色替えの編集プロンプト全文。
- `generation-sources.json`と`originals/`：元画像の出典と生成原本。原本はZIP対象外。

技術的な再出力は`node export-assets.mjs`、続いて`node export-orange-variant.mjs`を実行します。

## 参考と構成判断

US向けの英語カジュアルパズル掲載を参考に、主要モチーフを大きく、色を少数に絞り、太い輪郭で小さくても読める構成にしました。以下のデザイン上の観察は制作判断であり、コンバージョン改善を計測した結果ではありません。掲載画像は地域や更新で変わる場合があります。

- [Flow Free — Google Play](https://play.google.com/store/apps/details/Flow_Free?hl=en&id=com.bigduckgames.flow&gl=US)：色付きの経路と盤面でジャンルを伝える表現を参考。Blend Trailsの混色ルールは実際のゲームに合わせています。
- [Color Block Jam — Google Play](https://play.google.com/store/apps/details?id=com.GybeGames.ColorBlockJam&hl=en_US&gl=US)：少数の鮮やかな色、太い主要形状、盤面を大きく見せる構成を参考。
- [Toon Blast — Google Play](https://play.google.com/store/apps/details?id=net.peakgames.toonblast&hl=en_US&gl=US)：丸く大きなロゴと濃い輪郭によるカジュアルな読みやすさを参考。
- [Google Play公式の素材規格](https://support.google.com/googleplay/android-developer/answer/9866151?hl=en)：ストアアイコンは512×512、32-bit PNG、最大1024KB。フィーチャーグラフィックは1024×500、JPEGまたは24-bit PNG、透過不可。ユーザー指定の1024pxアイコンはマスターとして別途保存。
- [Google Playアイコンの公式デザイン仕様](https://developer.android.com/distribute/google-play/resources/icon-design-specifications)：外周は正方形で書き出し、表示時の角丸はGoogle Play側に任せています。

他作品のロゴ・キャラクター・素材は使用していません。元のゲーム素材と実装は保持し、全納品画像を`output/store-assets-blend-trails/`内に保存しています。

## 英語の代替テキスト

- A: Blend Trails logo beside red and yellow tile paths combining into orange on a bright sky-blue background.
- B: Blend Trails logo beside a five-column, four-row board with red and blue paths sharing two purple tiles.
- C: Blend Trails logo above blue and yellow paths crossing at a green tile on a sky-blue background.
- C追加: Red and yellow paths share an orange central segment across six cream tiles on a sky-blue sunburst background.
