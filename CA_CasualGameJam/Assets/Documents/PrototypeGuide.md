# COLOR TRACE 試作版

Unity 6000.3.24f1で `Assets/Scenes/Title.unity` を開き、Playで開始します。画面は縦向きです。

## 画面の流れ

- タイトル画面のステージ選択ボタンは1つで、Stage1、Stage2の形式で現在の進行番号を表示します。押すと表示中のステージへ直接移動します。
- クリア画面は選択を待ちます。HOMEはタイトルへ、NEXT STAGEは追加したステージ2（Stage02）へ移動します。
- ステージ2のクリア画面はALL STAGES CLEARを表示します。NEXT STAGEは無効で、HOMEからタイトルへ戻れます。
- ステージ1をクリアすると進行番号が2へ進み、HOMEで戻ったタイトルはStage2を表示します。タイトルから選ぶと、ステージ2を初期状態で開始します。
- ステージ2をクリアするとタイトルはStage3を表示します。Stage3は未実装なのでボタンを無効にし、COMING SOONを表示します。
- 進行番号はシーン移動中も保持します。同じステージの再クリア、未クリアの判定、リロードでは余分に番号を進めません。保存機能は対象外のため、アプリやPlayモードを再起動するとStage1から始まります。
- ステージ右上のホームボタンはタイトルへ戻ります。描画中・着色演出中でも戻ることができ、進行中の操作はシーン破棄時に終了します。

Build SettingsはTitle、SampleScene、Stage02の順です。旧StageSelectシーンは現在の遷移とBuild Settingsには含めません。

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
- 全16マスの条件を満たす線を確定するとクリア画面を表示し、HOMEまたはNEXT STAGEの選択を待ちます。

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

GameManagerのTiles Listには16枚を登録し、Board Sizeは4×4とします。
素材、音量、UI参照は各コンポーネントのInspectorから確認できます。

Color Puzzle/Import Text ResourcesはTMP標準リソースの導入用です。
Color Puzzle/Build Prototypeは初回の生成用で、生成済み盤面の上書きを防ぎます。普段の調整はシーンやPrefabを編集してください。
Color Puzzle/Build Scene FlowはタイトルとクリアUIを更新し、不足していればステージ2を生成します。既存のステージ1・2の問題設定は維持します。
StageClearNavigationのStage Numberでステージ番号、Next Stage Sceneで次のシーンを設定します。最終面ではNext Stage Sceneを空にします。自動遷移タイマーはありません。
StageProgressはプレイ中の次のステージ番号と、番号に対応するシーンを管理します。ステージ追加時はGetSceneNameにもシーンを登録してください。タイトルのMenuNavigationはUse Stage ProgressとStage Label / Stage Descriptionを使用します。
Color Puzzle/Apply Figma Stage UIは、既存の問題配置とクリア表示を維持してステージUIを更新します。
StageScreenLayoutはFigmaの1324×2879を基準にSafe Areaへ配置し、短い縦画面ではセクション間の余白を縮めます。盤面のSpriteRendererとColliderもUIの盤面領域へ追従します。

## テスト

Unity Test RunnerからColorPuzzle.EditModeTestsとColorPuzzle.PlayModeTestsを実行できます。
EditModeは色禁止・混色・完全一致・リロード後の表示・経路・空盤面を検証します。
PlayModeは解答経路、他色開始点、色別リロード、巻き戻し、接触判定、描画中の即時着色・混色、不正解マスの実際の面表示、他色を保持した取消、判定中のリスタートを検証します。
PuzzleVisualTestsでは、重なった辺・交点の混色、キャンセル・リロード後の線表示、禁止マークの中央配置、全開始色の確定後の未着色マスを含む枠・面・禁止マークの同期点滅、2秒での停止、リロード・再確定・初期化時の色の復元を検証します。
SceneNavigationTestsでは、タイトルの1ボタン、ボタン以外のタップ、連続タップ、ステージへの直接移動、クリア後の選択待ち、ホーム復帰、Stage1→Stage2→Stage3の表示・移動先、未実装ステージの無効ボタン、未クリア・再クリア時の番号維持、両方の解答経路と再選択後の初期化を検証します。
StageHudTestsでは、Figma UIの配置・盤面へのタッチ、ALLでの初期化と条件維持、リロード操作制限、Idle・描画中・着色中のホーム復帰を検証します。

### 解答例

座標は左上(0,0)、右・下が正方向です。

赤: (0,0) → (0,1) → (1,1) → (1,0) → (2,0) → (2,1) → (2,2) → (3,2) → (3,1) → (3,0)

青: (0,3) → (0,2) → (1,2) → (1,3) → (2,3) → (3,3) → (3,2) → (3,1)

ステージ2の赤: (3,0) → (2,0) → (1,0) → (0,0) → (0,1) → (1,1) → (1,2) → (2,2) → (2,1) → (3,1)

ステージ2の青: (0,3) → (0,2) → (1,2) → (2,2) → (3,2) → (3,3) → (2,3) → (1,3)

スマートフォン実機のタッチ操作と音の聞こえ方は、別途実機で確認してください。

## Verification

Unity Editor compilation: 0 errors / 0 warnings.
EditMode color/path rules (unchanged; previous run): 12 / 12 passed. PlayMode: 24 / 24 passed (including stage progression, live painting, rollback, both stage solutions, clear choices, stage UI, mixed lines and error indicators).
9:16 portrait title, drawing, clear choices and stage 2 visually checked.
Error feedback: all start lines committed, empty incorrect tiles, synchronized frames/faces/markers, two-second stop, restored face colors, and reload/recommit verified.
Live painting: start and intermediate cells colored while drawing, immediate mixing, incorrect face colors, backtrack/cancel preserving the other color, input locked during resolution, and cancellation on restart verified.
Scene navigation: the title button displays Stage1 / Stage2 and opens the corresponding stage; clear advances the session progress once; HOME retains progress; final title displays disabled Stage3 with COMING SOON; incomplete play, reload and reclearing do not advance it.
Test results: Logs/LivePaintFlow-EditMode.json (unchanged color/path rules) and Logs/StageProgress-PlayMode.json.
Figma stage previews: Logs/FigmaStage-Phone.png and Logs/FigmaStage-Reference.png.
Manual runtime check: frame advancement, 16 / 16 clear, and restart verified.
Screenshots: Logs/Prototype-Portrait.png and Logs/Prototype-Clear.png.
Visual improvements: Logs/VisualImprovements.png (mixed lines, centered forbidden markers and error indicators).
Title progress previews: Logs/StageProgress-Stage1.png, Logs/StageProgress-Stage2.png and Logs/StageProgress-Stage3.png.
Gameplay previews: Logs/LivePaintFlow-Drawing.png, Logs/LivePaintFlow-Clear.png and Logs/LivePaintFlow-Stage02.png.
Editor integration: com.unity.pipeline 0.9.1-exp.1.
