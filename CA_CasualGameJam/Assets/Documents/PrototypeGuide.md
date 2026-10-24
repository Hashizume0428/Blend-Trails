# COLOR TRACE 試作版

Unity 6000.3.24f1で `Assets/Scenes/Title.unity` を開き、Playで開始します。画面は縦向きです。

## 画面の流れ

- タイトル画面のステージ選択ボタンは1つで、Stage1、Stage2の形式で現在の進行番号を表示します。押すと表示中のステージへ直接移動します。
- クリア画面は選択を待ちます。HOMEはタイトルへ、NEXT STAGEは次の番号のステージへ移動します。全10問です。
- ステージ10のクリア画面はALL STAGES CLEARを表示します。NEXT STAGEは無効で、HOMEからタイトルへ戻れます。
- ステージ1をクリアすると進行番号が2へ進み、HOMEで戻ったタイトルはStage2を表示します。タイトルから選ぶと、ステージ2を初期状態で開始します。
- ステージ10をクリアするとタイトルはStage11を表示します。Stage11は未実装なのでボタンを無効にし、COMING SOONを表示します。
- 進行番号はシーン移動中も保持します。同じステージの再クリア、未クリアの判定、リロードでは余分に番号を進めません。保存機能は対象外のため、アプリやPlayモードを再起動するとStage1から始まります。
- ステージ右上のホームボタンはタイトルへ戻ります。描画中・着色演出中でも戻ることができ、進行中の操作はシーン破棄時に終了します。

Build SettingsはTitle、SampleScene（Stage1）、Stage02〜Stage10の順です。旧StageSelectシーンは現在の遷移とBuild Settingsには含めません。
既存の試作2問は、output/imagegen/stage-01-clean.png〜stage-10-clean.pngの5列×4行の問題へ置き換えています。開始点と指定色を再現し、画像の線は解答例として扱います。開始時に解答線は表示しません。
Stage3の中央の紫指定2マスは、各マスを個別の紫枠で囲みます。両方のマスにそれぞれ赤＋青が必要です。
ステージ入場時にシーンの番号を進行管理へ同期するため、Editorからステージを直接再生してホームへ戻った場合も、選択ボタンに同じ番号を表示します。

## 操作

- 赤・青の開始点を押すとそのマスを着色し、上下左右になぞると線と同時にマスを着色・混色します。マウスでも操作できます。同じ行・列を高速になぞった場合も、中間マスを順に着色します。
- 直前のマスへ戻ると線を短くし、取り消したマスから描画中の色だけを除去します。元からある別の色は保持します。
- 指を離すと線を確定して判定します。判定とクリア表示の演出中は次の線入力とリロードを待ちます。
- 入力キャンセル・フォーカス喪失では、描画中に加えた色と線を取り消し、他色の確定済みの状態を保持します。盤外で離した場合は最後の有効マスまで確定します。
- 盤面下の青・赤のリロードボタンで、その色の線と構成色だけを除去します。混色マスは残った単色へ戻ります。確定した色のボタンだけが使用できます。
- 灰色のALLボタンは、同じ問題の全ての線・色・確定状態を初期化します。描画中・着色演出中は、ALLと色別リロードを使用できません。
- 混色早見表は、このステージで使う紫＝赤＋青だけを表示します。黄色のリロードとヒントは表示しません。
- 線が同じ辺を通る部分と交点は混色で表示します。描画中の線も対象で、キャンセルやリロード後は残った線の色に戻ります。
- 禁止マークはマスの中央に表示します。赤・青両方の線を確定すると、未着色を含む全ての不正解マスの枠と面を2秒間点滅させます。禁止マスではバツも同じタイミングで点滅し、終了後は通常表示に戻ります。面の単色・混色は保ち、透明度を変えて点滅します。色別リロードでは点滅を止め、引き直しを確定した時に再判定します。
- 全20マスの条件を満たす線を確定するとクリア画面を表示し、HOMEまたはNEXT STAGEの選択を待ちます。

## 判定

通常マスは着色されていれば正解です。色枠は完成色を指定します。紫枠には赤と青の両方が必要です。
禁止マークは完成色に適用します。赤禁止では赤単体だけが不正解で、紫やオレンジは許可します。
不正解でも線と内部の構成色を保持し、面は実際の単色・混色で表示します。未着色マスはノーマルカラー（#F9F9F9）です。
リロード後も残った色で表示・再判定するため、紫指定マスが青単色になった場合は青、赤禁止マスが赤単体になった場合は赤を表示します。

他色で塗られた開始点も、その点の本来の色で開始できます。一度確定した色は、色別リロード後に再使用できます。
内部の基本色は赤・青・黄です。同じ色の重複と3色目の追加を防ぎます。

## Inspectorでの調整

盤面はシーン内のBoard配下、タイルはPuzzleTile Prefabのインスタンスです。
TileControllerのCoordinate、Is Start Tile、Start Color、Required Colors、Forbidden Colorで条件を設定します。
Has Required Colorsを有効にするとRequired Colorsの集合と完全一致で判定します。
Current Colors Listは実行中の状態で、開始時・全体リスタート時に初期化します。

