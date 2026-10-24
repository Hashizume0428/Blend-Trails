# クリア演出の変更内容と実装計画

作成日：2026-10-25（Asia/Tokyo）  
状態：**採用案の実装済み・検証中**。以下は実装前に作成した計画。実装内容を末尾へ追記する。

## 採用する見た目

- [採用動画：8秒・60fps](C:/GitHub/CGJ_8/CA_CasualGameJam/output/ui-clear-motion/blend-trails-clear-proposal.mp4)
- [再生・スロー確認ページ](C:/GitHub/CGJ_8/CA_CasualGameJam/output/ui-clear-motion/index.html)
- [場面ごとの画像](C:/GitHub/CGJ_8/CA_CasualGameJam/output/ui-clear-motion/storyboard.png)
- [動画の描画ソース](C:/GitHub/CGJ_8/CA_CasualGameJam/output/ui-clear-motion/render-proposal.mjs)

クリアを大きく祝ってから、完成した盤面と次のステージを見せる。B案の青空、クリーム色のカード、濃紺の輪郭、Lilita Oneの文字を使う。動画内のStage3→Stage4は表示例であり、実装では現在ステージ・次ステージ・パレットに連動させる。

8秒は確認動画の長さ。**次ステージ操作は約3.2秒で解放し、残りは操作できる状態での待機演出**にする。

## 変更内容と時間

時刻0は、正解が確定し `GameHud.PlayClearAsync` を呼び出す瞬間。既存の「指を離す→120ms待って線を確定する」処理はその前にあり、その待機時間は変更しない。

| クリア開始から | 具体的な変更 | 実装上の目標値 |
| --- | --- | --- |
| 0.00〜0.68秒 | 完成盤面が小さく弾み、斜めの光が一度横切る。プレイ用UIをフェードアウト | 盤面の一時拡大は最大約1.8%。UIは0.40〜0.70秒で消す |
| 0.48〜0.98秒 | 背景を暗くし、混色チェックの丸いバッジを中央付近に弾ませて表示 | バッジは0→最終サイズの約146%。拡大時に小さな回転と波紋を付ける |
| 0.68〜1.20秒 | 2つの色の丸が近づき、チェックが短い線→長い線の順に描かれる | 色は `GameManager.Palette` の第1色・第2色・混色を使用 |
| 1.07〜1.72秒 | STAGE CLEAR!が一文字ずつ弾んで登場 | 文字の開始を約22msずつずらす。Y移動・拡大・軽い傾き・透明度を組み合わせる |
| 1.10秒／2.45秒 | 中央から紙吹雪、結果カードへの移行時に左右から追加の紙吹雪 | 動画の目安は92枚＋42枚。ステージ3色・クリーム・黄色のアクセント |
| 1.65〜1.95秒 | 背後の完成盤面をいったんフェードアウト | 盤面の色・線データは保持し、表示だけを薄くする |
| 1.98〜2.78秒 | バッジと見出しが結果画面の上部へ移り、クリーム色カードが現れる。実盤面を60%へ縮小 | 移動・縮小はInOutCubic相当。カードは少し下から88→100%で登場 |
| 2.30〜2.65秒 | 縮小した完成盤面が現れる | 色・線・開始点・枠を保持。着地時に最大約2.4%の小さな弾み |
| 2.48〜2.88秒 | 現在ステージとメッセージが下からフェードイン | 現在の文言とステージ番号を継続利用 |
| 2.82〜3.18秒 | 次ステージボタンが下から弾んで登場 | 80→100%。3.18秒で演出の本編を完了し、約3.2秒で操作可能 |
| 3.18秒以降 | 見出しの拡大縮小、バッジの微小な上下動、星、ボタンの光、矢印の小さな左右移動 | 見出しは96.5〜103.5%、周期1.3秒。バッジ±3設計px。ボタン約±0.8%、矢印±5設計px |

光のスイープは盤面に1回、ボタンに登場後と待機中の間隔を空けた再生を行う。ボタンのスイープは動画では3.42秒・6.62秒。紙吹雪は放出後に落下して消え、待機中に放出し続けない。

現在の構成を継続するもの：完成盤面の60%縮小、現在ステージと次ステージの番号、3種類のパレット、HOMEボタンなし、既存のクリアSE。今回の承認動画は音声なしのため、SE追加・振動調整は別作業とする。

## 現在の実装から分かったこと

