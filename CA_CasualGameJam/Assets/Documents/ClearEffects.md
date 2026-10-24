# 着色時の振動とクリア演出

着色と混色で実際に構成色を追加したとき、TileHaptics.LightTapを呼びます。巻き戻し、キャンセル、色別リロード、ALLによる色の除去は振動しません。高速になぞって複数マスを同じタイミングで補完した場合、40ms以内の要求をまとめます。

- iPhone: UIKitのUIImpactFeedbackGeneratorのLightを使用します。ネイティブ実装はAssets/Plugins/iOS/ColorPuzzleHaptics.mmです。
- Android: View.performHapticFeedbackのCLOCK_TICKをUIスレッドで使用します。
- EditorとPC: 振動しません。実機の振動強度・触感は端末での確認が必要です。

端末側の触覚設定と対応状況に従います。実装の参照資料は[Androidの公式ガイド](https://developer.android.com/develop/ui/views/haptics/haptic-feedback)と[AppleのUIImpactFeedbackGenerator](https://developer.apple.com/documentation/uikit/uiimpactfeedbackgenerator)です。

クリア時の順序:

1. 線の確定後、全20マスの着色・指定色・禁止色を判定。正解なら次のステージ番号を即時保存し、演出中のアプリ終了にも備える。
2. 正解ならクリアSEと紙吹雪を一度再生。プレイ用UIを隠し、操作を待機。
3. 0.5秒後、実際の盤面を0.4秒かけて60％に縮小し、画面中央より上へ移動。
4. 完成した色・線・開始点・枠を保持した盤面の下に、左HOME・右Stage◯を横並びで0.2秒かけて表示。
5. ボタンを操作可能にし、選択を待つ。紙吹雪は約2.4秒で消え、再放出しない。

Stage◯は移動先の表示番号です。Stage3の後はStage4、Stage30の後は新規問題31へ進み、Stage60の後はStage61のエクストラへ進みます。以後もカウントアップします。プレイ中のHOMEはタイトルへ戻ります。
不正解のときは従来の2秒の点滅のみで、クリア演出は再生しません。

StageClearPresentationがTweenと進行を管理し、GameManagerの取消トークンに従います。初期化すると紙吹雪・移動・ボタン表示を取り消し、盤面の位置・大きさ・プレイ用UIを戻します。シーン破棄時にもTweenを終了します。
StageScreenLayoutが盤面とボタンのSafe Area配置を更新するため、縮小後も盤面は5列×4行の形状を保ちます。

Color Puzzle/Update Clear Presentationで全60シーンへUI参照を反映します。各シーンの問題条件は変更しません。

今回の確認はClearPresentationTestsの2件に限定し、両方成功しました。紙片の描画データ、演出の順序、色と線の保持、ボタンの配置と遷移、演出中の初期化からの復帰を確認しています。
縦画面の紙吹雪と完成盤面の表示も確認済みです。結果はLogs/ClearEffects-PlayMode.json、画像はLogs/ClearEffects-Confetti.pngとLogs/ClearEffects-Result.pngです。