GameManagerのTiles Listには20枚を登録し、Board Sizeは5×4とします。StageScreenLayoutのBoard Sizeも一致させます。
素材、音量、UI参照は各コンポーネントのInspectorから確認できます。

Color Puzzle/Import Text ResourcesはTMP標準リソースの導入用です。
Color Puzzle/Build Prototypeは初回の生成用で、生成済み盤面の上書きを防ぎます。普段の調整はシーンやPrefabを編集してください。
Color Puzzle/Build Reference Stages 1-10（Build Scene Flowも同じ処理）はReferenceStageBuilderの画像転記データで10シーンの問題設定と進行先を更新します。Inspectorで独自に調整した問題設定も上書きするため、通常の微調整では実行しません。
StageClearNavigationのStage Numberでステージ番号、Next Stage Sceneで次のシーンを設定します。最終面ではNext Stage Sceneを空にします。自動遷移タイマーはありません。
StageProgressはプレイ中の次のステージ番号を管理し、StageCatalogは番号とシーンを対応付けます。ステージを追加する場合はCount、シーン、Build Settings、解答テストを更新してください。タイトルのMenuNavigationはUse Stage ProgressとStage Label / Stage Descriptionを使用します。
Color Puzzle/Apply Figma Stage UIは、既存の問題配置とクリア表示を維持してステージUIを更新します。
StageScreenLayoutはFigmaの1324×2879を基準にSafe Areaへ配置し、短い縦画面ではセクション間の余白を縮めます。盤面のSpriteRendererとColliderもUIの盤面領域へ追従します。

## テスト

Unity Test RunnerからColorPuzzle.EditModeTestsとColorPuzzle.PlayModeTestsを実行できます。
EditModeは色禁止・混色・完全一致・リロード後の表示・経路・空盤面を検証します。
PlayModeは解答経路、他色開始点、色別リロード、巻き戻し、接触判定、描画中の即時着色・混色、不正解マスの実際の面表示、他色を保持した取消、判定中のリスタートを検証します。
PuzzleVisualTestsでは、重なった辺・交点の混色、キャンセル・リロード後の線表示、禁止マークの中央配置、全開始色の確定後の未着色マスを含む枠・面・禁止マークの同期点滅、2秒での停止、リロード・再確定・初期化時の色の復元を検証します。
SceneNavigationTestsでは、タイトルの1ボタン、ボタン以外のタップ、連続タップ、ステージへの直接移動、クリア後の選択待ち、ホーム復帰、Stage1〜Stage10の連続クリア・全条件配置・20マスの着色、Stage11の無効ボタン、未クリア・再クリア時の番号維持と再選択後の初期化を検証します。
PuzzleFlowTestsとPuzzleVisualTestsの色禁止などは、製品用問題と独立したテスト専用の条件を設定して回帰確認します。
StageHudTestsでは、Figma UIの配置・盤面へのタッチ、ALLでの初期化と条件維持、リロード操作制限、Idle・描画中・着色中のホーム復帰を検証します。

### 解答例

座標は左上(0,0)、右・下が正方向です。

Stage1の赤: (0,0) → (4,0) → (4,1) → (0,1)

Stage1の青: (4,3) → (0,3) → (0,2) → (4,2)

全10問の転記と解答例は、ReferenceStages.mdとテスト用StageTestReference.csで確認できます。同じ行・列の中間マスは順に通ります。

スマートフォン実機のタッチ操作と音の聞こえ方は、別途実機で確認してください。

## Verification

Unity Editor compilation: 0 errors / 0 warnings.
Previous full run: EditMode 13 / 13 passed; PlayMode 25 / 25 passed (all ten stage solutions, start/required-color placements, progression, live painting, rollback, stage UI, mixed lines and error indicators). The latest UI adjustments use a focused check only.
Latest focused PlayMode check: 1 / 1 passed (Stage3's independent frames, matching stage numbers, Home and title-button navigation). Result: Logs/StageUiAdjustments-PlayMode.json.
Previous 9:16 portrait previews checked stages 1, 3, 8 and 10. Stage 8's live mixed faces and lines verified at 20 / 20 correct before release.
Error feedback: all start lines committed, empty incorrect tiles, synchronized frames/faces/markers, two-second stop, restored face colors, and reload/recommit verified.
Live painting: start and intermediate cells colored while drawing, immediate mixing, incorrect face colors, backtrack/cancel preserving the other color, input locked during resolution, and cancellation on restart verified.
Scene navigation: Title -> Stage1 through Stage10 -> clear HOME / NEXT STAGE; final NEXT STAGE disabled; final title displays disabled Stage11 with COMING SOON. Incomplete play, reload and reclearing do not advance progress.
Stage3 uses individual purple frames. Stage entry synchronizes its number with the title button.
Test results: Logs/TenStages-EditMode.json and Logs/TenStages-PlayMode.json.
Manual runtime check: frame advancement and 20 / 20 correct during live painting verified. Current Unity Console: 0 errors / 0 warnings.
Screenshots: Logs/TenStages-01.png, Logs/TenStages-08.png, Logs/TenStages-08-Mixed.png and Logs/TenStages-10.png. The earlier TenStages-03.png shows the former shared frame.
Editor integration: com.unity.pipeline 0.9.1-exp.1.
