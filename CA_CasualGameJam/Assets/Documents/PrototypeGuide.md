# COLOR TRACE 試作版

Unity 6000.3.24f1で `Assets/Scenes/Title.unity` を開き、Playで開始します。画面は縦向きです。

## 画面の流れ

- タイトル画面のステージ選択ボタンは1つで、Stage1、Stage2の形式で現在の進行番号を表示します。押すと表示中のステージへ直接移動します。
- クリア画面は完成した盤面と中央のStage◯ボタンを表示して選択を待ちます。押すと表示された次のステージへ移動します。用意された問題は全60問です。
- Stage1〜4は既存問題を使うチュートリアルで、Stage1のみ指のガイドを表示します。Stage5〜60はノーマル56問を番号順に出題します。
- ステージ1をクリアすると進行番号が2へ進み、HOMEで戻ったタイトルはStage2を表示します。タイトルから選ぶと、ステージ2を初期状態で開始します。
- Stage60クリア後はStage61以降のエクストラへ進みます。問題5〜60から直近8問と同じ問題を除いて抽選し、Stage番号は61、62、63…と増え続けます。最初の抽選は問題53〜60を除いた問題5〜52が対象です。
- 進行番号は端末のPlayerPrefsへ保存し、アプリのタスク終了やPlayモードの再起動後も引き継ぎます。クリアの正解判定時に次の番号を即時保存するため、紙吹雪や縮小演出中に終了しても進行を保持します。同じステージの再クリア、未クリアの判定、リロードでは余分に番号を進めません。プレイ途中の線・着色は保存せず、続きの問題は初期状態から始まります。
- ステージ右上のホームボタンはタイトルへ戻ります。クリア演出中を除き、描画中・線の確定中でも戻ることができ、進行中の操作はシーン破棄時に終了します。

Build SettingsはTitle、SampleScene（問題1）、Stage02〜Stage60の順です。エクストラは既存ノーマルのシーンを再利用し、表示するStage番号を進行管理から取得します。旧StageSelectシーンは現在の遷移とBuild Settingsには含めません。
既存の試作2問は、output/imagegen/stage-01-clean.png〜stage-10-clean.pngの5列×4行の問題へ置き換えています。開始点と指定色を再現し、画像の線は解答例として扱います。開始時に解答線は表示しません。
Stage3の中央の紫指定2マスは、各マスを個別の紫枠で囲みます。両方のマスにそれぞれ赤＋青が必要です。
問題11〜20はoutput/stage-ideas-11-20/stages-11-20.json、問題21〜30はoutput/stage-ideas-21-30/stages-21-30.jsonの開始点・指定色を転記しています。全問5列×4行で、解答線は開始時に表示しません。問題データは赤・青を基準に保持し、実行時にそのステージの色へ置き換えます。
問題31〜60はoutput/stage-ideas-31-60/stages-31-60.jsonの追加30案を使用します。禁止マスはありません。通常マスは単色のみ、混色は指定マスのみという同じルールです。
ステージ入場時にシーンの番号を進行管理へ同期するため、Editorからステージを直接再生してホームへ戻った場合も、選択ボタンに同じ番号を表示します。

## ステージの色

チュートリアルを含む表示Stage番号を基準に、5ステージごとに切り替えます。

| 表示番号 | 第1色（旧赤） | 第2色（旧青） | 混色（旧紫） |
|---|---|---|---|
| 1〜5、16〜20、31〜35 … | 赤 | 青 | 紫 |
| 6〜10、21〜25、36〜40 … | 赤 | 黄 | オレンジ |
| 11〜15、26〜30、41〜45 … | 青 | 黄 | 緑 |

盤面・開始位置・解法は共通です。Stage61以降のエクストラでも、再利用する問題IDではなく表示番号から色を決定します。入場時に色を保持し、クリアで次の番号を保存しても演出中の色は変えません。HOME・リロード・再起動後も同じ表示番号なら同じ組み合わせです。

## 操作

- そのステージの基本2色の開始点を押すとそのマスを着色し、上下左右になぞると線と同時にマスを着色・混色します。マウスでも操作できます。同じ行・列を高速になぞった場合も、中間マスを順に着色します。
- 着色・混色でマスに色を追加した瞬間、スマートフォンを軽く振動させます。高速移動で40ms以内に続く追加は1回にまとめます。巻き戻し・キャンセル・リロードで色を除去する際と、Editorでは振動しません。
- 直前のマスへ戻ると線を短くし、取り消したマスから描画中の色だけを除去します。元からある別の色は保持します。
- 指を離すと線を確定して判定します。判定とクリア表示の演出中は次の線入力とリロードを待ちます。
- 入力キャンセル・フォーカス喪失では、描画中に加えた色と線を取り消し、他色の確定済みの状態を保持します。盤外で離した場合は最後の有効マスまで確定します。
- 盤面下の色別リロードボタンで、その色の線と構成色だけを除去します。中央は第2色、右は第1色です。混色マスは残った単色へ戻ります。確定した色のボタンだけが使用できます。
- 灰色のALLボタンは、同じ問題の全ての線・色・確定状態を初期化します。描画中・着色演出中は、ALLと色別リロードを使用できません。
- 混色早見表は、そのステージの混色＝第1色＋第2色を表示します。色別リロードは常に使用中の2色だけを表示します。
- 線が同じ辺を通る部分と交点は混色で表示します。描画中の線も対象で、キャンセルやリロード後は残った線の色に戻ります。
- 禁止マークはマスの中央に表示します。使用中の2色の線を両方確定すると、未着色を含む全ての不正解マスの枠と面を2秒間点滅させます。禁止マスではバツも同じタイミングで点滅し、終了後は通常表示に戻ります。面の単色・混色は保ち、透明度を変えて点滅します。色別リロードでは点滅を止め、引き直しを確定した時に再判定します。
- 全20マスが正解なら紙吹雪を一度放出し、0.5秒後に盤面を0.4秒かけて60％へ縮小・移動します。完成した色と線を保ち、盤面の下にHOME・Stage◯を0.2秒で表示します。ボタンは演出完了後に操作でき、紙吹雪は自然に消えます。不正解時はこの演出を行いません。