| 箇所 | 現在の動作 | 変更の方針 |
| --- | --- | --- |
| `StageClearPresentation.PlayAsync` | 紙吹雪→0.5秒待機→0.4秒縮小→0.2秒ボタン表示 | 約3.18秒の本編へ置き換え、待機中の動きは別管理 |
| `StageScreenLayout.ApplyLayout` | 盤面・カード・見出しの親・ボタンの親の位置や大きさを再設定 | 配置の基準と演出の相対変位を分け、Tweenと競合させない |
| `ToonClearBadge` | チェック全体を一度にメッシュ描画 | 色の集合・線の描画進行を追加 |
| `ClearConfetti` | 画面上から100枚落下する1回の演出 | 中央・左右の2回放出へ変更し、前後の描画位置を制御 |
| `GameManager.CommitLineAsync` | 進行保存→SE→演出待ち→`Cleared`へ移行 | この順序を保つ。演出側の完了を3.18秒にする |
| `GameHud` | 演出中は操作を制限。次ステージ番号を現在ステージ＋1で表示 | 本編完了と操作解放を揃える。待機アニメーションで操作を待たせない |

盤面は `SpriteRenderer` と `BoardLineRenderer` の `MeshRenderer` で描かれている。`CanvasGroup.alpha` では盤面全体をフェードできない。また現在の `ToonClearBackdrop` は盤面より後ろにあるので、このScrimだけでは祝福中の盤面を暗くできない。

## 変更するファイルと責務

### 既存ファイル

| ファイル | 変更する内容 |
| --- | --- |
| [StageClearPresentation.cs](C:/GitHub/CGJ_8/CA_CasualGameJam/Assets/Scripts/StageClearPresentation.cs) | 本編の時間軸・各要素への指示・待機ループ・停止と復元を管理。`PlayAsync`、`ResetPresentation`、`IsPlaying`、`IsPresented`は呼び出し側との契約を維持 |
| [StageScreenLayout.cs](C:/GitHub/CGJ_8/CA_CasualGameJam/Assets/Scripts/StageScreenLayout.cs) | Safe Areaに応じた配置の基準と、盤面の進行度・一時拡大を管理。カードとボタンの基準位置は親に、演出は子に持たせる |
| [ToonClearBadge.cs](C:/GitHub/CGJ_8/CA_CasualGameJam/Assets/Scripts/ToonClearBadge.cs) | 2色の集合、チェックの描画進行、通常状態への復元。既存のパレット設定を再利用 |
| [ClearConfetti.cs](C:/GitHub/CGJ_8/CA_CasualGameJam/Assets/Scripts/ClearConfetti.cs) | 放出位置・初速度・開始時刻を持つ紙片を再利用。中央と左右の放出、寿命、前後レイヤー、文字や盤面を覆いにくい範囲を設定 |
| [GameHud.cs](C:/GitHub/CGJ_8/CA_CasualGameJam/Assets/Scripts/GameHud.cs) | 入力解放条件と表示更新を確認し、必要な範囲だけ調整。番号・パレット・遷移の既存責務を継続 |
| [ToonSkinBuilder.cs](C:/GitHub/CGJ_8/CA_CasualGameJam/Assets/Editor/ToonSkinBuilder.cs) | `StyleClear`の末尾から共通の演出設定処理を呼び、B案再適用で演出参照が失われないようにする |
| [ClearScreenBuilder.cs](C:/GitHub/CGJ_8/CA_CasualGameJam/Assets/Editor/ClearScreenBuilder.cs) | `Apply`の参照・親子関係の再設定と新しい演出を整合。新しい共通設定処理を再利用する。iOS設定も変更する既存の`ApplyAll`は今回の反映入口に使わない |
| [ClearPresentationTests.cs](C:/GitHub/CGJ_8/CA_CasualGameJam/Assets/Tests/PlayMode/ClearPresentationTests.cs) | 新しい時間軸に合わせ、操作制限・縮小・線の保持・中断復元・ループ中の操作を検証 |
| [PuzzleInteractionSceneTests.cs](C:/GitHub/CGJ_8/CA_CasualGameJam/Assets/Tests/EditMode/PuzzleInteractionSceneTests.cs) | シーンの参照、装飾のRaycast無効化、操作ボタン1個、HOMEなしを確認 |
| [ToonUISkin.md](C:/GitHub/CGJ_8/CA_CasualGameJam/Assets/Documents/ToonUISkin.md)、[ClearEffects.md](C:/GitHub/CGJ_8/CA_CasualGameJam/Assets/Documents/ClearEffects.md)、[素材README](C:/GitHub/CGJ_8/CA_CasualGameJam/Assets/UI/ToonBold/README.md) | 実装完了後、時間軸・反映方法・検証結果へ更新。古い`ClearEffects.md`にはHOMEありの記述が残っているため、現行仕様へ統一 |

