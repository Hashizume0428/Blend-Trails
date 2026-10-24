# ステージ1 チュートリアル 実装結果

実装・検証: 2026-10-24〜25（Asia/Tokyo）

## 動作

- Stage1（SampleScene）だけで、赤・青の想定回答を指と点線で案内する。
- 描画中・判定中・クリア演出中・画面遷移中はガイドを非表示にする。
- 青からの操作にも対応。色別リセット・全リセット・操作取消・中断復帰時に案内を再評価する。
- 別の経路や別の着色分担でも既存のクリア条件を満たせばクリアする。想定回答と矛盾する案内は表示しない。
- 説明文、日本語UI、操作制限、ガイド表示済みの保存フラグは追加していない。

## 変更点

- Assets/Scripts/StageOneTutorialGuide.cs: 表示とアニメーションを担当する新規コンポーネント。
- Assets/UI/Tutorial/StageOneTutorial.prefab: 指・接触円・矢印・点線の専用UI。
- Assets/Editor/StageOneTutorialBuilder.cs: ステージ1だけを更新する配置処理。
- Assets/Editor/ReferenceStageBuilder.cs: 再生成時の重複・他ステージへの混入を防止。
- Assets/Scenes/SampleScene.unity: 専用Prefabと既存のゲーム状態・カメラへの参照を保存。
- Assets/Tests/PlayMode/StageOneTutorialTests.cs: ガイドの6件の受け入れテスト。
- Assets/Documents/StageProgression.md: Stage1のガイド仕様を反映。

GameManager、PuzzleRules、InputTouchManager、LineController、BoardLineRendererは変更していない。
背景・盤面などの全体スキンは既存のものを使用する。

## 検証結果

全34件成功。

| 対象 | 結果 |
|---|---|
| StageOneTutorialTests | PlayMode 6/6成功 |
| StageHudTests | PlayMode 6/6成功 |
| PuzzleRulesTests | EditMode 22/22成功 |

保存したテスト結果:
- Logs/StageOneTutorial-PlayMode.json
- Logs/StageOneTutorial-HudRegression.json
- Logs/StageOneTutorial-RulesRegression.json

指の移動を1ループ以上再生しても盤面が着色されないこと、全タイルでUIのレイキャストが発生しないこと、実際の入力開始が通ることを確認。
別解は経路の形だけが異なる例に加え、赤9マス・青11マスの別の着色分担でもクリアを確認。
Stage2、Stage30、Stage31のエクストラでガイドが存在しないことを確認。

Editor上ではフレームが進んでいる状態を確認してから、9:16と9:18の縦画面を撮影。
1080×2160で青ルートの4つの折れ点を測定し、指先とタイル中心の最大誤差は約0.000016ピクセル。
実端末でのタッチ操作とノッチ付き端末のSafeAreaは、このEditor検証には含まない。

配置処理を2回適用してもガイドは1個。
ステージ1由来のPrefabを他ステージへ複製して配置処理を行うと、専用ガイドだけが除去されることも確認。

## 実装後の画面

- [赤の案内](StageOneTutorial-Red.png)
- [青の案内](StageOneTutorial-Blue.png)
- [縦長画面の青の案内](StageOneTutorial-Blue-Tall.png)

## 調整方法

Assets/UI/Tutorial/StageOneTutorial.prefab の StageOneTutorialGuide から待ち時間、移動速度、指の大きさを調整できる。
Color Puzzle/Apply Stage 1 Tutorial でStage1だけを再配置できる。
指の素材は組み込みimage_genで生成した透過PNG。生成指示は hand-pointer-prompt.txt を参照。

検証終了後、Playモードを停止し、元のTitleシーンと検証前の進行データ（Stage11）を復元した。


指の保存先: [HandPointer.png](../../../Assets/UI/Tutorial/HandPointer.png)（組み込みimage_gen、[生成指示](hand-pointer-prompt.txt)）。


## コミット時の進行状態

2026-10-25のユーザー依頼により、保存済み進行をStage1・問題1・出題履歴なしへ初期化済み。PlayerPrefsはローカル保存であり、Gitのコミット対象には含まれない。

