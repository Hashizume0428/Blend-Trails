# unityroom GIFサムネイル候補

①混ざる瞬間、②小さな盤面を一気にクリア、③あと1マス、④ロゴの混色マークが動く、の4案。各512×512 / 256×256、20fps、無限ループ。

index.htmlで4案を同時比較できます。manifest.jsonに尺・容量・フレーム数を記録しています。qa/validation.jsonに検証結果を保存しています。連番フレーム・QA画像・配布ZIPはGit対象外で、画像はスクリプトから再生成できます。

既存のBlendTrailsLogo.png / BlendTrailsMix.pngを利用。②③の3×3盤面はサムネイル用のデモ盤面で、実ゲームの録画ではありません。2本の上下左右経路で9マスを埋め、指定した2マスのみで赤と青を重ねます。

再生成: node output/unityroom-thumbnails/render.mjs

検証: node output/unityroom-thumbnails/verify.mjs
