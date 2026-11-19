# ② 盤面だけ v2

ロゴ・文字・外枠・紙吹雪を外し、元の②と同じ3×3のマスと経路だけを拡大。マスの周りと隙間は水色背景（#28B9F0）。赤→青→混色→完成を保持→巻き戻しの4.8秒ループ。512px版と256px版、20fps。

線はStagePaletteVisuals / GlobalScriptsの赤#FF3E43・青#0068FF・紫#8A25F5。ToonSkinBuilderの線幅0.24と始点スプライト幅0.57（マス幅0.95）を同じ比率で使用。始点は実際のStartRed.png / StartBlue.png。線の光沢、始点の白い反射、移動中の白い点は削除。重複辺は紫、混色の交点はBoardLineRendererと同様に角型。

再生成: node output/unityroom-thumbnails/render.mjs --board-only
検証: node output/unityroom-thumbnails/verify.mjs --board-only

連番フレームとQA画像はGit対象外です。完成GIF・静止画・manifest.json・qa/validation.jsonを保存しています。
