# 広告・計測SDKの削除確認

実施日: 2026-11-06 / Unity 6000.3.24f1

## 変更内容

- `Assets/MaxSdk`、`Assets/Adjust` と各 `.meta` を削除。Android/iOS のネイティブSDK、Editor用処理、Prefab、Resources内のキー・設定、自動初期化処理も含む。
- Android Manifestから広告ID・インストールリファラー・広告計測用の通信権限、Adjust受信処理、Facebook/Instagramのパッケージ照会を削除。ゲームの振動用権限は維持。
- Android、iOS、Standaloneのアプリ識別子を、指定された `com.zume.blendtrails` に変更。Company Nameは `zume`。
- ゲーム本体のソースにMAX/Adjust呼び出しはなく、削除対象へのシーン・Prefab・その他アセットのGUID参照もなかった。UPMで導入されたSDKはなく、Packagesの変更は不要。
- SDK用の生成済みC#プロジェクトファイル4件も削除。
- 初回APK検査で、以前のAndroid生成プロジェクトに残った広告設定JSONとキーの混入を検出したため、`Library/Bee/Android/Prj/IL2CPP/Gradle` を削除して再生成。

## 確認結果

- Unityの再コンパイル完了後、読み込まれたMAX/Adjustアセンブリは0件。
- `Assets`、`Packages`、`ProjectSettings`内にSDK参照・会社の旧アプリ識別子は残っていない。削除した設定内のキー2件を値を表示せず照合し、他のソースへの残存がないことも確認。
- 削除SDKの全アセットGUIDを照合し、残ったアセットからの参照は0件。

| テスト | 成功 | 失敗 |
| --- | ---: | ---: |
| EditMode（ルール・ステージ構成・全60ステージの設定など） | 48 | 0 |
| PlayMode / PuzzleFlowTests（描画、混色、クリア、リセットなど） | 8 | 0 |
| PlayMode / TitlePresentationTests（起動演出、開始ボタンなど） | 5 | 0 |
| PlayMode / SceneNavigationTests.ClearingStageOneUpdatesTheTitleToStageTwoAndStartsThatStage（クリア後の次ステージ・ホーム・再開） | 1 | 0 |

PlayModeテスト全体の一括実行はしていない。上表の14件を実行した。

## Androidビルド・APK検査

- Android / IL2CPP / ARM64のAPKビルドに成功。最終ビルドのエラーは0件。警告12件はいずれも既存アプリアイコンの圧縮設定に関するもの。
- 検証用APK: `Build/SdkRemoval/BlendTrails.apk`（41,737,336 bytes）。
- APKのApplication IDが `com.zume.blendtrails` であることをAndroid SDKのaaptで確認。
- 最終APKの全エントリを展開して照合し、MAX/AdjustのSDK文字列、会社の旧アプリ識別子、削除したキー2件の一致はすべて0件。生成Gradleプロジェクトにも広告SDKの依存関係・設定ファイルは残っていない。
- APKの権限は `android.permission.VIBRATE` と `android.permission.INTERNET`。広告ID・インストールリファラー権限は含まれていない。
- 検証後は元のSampleSceneを未変更・非再生の状態で開き、SDKアセンブリが読み込まれていないことを再確認。

## 制限・アプリ識別子について

接続されたAndroid端末がないため、実機上の操作確認は未実施。iOS実機ビルド・署名・ストア登録も未実施。

Bundle IDはOSやストアがアプリを区別する識別子。AndroidではApplication IDに相当する。変更後のAndroid版は旧IDのアプリとは別アプリとして扱われ、旧アプリの更新や保存データの自動引き継ぎにはならない。個人アカウントでのストア登録は別途必要。

今回の削除は現在の作業ツリーが対象。Gitの過去の履歴と過去に配布したビルドは変更していない。

- [Apple: Bundle ID](https://developer.apple.com/help/glossary/bundle-id/)
- [Android: Application ID](https://developer.android.com/build/configure-app-module)