## 判定

通常マス（色指定なし）は、そのステージの基本2色のどちらか単色のみ正解です。未着色・混色・使用外の基本色は不正解です。色指定のない開始点にもこのルールを適用します。色枠は完成色を指定し、単色枠はその単色、混色枠には使用中の2色の両方が必要です。
通常マスを混色に塗る操作は許可します。両方の線を確定した時に不正解と判定し、既存の2秒点滅を行います。色別リロードで片方の色を除去すると単色へ戻り、そのマスは再判定されます。
禁止マークは完成色に適用します。通常マスでは単色の条件も必要です。紫指定マスに赤禁止があっても、完成色の紫は赤単体の禁止には抵触しません。
不正解でも線と内部の構成色を保持し、面は実際の単色・混色で表示します。未着色マスはノーマルカラー（#F9F9F9）です。
リロード後も残った色で表示・再判定するため、紫指定マスが青単色になった場合は青、赤禁止マスが赤単体になった場合は赤を表示します。

他色で塗られた開始点も、その点の本来の色で開始できます。一度確定した色は、色別リロード後に再使用できます。
内部の基本色は赤・青・黄です。同じ色の重複と、そのステージで使用しない色の追加を防ぎます。

## Inspectorでの調整

盤面はシーン内のBoard配下、タイルはPuzzleTile Prefabのインスタンスです。
TileControllerのCoordinate、Is Start Tile、Start Color、Required Colors、Forbidden Colorで条件を設定します。
Has Required Colorsを有効にするとRequired Colorsの集合と完全一致で判定します。
Current Colors Listは実行中の状態で、開始時・全体リスタート時に初期化します。
保存されたStart Color・Required Colors・Forbidden Colorは基準の赤・青・紫です。実行中のStartColor・RequiredColor・ForbiddenColorは変換後の色を返し、Current Colors Listは実際に塗った色を保持します。基準の色へ黄を直接追加することで3色ステージを作る構成にはしていません。

GameManagerのTiles Listには20枚を登録し、Board Sizeは5×4とします。StageScreenLayoutのBoard Sizeも一致させます。
素材、音量、UI参照は各コンポーネントのInspectorから確認できます。

Color Puzzle/Import Text ResourcesはTMP標準リソースの導入用です。
Color Puzzle/Build Prototypeは初回の生成用で、生成済み盤面の上書きを防ぎます。普段の調整はシーンやPrefabを編集してください。
Color Puzzle/Build Reference Stages 1-60（Build Scene Flowも同じ処理）はReferenceStageBuilderの転記データで60シーンの問題設定と進行先を更新します。Inspectorで独自に調整した問題設定も上書きするため、通常の微調整では実行しません。Color Puzzle/Add Reference Stages 31-60は既存1〜30を再保存せずに、新しい30シーンを追加します。Color Puzzle/Add Reference Stages 11-30は以前の20問追加用として範囲11〜30を維持しています。
Color Puzzle/Apply Revised Stages 5,6,9,10は、確認済みの差し替え4問だけを更新します。旧配置は再生成の定義からも削除しているため、全問再生成やエクストラ出題でも差し替え後の問題を使います。
StageClearNavigationのStage Numberでステージ番号、Next Stage Sceneで次のシーンを設定します。最終面ではNext Stage Sceneを空にします。自動遷移タイマーはありません。
StageProgressは表示番号・選択済みの問題ID・直近8問の履歴をPlayerPrefsのColorPuzzle.StageProgress.v2へJSONとして保存します。旧ColorPuzzle.CurrentStageNumberの整数セーブは進行番号を保って移行し、以後も互換用に更新します。30問版のエクストラでStage31〜60にいるセーブは、表示番号を保って同番号の新規問題へ移行します。Stage61以上は選択済みの問題と履歴を維持します。初回はStage1、Stage60の後も出題を続けます。HOMEやタスキルで選択済みの問題を再抽選しません。アンインストール・アプリデータ削除や別端末への引き継ぎには対応しません。StageCatalogは問題IDとシーンを対応付けます。詳細はStageProgression.mdを参照してください。
Color Puzzle/Apply Figma Stage UIは、既存の問題配置とクリア表示を維持してステージUIを更新します。
Color Puzzle/Apply Stage Color Rotationは、保存済みの全60シーンに共有素材・早見表の参照だけを設定します。盤面条件は再生成しません。共有素材はAssets/UI/Stage/StagePaletteVisuals.assetです。既存PNGの色部分はStagePaletteUI.shaderで切り替え、白い矢印・灰色の記号・影は維持します。
StageScreenLayoutはFigmaの1324×2879を基準にSafe Areaへ配置し、短い縦画面ではセクション間の余白を縮めます。盤面のSpriteRendererとColliderもUIの盤面領域へ追従します。
Color Puzzle/Update Clear Presentationは問題配置を保持し、全60シーンのクリア演出UIだけを更新します。StageClearPresentationで演出時間、ClearConfettiで紙片数と表示時間を調整できます。詳細はClearEffects.mdを参照してください。

