# COLOR TRACE 試作版

Unity 6000.3.24f1で `Assets/Scenes/SampleScene.unity` を開き、Playで開始します。画面は縦向きです。

## 操作

- 赤・青の開始点を押し、上下左右になぞって離すと色が確定します。マウスでも操作できます。
- 直前のマスへ戻ると線を短くできます。描画中はマスの色は変わりません。
- 指を離すと、開始マスからなぞった順に1マスずつ着色します。着色演出中は次の線入力と色別リロードを待ちます。
- 色別のRESETボタンで、その色の線と構成色だけを除去します。混色マスは残った単色へ戻ります。
- 線が同じ辺を通る部分と交点は混色で表示します。描画中の線も対象で、キャンセルやリロード後は残った線の色に戻ります。
- 禁止マークはマスの中央に表示します。赤・青両方の線を確定すると、未着色を含む全ての不正解マスの枠を2秒間点滅させます。禁止マスではバツも同じタイミングで点滅し、終了後は通常表示に戻ります。色別リロードでは点滅を止め、引き直しを確定した時に再判定します。
- 全16マスの条件を満たすとクリアです。PLAY AGAINで初期状態に戻ります。

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

## テスト

Unity Test RunnerからColorPuzzle.EditModeTestsとColorPuzzle.PlayModeTestsを実行できます。
EditModeは色禁止・混色・完全一致・リロード後の表示・経路・空盤面を検証します。
PlayModeは解答経路、他色開始点、色別リロード、巻き戻し、接触判定、キャンセル、なぞった順の着色、不正解マスの実際の面表示、演出中リスタートを検証します。
PuzzleVisualTestsでは、重なった辺・交点の混色、キャンセル・リロード後の線表示、禁止マークの中央配置、全開始色の確定後の未着色マスを含む同期点滅、2秒での停止、リロード・再確定・初期化を検証します。

### 解答例

座標は左上(0,0)、右・下が正方向です。

赤: (0,0) → (0,1) → (1,1) → (1,0) → (2,0) → (2,1) → (2,2) → (3,2) → (3,1) → (3,0)

青: (0,3) → (0,2) → (1,2) → (1,3) → (2,3) → (3,3) → (3,2) → (3,1)

スマートフォン実機のタッチ操作と音の聞こえ方は、別途実機で確認してください。

## Verification

Unity Editor compilation: 0 errors / 0 warnings.
EditMode: 12 / 12 passed. PlayMode: 11 / 11 passed (including sequential painting, mixed lines and error indicators).
Error feedback: all start lines committed, empty incorrect tiles, synchronized markers, two-second stop, and reload/recommit verified.
Sequential painting: draw order, separate display frames, actual face colors on incorrect tiles, blocked input during painting, and cancellation on restart verified.
Test results: Logs/SequentialPaint-EditMode.json and Logs/SequentialPaint-PlayMode.json.
Manual runtime check: frame advancement, 16 / 16 clear, and restart verified.
Screenshots: Logs/Prototype-Portrait.png and Logs/Prototype-Clear.png.
Visual improvements: Logs/VisualImprovements.png (mixed lines, centered forbidden markers and error indicators).
Editor integration: com.unity.pipeline 0.9.1-exp.1.
