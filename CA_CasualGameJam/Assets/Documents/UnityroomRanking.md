# unityroom 総合ステージクリアランキング

unityroom公式SDK `com.unityroom.client` 0.1.0を使用します。Gitリビジョンは `5e3b8e55d675c8990161f8885593c2af3f0ec99c` に固定し、Packages/manifest.jsonとpackages-lock.jsonへ登録しています。

## 送信するスコア

- ボードNo.1「総合ステージクリアランキング」へ、完了したステージ数を整数で送信します。
- スコアは `StageProgress.ClearedStageCount`（現在のステージ番号 − 1）です。Stage1のクリア直後は1、Stage61のプレイ中は60、Stage61のクリア直後は61になります。チュートリアルも含みます。
- 正解時の進行保存後に送信待ちへ追加します。二重クリアや不正な番号では追加しません。初回起動の0は送信しません。
- 起動時には既存の保存データからも送信待ちへ追加します。ブラウザーを閉じて未送信になっても、次回起動で同期できます。

## 実行と設定

`Assets/Scripts/UnityroomStageRanking.cs` にテキストで提供されたHMAC認証用キーとボードNo.1を設定しています。WebGLの通常ビルドだけで起動し、Editor・Android・iOS・テストビルドでは自動送信しません。シーンへのPrefab配置やボタンの追加は不要です。

送信サービスはシーン間で保持します。5秒間隔で最新の未送信スコアを確認し、送信を直列化します。送信中の新しいクリアも保持し、成功済みの同じ値や小さい値は再送しません。タイムアウトは15秒、失敗後の再試行は30秒間隔です。ゲーム進行やクリア演出は通信の完了を待ちません。

unityroom側の設定は、添付画像どおり「単位: Stage」「数値（整数）」「降順（大きいほど良い）」「ハイスコア更新時のみ」にします。公式SDKはボード側の上書きルールに従います。

## 動作確認

`StageRankingSyncTests` は重複・0の除外、送信中のクリア保持と同時送信防止、通信失敗後の再送、キャンセルを検証します。`StageProgressPersistenceTests` は保存後のクリア通知、二重通知防止、旧セーブとエクストラの総クリア数を含めて検証します。

Unity 6000.3.24f1での確認結果:

- PlayMode: 送信キュー4件、進行保存9件の計13件が成功。結果は `Logs/UnityroomRanking-SyncTests.json` と `Logs/UnityroomRanking-ProgressTests.json`。
- WebGL: unityroomビルドプロファイルで成功、エラー0件。キー差し替え直後のEditorコード再読み込みに関する警告が1件ありましたが、完成したデータに正しいキーが含まれることを照合し、そのビルドのブラウザー送信も確認しました。終了時のEditor再コンパイル確認は `up_to_date`、エラー・警告0件です。
- ブラウザー: Stage1を操作でクリアし、No.1へスコア1を送信。ローカル受信先でHMAC署名とタイムスタンプが正しいことを確認しました。意図的なHTTP 429の後、Stage2へ移動しても約30秒後に再送してHTTP 200となり、ページ再読み込み後にも保存済みのスコア1を再同期しました。ブラウザーの実行エラーは0件です。
- この通信確認は127.0.0.1の受信先で行い、unityroomの本番ランキングにはテストスコアを登録していません。記録は `Logs/UnityroomRanking-WebGL-requests.jsonl`。
- アップロード用ZIPは `Build/UnityroomRanking.zip`（23,226,549 bytes、17ファイル）。ZIPのルートにindex.htmlがあること、全ファイルのCRC、完成データ内の設定キーを確認しています。ビルド結果と梱包結果は `Logs/UnityroomRanking-WebGL-build.json` と `Logs/UnityroomRanking-package.json`。

本番への反映はWebGLを再ビルドしてunityroomへアップロードします。ローカルやEditorでunityroomへの本番スコア登録は確認できません。unityroom側で「APIを使用する」が有効であることを確認し、アップロード後にログイン状態でステージを1つクリアして、数秒後にボードNo.1のスコアを確認してください。

- [unityroom: スコアランキング機能の実装方法](https://help.unityroom.com/2e9dc3ed5de980d5a10ce7ebb145e069)
- [unityroom公式SDK](https://github.com/unityroom/unityroom-sdk)
