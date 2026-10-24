# 画像資料のステージ1〜10

資料: `output/imagegen/stage-01-clean.png`〜`stage-10-clean.png`。全問5列×4行、赤と青の開始点が各1つです。色枠だけを条件とし、資料にない禁止マスは追加しません。
画像のルートは解答例であり、ゲーム開始時は全20マスが未着色です。元の試作2問は置き換えています。

この表では資料のプロンプトと同じく、`r行c列`を1始まりで表します。UnityのCoordinateは左上(0,0)、右がx、下がyです。

| Stage | 赤開始 | 青開始 | 赤指定 | 青指定 | 紫指定 |
|---|---|---|---|---|---|
| 1 | r1c1 | r4c5 | r1c5, r2c1 | r4c1, r3c5 | なし |
| 2 | r1c1 | r3c4 | r1c5, r4c5 | r4c3 | なし |
| 3 | r1c5 | r4c1 | r1c3 | r4c3 | r2c3, r3c3（個別の紫枠） |
| 4 | r2c2 | r3c2 | r1c1 | r1c5, r3c3 | r1c3, r2c3, r4c4 |
| 5 | r3c5 | r1c1 | r4c5 | r3c2 | r1c4, r3c1 |
| 6 | r1c1 | r1c5 | r1c3, r4c3 | r4c4 | r1c4 |
| 7 | r3c3 | r1c3 | r2c3 | r4c1 | r1c4, r2c4 |
| 8 | r1c2 | r1c1 | なし | r2c3 | r1c4, r1c5, r2c5, r3c5, r4c3, r4c4, r4c5 |
| 9 | r3c3 | r2c3 | r2c4 | r1c5 | r1c4, r3c1 |
| 10 | r3c5 | r1c1 | r2c2 | なし | r1c3, r2c4, r3c1 |

盤面の転記データは `Assets/Editor/ReferenceStageBuilder.cs` にあります。`Color Puzzle/Build Reference Stages 1-10`はこの転記を使って全10シーンを更新するため、Inspectorでの独自調整も上書きします。
各シーンには20枚のPrefabインスタンスと条件を保存しています。プレイヤー実行時に資料フォルダーへアクセスする必要はありません。

資料を別途転記した `Assets/Tests/PlayMode/StageTestReference.cs` で、開始点・指定枠・全問の解答例を検証します。
紫指定は各マスの赤＋青完全一致です。Stage3は追加の指示に合わせ、画像の共有枠を各マスの個別枠に変更しています。不正解マスの枠と面がそれぞれ点滅します。

進行はタイトル→現在のStage→クリア→HOMEまたはNEXT STAGEです。Stage10の次は無効、HOMEで戻るとStage11を無効表示します。進行はセッション中のみ保持します。