`GameManager`、`StageProgress`、`StageClearNavigation`、`SceneNavigator`は既存の呼び出し順と保存・遷移処理を利用する。演出のために正解判定・進行保存の仕様を変える必要はない。

### 追加する小さな補助コンポーネント（仮称）

| 追加予定ファイル | 責務と追加理由 |
| --- | --- |
| `Assets/Scripts/ClearBoardVisuals.cs` | 盤面の表示用透明度だけを制御。タイルや線の色データから切り離す |
| `Assets/Scripts/ClearTitleAnimator.cs` | TMPの文字ごとの頂点・色を動かし、元のメッシュに戻す。演出管理クラスの肥大化を避ける |
| `Assets/Scripts/ClearCelebrationGraphic.cs` | 放射光・波紋・星を軽いuGUIメッシュで描画。新しいビットマップ素材を増やさずに再現 |
| `Assets/Scripts/ClearSheenGraphic.cs` | 盤面とボタンの内側へ光を通す。対象範囲と角丸を指定し、枠外へ漏らさない |
| `Assets/Editor/ClearMotionBuilder.cs` | 共通の演出階層と参照を設定し、既存ステージへ演出だけを反映する入口を用意 |

名称は実装時にプロジェクト内の重複を再確認する。時間・強さはまず`StageClearPresentation`のSerializeFieldで調整可能にし、シーン共通の初期値をBuilderが設定する。今回だけのために新しいパッケージ、動画再生機構、Animator Controller、共通設定用ScriptableObjectは増やさない。

## 実装方式で先に決めておく点

### 1. レイアウトと動きの担当を分ける

- `StageScreenLayout`がSafe Areaと設計幅941を基準に最終位置を算出する。動画の720×1280を端末へ固定適用しない。
- バッジと見出しは既存の`ToonClearHeading`配下を使い、親が配置、子が相対移動・回転・拡大を担当する。
- カードは配置用の親を追加し、既存`CreamCard`をその子へ移す。既存Imageや参照を作り直さない。
- `ClearActions`の子は従来どおり`NextStageButton`1個とし、ボタン自身の相対位置・倍率を動かす。光はボタンの子に追加する。
- 盤面は`_board`を直接Tweenせず、`SetClearProgress`に加えて表示用の一時拡大係数をレイアウト側へ渡す。画面比率が変わっても同じ計算経路を使う。

設計座標の目安は、バッジ中心Y=660→285、見出し中心Y=940→442、盤面の結果中心=Safe Area中央から下へ80設計px、ボタン中心=下へ470設計px。最終配置は既存のSafe Area換算を利用する。

### 2. 描画の順序を分ける

背面から順に次の構成にする。

1. 共通の空・雲。
2. 既存のカメラ空間`ToonClearBackdrop`内に、背景用Scrimと独立して表示するクリーム色カード。
3. 実際の盤面・開始点・線。
4. Overlayの`ClearPanel`内に祝福中だけ使うScrim。盤面も暗くし、結果画面への移行中に0へ戻す。
5. 放射光・背面の紙吹雪、バッジ・見出し・ステージ文字・ボタン。
6. 前面の星と少量の紙吹雪。結果表示中は盤面・見出し・ボタンの領域を避ける。

既存の`_cardBackdrop.alpha`1値で、Scrimとカードを同時にフェードさせる実装を分離する。全画面の入力遮断は既存の透明な`ClearPanel` Imageを利用し、追加装飾はすべて`raycastTarget=false`にする。

### 3. 実盤面のフェードで着色データを変えない

`ClearBoardVisuals`が盤面のSpriteRenderer群と合成線のMeshRendererを取得し、`MaterialPropertyBlock`のTintアルファへ表示倍率を乗算する方針。再生前のPropertyBlockを保持し、停止時に戻す。`SpriteRenderer.color`、タイルの`CurrentColors`、線の経路・頂点カラー、`sharedMaterial`の値は演出用に書き換えない。

