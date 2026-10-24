# 着色時の振動とクリア演出

着色と混色で実際に構成色を追加したとき、TileHaptics.LightTapを呼びます。巻き戻し、キャンセル、色別リロード、ALLによる色の除去は振動しません。高速になぞって複数マスを同じタイミングで補完した場合、40ms以内の要求をまとめます。

- iPhone: UIKitのUIImpactFeedbackGeneratorのLightを使用します。ネイティブ実装はAssets/Plugins/iOS/ColorPuzzleHaptics.mmです。
- Android: View.performHapticFeedbackのCLOCK_TICKをUIスレッドで使用します。
- EditorとPC: 振動しません。実機の振動強度・触感は端末での確認が必要です。

端末側の触覚設定と対応状況に従います。実装の参照資料は[Androidの公式ガイド](https://developer.android.com/develop/ui/views/haptics/haptic-feedback)と[AppleのUIImpactFeedbackGenerator](https://developer.apple.com/documentation/uikit/uiimpactfeedbackgenerator)です。

クリア時の順序:

1. 線の確定後、全20マスの着色・指定色・禁止色を判定。正解なら次のステージ番号を即時保存し、演出中のアプリ終了にも備える。
2. クリアSEを再生し、完成盤面を小さく弾ませて光を通す。プレイ用UIを0.4〜0.7秒で消す。
3. 0.6秒から中央の混色チェックが弾んで登場。現在のパレットの2色が集まり、短い線、長い線の順にチェックを描く。
4. 1.07秒からSTAGE CLEAR!を文字ごとに弾ませ、1.1秒と2.45秒に中央・左右から紙吹雪を放出する。祝福中だけ盤面の手前に暗幕を表示する。
5. 1.98〜2.78秒にバッジと見出しを上部へ移動し、クリーム色カードを表示。実際の盤面を60%に縮小し、色・線・開始点・枠を保って再表示する。
6. 現在ステージとメッセージを表示し、2.82〜3.18秒で次ステージボタンを弾ませて登場させる。HOMEボタンは置かない。
7. 約3.2秒で操作を解放。文字の拡大縮小、バッジの上下動、星、ボタンの光、矢印の小さな動きを継続する。紙吹雪は有限で消え、待機中に追加放出しない。

Stage◯は移動先の表示番号です。Stage3の後はStage4、Stage30の後は新規問題31へ進み、Stage60の後はStage61のエクストラへ進みます。以後もカウントアップします。プレイ中のHOMEはタイトルへ戻ります。
不正解のときは従来の2秒の点滅のみで、クリア演出は再生しません。

StageClearPresentationがunscaled timeの共通時間軸を管理し、GameManagerの取消トークンに従います。本編の完了と待機アニメーションを分離し、待機中も次ステージへ移動できます。初期化・無効化・シーン破棄では非同期処理を取り消し、紙吹雪・文字メッシュ・表示用透明度・位置・大きさ・プレイ用UIを戻します。
StageScreenLayoutが盤面とボタンのSafe Area配置を更新するため、縮小後も盤面は5列×4行の形状を保ちます。

ClearBoardVisualsはMaterialPropertyBlockによる表示用のアルファだけを変えます。タイルの着色データ、線の経路・頂点カラー、共有マテリアルは変更しません。ClearTitleAnimatorは既存TMPの頂点を使い、追加パッケージや文字ごとのGameObjectは不要です。

停止・保存後、**Color Puzzle / Apply Approved Clear Motion**で実在するゲームシーンへ演出参照を反映します。反映前にシーンを検査し、反映前後の問題条件が一致することを確認します。B案全体の再適用と既存クリア画面の更新も共通のClearMotionBuilder.Applyを呼びます。

採用動画・実装計画はdocs/plans/2026-10-25-stage-clear-motion.mdを参照。今回の実画面・動画・テスト結果はoutput/ui-clear-motion-implementation/に保存します。振動の実機確認は引き続き別途です。
