# B案「赤＋黄」「青＋黄」の追加画像

作成日：2026-10-25（Asia/Tokyo）。内蔵 `image_gen` で前回のB案画像を編集しました。Unityへの反映は行っていません。

[3配色を画面別に比較](index.html) ／ [通常プレイの3配色一覧](gameplay-comparison.png) ／ [追加2配色の主要画面](overview.png) ／ [全生成プロンプト](generation-specs.json)

## 新規10枚

各配色について、タイトル・通常プレイ・不正解表示・線の再開時の色選択・クリアを5枚ずつ生成しています。

| 画面 | 赤＋黄＝オレンジ | 青＋黄＝緑 |
|---|---|---|
| タイトル | [PNG](red-yellow/01-title.png) | [PNG](blue-yellow/01-title.png) |
| 通常プレイ | [PNG](red-yellow/02-gameplay.png) | [PNG](blue-yellow/02-gameplay.png) |
| 不正解表示 | [PNG](red-yellow/04-incorrect.png) | [PNG](blue-yellow/04-incorrect.png) |
| 線の再開・色選択 | [PNG](red-yellow/05-resume-color.png) | [PNG](blue-yellow/05-resume-color.png) |
| クリア | [PNG](red-yellow/06-clear.png) | [PNG](blue-yellow/06-clear.png) |

## 共通条件

- 選定済み2D B / Toon Boldの青空背景・クリーム・濃紺の輪郭線、配置・サイズを継承。
- ゲームの第1色／第2色／混色を連動させ、開始点、指定枠、タイル面、線、重なり、混色早見表、色別リセット、色選択、タイトルの混色モチーフへ反映。
- 黄色のリセット矢印と色選択文字は濃紺にして見分けやすくする。
- 背景・HOME・主ボタンの青は共通UI色。プレイ色の変更では置き換えない。
- クリアは縮小した完成盤面と次ステージボタン1つ。HOMEやリセットは表示しない。
- 不正解画像では線・着色を保持し、2行3列の誤りマスの枠と面が点滅している途中を表現。
- `COLOR TRACE` は前回の仮称を継承。

## 盤面と表示番号

色を比較できるよう、盤面配置と線の経路は前回のStage3の表示例に統一しました。新しい問題の作成・実データの差し替えは行っていません。

赤＋黄の見出しはSTAGE 6、次へはSTAGE 7。青＋黄の見出しはSTAGE 11、次へはSTAGE 12。配色区間を示す表示例であり、実際の問題6・11の配置ではありません。

実装済みの配色区間はStage1〜5が赤＋青、6〜10が赤＋黄、11〜15が青＋黄、以降15ステージ周期です。

チュートリアルはStage1専用のため、[既存の赤＋青の指ガイド](shared/03-tutorial.png)を共通で参照します。別配色のStage1や、後続ステージのチュートリアルは追加していません。

## ファイル

- `red-yellow/` と `blue-yellow/`：新規PNG各5枚。
- `red-blue/`：横並び比較に使う既存PNG5枚のコピー。
- `shared/03-tutorial.png`：既存Stage1ガイドのコピー。
- `index.html`：3配色を画面ごとに比較し、拡大できるページ。
- `gameplay-comparison.png` と `overview.png`：生成画像を加工せず並べた確認用一覧。
- `generation-specs.json`：内蔵画像生成へ渡した全10プロンプト。
- `generation-sources.json`：生成原本と保存先の対応。
- `manifest.json`：画像寸法・容量・SHA-256。
- `palette-variants.zip`：新規10枚・比較用6枚・一覧と確認資料。
- `build-palettes.mjs`：確認ページ・一覧・メタデータを生成するスクリプト。

動き、SE、振動は静止画には含まれません。実装に用いる具体的な色値・縮小率は実装時に数値で指定します。画像は認識確認中の提案で、採用済み記録の更新は行っていません。