線の`PuzzleLine.mat`はローカルの`Universal Render Pipeline/2D/Sprite-Unlit-Default`を参照し、シェーダー内で`_Color`を頂点カラーへ乗算していることを確認済み。着手時のStage3確認で、タイル・外枠の実際のマテリアルも含めてアルファ乗算を検証する。これでリスタート時に古い完成色を復元してしまう問題を避ける。

`BoardLineRenderer`は盤面と同じ親の下にあり、ローカル座標で線を保持するため、既存の盤面Transformによる縮小を利用できる。正解の縮小画像を新しく描き直したり、画面キャプチャを盤面へ置き換えたりしない。

### 4. 文字アニメーションは既存のTMPで行う

プロジェクト内に`DOTweenTMPAnimator`の実装は見当たらない。`ClearTitleAnimator`が可視文字の元の頂点・色を一度キャッシュし、文字ごとの中心から倍率・位置・傾きを適用する方式にする。文字ごとのGameObject生成は不要。

開始とTMPの再レイアウト時にキャッシュを更新し、停止時には頂点・色を復元する。待機中の見出し全体の拡大縮小はRectTransformで行い、文字ごとの登場アニメーションと同じ値を二重に変更しない。

### 5. 本編の完了と待機演出を分ける

- 時間の基準はunscaled time。既存のUniTaskとDOTweenを継続利用する。
- 有限の本編は3.18秒で完了し、`IsPlaying=false`、`IsPresented=true`へ移る。`GameManager`はその完了後に`GameState.Cleared`となる。
- 見出し・バッジ・ボタン・星の待機演出は別管理にし、`PlayAsync`で無限ループの終了を待たない。
- 次ステージ操作には、既存の`CanOperate`と`GameState.Cleared`、演出側のCanvasGroup設定の両方が揃って必要。透明になったボタンへの先行タップも防ぐ。
- `RestartStage`、取消トークン、`OnDisable`、`OnDestroy`で本編・待機演出・文字メッシュ・表示用透明度・相対変位をまとめて復元する。
- `OnDisable`だけで止めた際も、古い非同期処理が再開してボタンを表示しないよう、再生ごとの取消と世代番号を併用する。
- 色・ステージ番号は再生開始時に確定して持つ。進行保存が先に次ステージへ進んでいるため、クリア画面の現在番号を`StageProgress.CurrentStageNumber`から取り直さない。

背景の雲の数pxの微動は、主演出完成後の動画比較で調整する項目とする。必要な場合もクリア中だけの表示オフセットとし、タイトルや通常プレイの背景へ常時適用しない。

## 反映対象と事前確認

調査時点の`StageCatalog.Count`はディスク上で60、一方、実在するゲームシーンは`SampleScene`＋`Stage02`〜`Stage30`の30件。HEADの同定義は30のため、実装開始時には作業中の変更状況を改めて確認する。

既存のBuilderの多くは`1..StageCatalog.Count`でループするので、現状のまま一括実行すると存在しないStage31以降へ到達する。新しい演出反映入口は、対象候補の存在・参照を**書き込み前に**確認し、実在するステージだけを一覧化して反映する。追加ステージが揃った後はそれらにも同じ処理を適用できるようにする。今回のUI実装のためにステージ上限を書き戻したり、新しい問題を生成したりしない。

`Title.unity`や`StageSelect.unity`を対象に含めず、タイル条件・開始座標・経路・パレットのデータは変更しない。新規階層の追加はUnity Editor APIで行い、既存シーンのYAMLを直接編集しない。反映処理は繰り返し実行しても装飾やButtonが重複しないようにする。

## 実装する順序と各段階の完了条件

1. **開始状態を確認する。** 作業ツリー、実在シーン一覧、未保存シーン、既存テストの状態、進行データを確認。演出を加える前の状態を保持し、ステージ追加など並行する変更と切り分ける。
2. **Stage3で表示の土台を作る。** Scrimとカードの分離、レイアウト用の親、盤面の表示用フェードを追加。盤面と線の色が保持され、停止時に元へ戻ることを先に確認する。
3. **本編を通す。** 約3.18秒の時間軸、バッジ集合・描画、見出しの文字別登場、上部への移動、盤面の60%縮小、次ボタンの順でつなぐ。操作可能になる瞬間までを確認する。
4. **装飾と待機演出を足す。** 2回の紙吹雪、放射光・波紋・星、盤面とボタンの光、継続する小さな動きを追加。重要な文字と盤面を隠さないこと、中断時にすべて止まることを確認する。
5. **共通設定処理で展開する。** 現存ステージへ演出参照を反映。B案の再適用とクリア画面更新処理からも同じ構成を設定できるようにする。問題条件の差分が出ないことを確認する。
6. **検証と見比べを行う。** 関連テスト→全体の回帰テスト、3配色・画面比率・実機性能を確認。Unityで録画し採用動画と並べ、タイミングを調整してからドキュメントを更新する。

