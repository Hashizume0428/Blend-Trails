# タイトル画面 アニメーション案

2026-10-30 / Blend Trails: Color Puzzle

- `blend-trails-title-proposal.mp4`: 登場から待機までの12秒。720×1280、60fps、H.264、音声なし。
- `blend-trails-title-idle-loop.mp4`: 待機部分だけの6秒のシームレスループ。
- `index.html`: 動画の再生、0.5倍速、場面ジャンプ、待機ループの切り替え。
- `storyboard.png`: 8場面のコマ送り。
- `motion-spec.json`: 演出のタイミングと実装時の目安。
- `video-validation.json`: 全フレームのデコード、形式、ループ境界、元データ不変の確認結果。

既存のTitle.unityの構図、ToonBoldのPNG、Lilita Oneフォントを使った実装前の動画です。ゲーム本体は変更していません。ステージ番号は保存シーンの初期表示に合わせてSTAGE 1にしています。

ロゴが弾んで登場 → 混色イラストが浮かぶ → 赤と黄の光が合流してオレンジが光る → ステージボタンが登場 → ゆるやかな待機、という流れです。開始ボタンは約2.6秒から操作可能にする想定です。

再生成: `node output/ui-title-motion/render-title.mjs`

静止画のみ: `node output/ui-title-motion/render-title.mjs --stills-only`
