# ステージUI比較案

採用決定: **2D B / Toon Bold（ポップ）**。[採用した3画面とデザイン方針](SELECTED.md)

生成日: 2026-10-24（日本時間）
生成方式: 内蔵 image_gen。2D 2案 / 3D風 2案。

## 共通仕様

既存のCOLOR TRACEを基準にした縦画面のプレイ画面です。ステージ選択画面ではありません。
5列×4行のStage3、ホーム、赤＋青＝紫の混色ガイド、ALL・青・赤のリセットを共通にしています。
生成画像はUI検討用の静止画であり、Unityのシーンや素材は変更していません。

| 案 | 画像 | 特徴 | 生成プロンプト |
|---|---|---|---|
| 2D A — Clean Pop | [PNG](01-2d-clean-pop.png) | 余白と明るい配色で、盤面の見やすさを優先したフラットな2D。 | [Prompt](01-2d-clean-pop-prompt.txt) |
| 2D B — Toon Bold | [PNG](02-2d-toon-bold.png) | 太い輪郭線とくっきりした影で、遊び心を強めた2D。 | [Prompt](02-2d-toon-bold-prompt.txt) |
| 3D A — Jelly Gloss | [PNG](03-3d-jelly-gloss.png) | 光沢・丸み・厚みで、押したくなる手触りを表現した3D風。 | [Prompt](03-3d-jelly-gloss-prompt.txt) |
| 3D B — Soft Toy | [PNG](04-3d-soft-toy.png) | マットな素材と柔らかい陰影で、落ち着いた親しみやすさを出した3D風。 | [Prompt](04-3d-soft-toy-prompt.txt) |

比較用ページ: [index.html](index.html)

追加したタイトル・クリアを含む全12画面: [全画面比較](title-clear/index.html) / [新規8枚の保存一覧・プロンプト](title-clear/README.md)

## 仕上げの編集プロンプト

2D A / Bは面の陰影を抑え、2D A / Bと3D Aの指定マスを四角い色枠に揃えています。再現時は各案の初回生成後、次の編集プロンプトを使用してください。

- [2D A 編集](01-2d-clean-pop-refinement.txt)
- [2D B 編集](02-2d-toon-bold-refinement.txt)
- [3D A 編集](03-3d-jelly-gloss-refinement.txt)

## 参考

- [Royal Match / Dream Games](https://www.dreamgames.com/games/royal-match): 丸み、なめらかな形、色の豊かさと立体的な仕上げ。
- [Toon Blast・Match Factory! / Peak](https://www.peak.com/games): カジュアルパズルの盤面中心の画面構成と親しみやすい表現。
- [Candy Crush / King](https://www.king.com/game/candycrush): 明るい色彩と、直感的なカジュアルパズルの表現。

US市場のカジュアルゲームを参考に、既存ゲームの機能に合わせて独自に構成しています。各社のロゴやキャラクターは使用していません。