## 検証する項目

### 自動で確認する挙動

- クリア判定前には再生せず、正解確定後に1回だけ開始する。
- 盤面の本格的な縮小は約1.98秒以降。紙吹雪の中央放出は約1.10秒で、従来の「開始直後に紙吹雪」というテスト期待値を更新する。
- 本編中に次ステージを押しても遷移しない。約3.18秒で本編が終わり、待機演出が動いている最中でも次へ進める。
- 盤面倍率は最終的に60%。タイルの色、指定色、線の経路・混色、メッシュの基底カラーを保持する。
- Stage3→Stage4、Stage10→Stage11、桁が増える表示番号、Extraの表示番号でラベルと移動先が一致する。上限に関する期待値は実装開始時のカタログを使う。
- 赤＋青、赤＋黄、青＋黄の各配色がバッジ・紙吹雪に反映される。
- バッジ登場中・縮小中・ボタン登場中・待機中のそれぞれで中断し、非同期処理・Tween・文字・盤面の表示が復元される。再クリアで演出が二重にならない。
- シーン内の参照が揃い、装飾がタップを遮らず、`ClearActions`直下は次ボタン1個、HOMEボタンなし。

既存の`StageProgressTestFixture`を使ってテスト前の進行データを退避・復元する。自動テストのためにユーザーの進行をリセットしない。カタログと未生成シーンによる既存の失敗は、演出変更による失敗と分けて記録する。

### 画面・端末で確認する見た目

- 9:16、縦長スマートフォン相当、3:4タブレット相当、ノッチのSafe Areaで、バッジ・見出し・ボタンが収まる。
- 見出しが最大倍率になってもカードの枠やバッジへ不自然に重ならない。ボタンの光が角丸から漏れない。
- 盤面が消える途中に線だけ残ったり、再表示時に着色が変わったりしない。枠や画像の継ぎ目も出ない。
- Androidで文字アニメーション・紙吹雪・半透明の重なりによる負荷を確認し、60fpsを目標にする。毎フレームのInstantiate・マテリアル複製・大きな配列確保は避ける。
- 低いフレームレートやアプリのバックグラウンド復帰でも、紙吹雪が一度に多重放出されたり、入力不可のまま止まったりしない。

## 次に「実装」を依頼されたときの着手点

この文書の「実装する順序」の1から進める。まずStage3だけでレイヤー分離・レイアウト競合解消・実盤面フェードを確認し、その後に時間軸と各効果を組み合わせる。採用動画を外観の基準とし、`render-proposal.mjs`の数値をUnityのSafe Area換算へ移す。動画ファイル自体をゲームの演出素材として組み込む方式は使わない。

計画作成時の成果物はこの計画書のみであり、実装には着手していなかった。

## 実装記録（2026-10-25）

- 実装開始時にはStage31〜60の追加が完了し、現存する全60ゲームシーンへClearMotionBuilderで反映した。反映前後のタイル座標・開始色・指定色・禁止色・盤面寸法の一致を検証している。
- 計画の5つの補助コンポーネントを追加し、約3.18秒の本編と待機演出を共通のunscaled timeから評価する。次ステージ操作は待機演出の継続中も可能。
- UnityのLinear色空間と採用動画のsRGB合成の差を実画面で調整し、暗幕と放射光の強さを合わせた。ClearTitle.matを追加し、クリア文字の青い影を専用マテリアルへ分離した。
- 紙吹雪は結果カード内の見出し・盤面・ボタンを覆わない範囲へ制限した。各紙片の回転位相もずらしている。
- Stage3の実画面とクリア関連5件のPlayModeテスト、全48件のEditModeテストで動作を確認。全体の回帰テストと実画面の録画を継続する。
- 新しい反映コマンドの事前検査は空のシーンで行い、URPのGlobal Lightがプレビューシーンと重複登録されるEditor上のエラーを避ける。
- 実画面・比較ページ・動画・検証結果：`output/ui-clear-motion-implementation/`。APKの作成、端末への再インストール、振動確認は今回行わない。
