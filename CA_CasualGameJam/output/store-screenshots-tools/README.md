# スクリーンショット再撮影用ツール

成果物の保存先はプロジェクト直下の `StoreScreenshots/` と `StoreScreenshots.zip` です。

`StoreShots.cs` はUnity Pipelineの `script run` でメモリー内にコンパイルして使用します。Assets内へ配置する必要はありません。

実行順序は次のとおりです。ゲーム再生を止め、シーンを保存した状態で始めてください。既存PNGの上書きを避けるため、保存先にPNGがある場合はSetupが中止します。

1. `StoreShots.Setup` を実行して設定・進行データを退避し、Gameビューを1080×1920に設定する。
2. `editor set_auto_tick` でフレーム更新を有効にし、`editor.playmode start` で再生する。
3. `StoreShots.Start` を実行し、`status.json` が `complete` になるまで待つ。
4. `editor.playmode stop` で再生を止め、`StoreShots.Restore` で元の設定と進行データに戻す。
5. Pillowが使えるPythonで `package_screenshots.py` を実行し、寸法・容量・重複を検証して一覧画像とZIPを作る。

Pipelineでの実行例:

```powershell
unity command script run --caller plugin --skill unity-cli --format json -- --file output/store-screenshots-tools/StoreShots.cs --entry StoreShots.Setup
```

Setup・Start・Restoreをそれぞれ呼び分けます。`editor-backup.json` は撮影前の復元情報、`restoration.json` は復元完了記録です。