## テスト

Unity Test RunnerからColorPuzzle.EditModeTestsとColorPuzzle.PlayModeTestsを実行できます。
EditModeは色禁止・混色・完全一致・リロード後の表示・経路・空盤面を検証します。
PlayModeは解答経路、他色開始点、色別リロード、巻き戻し、接触判定、描画中の即時着色・混色、不正解マスの実際の面表示、他色を保持した取消、判定中のリスタートを検証します。
PuzzleVisualTestsでは、重なった辺・交点の混色、キャンセル・リロード後の線表示、禁止マークの中央配置、全開始色の確定後の未着色マスを含む枠・面・禁止マークの同期点滅、2秒での停止、リロード・再確定・初期化時の色の復元を検証します。
SceneNavigationTestsでは、タイトルの1ボタン、ボタン以外のタップ、連続タップ、ステージへの直接移動、クリア後の選択待ち、ホーム復帰、Stage1〜Stage60の連続クリア・全条件配置・20マスの着色、Stage61/62の表示とエクストラ出題、未クリア・再クリア時の番号維持と再選択後の初期化を検証します。
PuzzleFlowTestsとPuzzleVisualTestsの色禁止などは、製品用問題と独立したテスト専用の条件を設定して回帰確認します。
StageHudTestsでは、Figma UIの配置・盤面へのタッチ、ALLでの初期化と条件維持、リロード操作制限、Idle・描画中・着色中のホーム復帰を検証します。
StageColorPaletteTestsでは、5ステージ周期、3組の通常マス・指定色・禁止色、使用外の色の拒否、繰り返し適用時の基準データ維持を検証します。StagePaletteFlowTestsでは、黄のリロード、オレンジ・緑の線混色とキャンセル、Stage61/66/71での同一問題の再利用、切り替え境界でのクリア演出と再初期化を検証します。

### 解答例

座標は左上(0,0)、右・下が正方向です。

Stage1の赤: (0,0) → (4,0) → (4,1) → (0,1)

Stage1の青: (4,3) → (0,3) → (0,2) → (4,2)

全60問の転記と解答例は、ReferenceStages.md、ReferenceStageBuilder.cs、テスト用StageTestReference.csで確認できます。同じ行・列の中間マスは順に通ります。

スマートフォン実機のタッチ操作と音の聞こえ方は、別途実機で確認してください。

## Verification

Unity Editor compilation: 0 errors / 0 warnings.
The sixty-stage expansion (2026-10-25) passed the full PlayMode suite: 53 / 53. Results: Logs/SixtyStages-PlayMode.json.
After the final presentation fixes, EditMode 48 / 48 and the focused StageProgression PlayMode checks 9 / 9 passed. Results: Logs/SixtyStages-EditMode.json and Logs/SixtyStages-Progression.json.
The final integration test clears all sixty authored puzzles with their rotating palettes, checks every start/frame and all twenty cells, then verifies Stage61/62 navigation, title/header/button numbering, HOME/reentry and persistent extra selection.
Persistence checks include former extra saves at display stages31/45/60, a stale integer key, an old extra at Stage73, forty extra draws excluding the previous eight puzzles, duplicate clears and saving before presentation. Tests restore previous user preferences.
All thirty additional authoring definitions and example routes match output/stage-ideas-31-60/stages-31-60.json. Title and all original puzzle scenes remain byte-for-byte unchanged. Build Settings contains Title plus sixty puzzle scenes. Results: Logs/SixtyStages-Audit.json.
New scenes retain the current skin's start-dot size, tile/frame sizes, sliced board backing, clear-button sprite, font and contained next arrow. EditMode checks cover those visual contracts across all sixty scenes.
Portrait previews checked new stages31/36/41 in all three palettes. A final Stage60 clear reached 20 / 20 and displayed Stage61; frames advanced during verification. Files: Logs/SixtyStages-Stage31.png, Logs/SixtyStages-Stage36.png, Logs/SixtyStages-Stage41.png and Logs/SixtyStages-Clear60.png.
Mobile builds, device haptics and actual mobile task termination were not tested in this change.
Editor integration: com.unity.pipeline 0.9.1-exp.1.
