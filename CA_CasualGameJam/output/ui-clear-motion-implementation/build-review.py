from pathlib import Path
import json, subprocess, sys, re
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parent
repo = root.parent.parent
sys.path.insert(0, str(root.parent / 'ui-clear-motion' / '_tools'))
import imageio_ffmpeg

ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
samples = json.loads((root/'video-samples.json').read_text(encoding='utf-8-sig'))
lines = ['ffconcat version 1.0']
for i, sample in enumerate(samples):
    file = root / sample['file']
    assert file.exists(), file
    end = samples[i+1]['elapsed'] if i+1 < len(samples) else 8
    lines += [f"file '{file.as_posix()}'", f"duration {max(.001,end-sample['elapsed']):.7f}"]
lines.append(f"file '{(root/samples[-1]['file']).as_posix()}'")
(root/'native-frames.ffconcat').write_text('\n'.join(lines), encoding='utf-8')
video = root/'blend-trails-clear-native.mp4'
subprocess.run([ffmpeg,'-hide_banner','-loglevel','error','-y','-f','concat','-safe','0','-i',str(root/'native-frames.ffconcat'),'-vf','scale=720:1280','-r','30','-t','8','-an','-c:v','libx264','-preset','fast','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',str(video)],check=True)
subprocess.run([ffmpeg,'-hide_banner','-loglevel','error','-y','-ss','3.65','-i',str(video),'-frames:v','1',str(root/'poster.png')],check=True)
subprocess.run([ffmpeg,'-hide_banner','-loglevel','error','-y','-i',str(video),'-vf','fps=2,scale=180:-2,tile=4x4','-frames:v','1',str(root/'native-storyboard.png')],check=True)
decoded = subprocess.run([ffmpeg,'-hide_banner','-i',str(video),'-f','null','-'],capture_output=True,text=True)
assert decoded.returncode == 0, decoded.stderr
counts = re.findall(r'frame=\s*(\d+)',decoded.stderr)
assert counts and int(counts[-1]) >= 239, decoded.stderr
assert '720x1280' in decoded.stderr and '30 fps' in decoded.stderr
ready = next(s['elapsed'] for s in samples if s['ready'])
assert 3.15 <= ready <= 3.6, ready
assert len(set(s['frame'] for s in samples)) == len(samples)
metadata = {'source':'Unity ScreenCapture, real-time sampling','seconds':8,'width':720,'height':1280,'fps':30,'decodedFrames':int(counts[-1]),'capturedFrames':len(samples),'firstReadySeconds':ready,'bytes':video.stat().st_size,'decodeErrors':0,'audio':False}
(root/'video-validation.json').write_text(json.dumps(metadata,indent=2),encoding='utf-8')

sheet=Image.new('RGB',(1278,748),'#071b52')
for i,name in enumerate(['native-result','native-stage6','native-stage11']):
    with Image.open(root/f'{name}.png') as image:
        image=image.resize((394,700),Image.Resampling.LANCZOS)
        sheet.paste(image,(24+i*418,24))
sheet.save(root/'palettes.png')

page='''<!doctype html><html lang="ja"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Blend Trails — クリア演出 実装確認</title>
<style>*{box-sizing:border-box}body{margin:0;background:#071631;color:#fff6d8;font:15px/1.7 system-ui,sans-serif}main{max-width:1160px;padding:30px;margin:auto}h1{font-size:clamp(25px,4vw,38px);margin:4px 0 10px}p{color:#c0d6ec;margin:0 0 22px}.tag{color:#82d8ff;font-size:12px;font-weight:700;letter-spacing:.12em}.compare{display:grid;grid-template-columns:1fr 1fr;gap:24px}article{background:#102541;border:1px solid #28405d;border-radius:18px;padding:16px}h2{margin:0 0 12px;font-size:18px}.movie{display:flex;justify-content:center}video{max-width:100%;max-height:66vh;aspect-ratio:9/16;display:block;border-radius:10px;background:#071b52}.controls{display:flex;gap:12px;flex-wrap:wrap;margin:18px 0 26px}button,a{color:#fff6d8;font:inherit}button{background:#164c76;border:1px solid #3fa3d1;border-radius:9px;padding:9px 16px;cursor:pointer}a{color:#8bdcff}section{margin-top:25px}.screens{display:grid;grid-template-columns:repeat(auto-fit,minmax(240px,1fr));gap:20px}img{width:100%;display:block;border-radius:10px}.wide{width:100%;margin-top:16px}.meta{font-size:12px;color:#a5bed8;margin-top:12px}@media(max-width:650px){main{padding:20px 14px}.compare{gap:12px}article{padding:10px}video{max-height:none}h2{font-size:15px}.screens{grid-template-columns:1fr 1fr}}</style>
<main><div class="tag">BLEND TRAILS: COLOR PUZZLE</div><h1>クリア演出 実装確認</h1><p>採用動画の流れをUnityへ実装しました。混色チェック、文字のバウンス、紙吹雪、縮小盤面、次ステージボタンを順に表示します。</p>
<div class="compare"><article><h2>Unityの実画面</h2><div class="movie"><video id="native" src="blend-trails-clear-native.mp4" poster="poster.png" controls loop muted playsinline preload="auto"></video></div><div class="meta">画面録画を8秒のMP4へ書き出し。表示の比較用／音声なし。</div></article><article><h2>採用した動画案</h2><div class="movie"><video id="proposal" src="../ui-clear-motion/blend-trails-clear-proposal.mp4" controls loop muted playsinline preload="auto"></video></div><div class="meta">8秒の動画モックアップ。</div></article></div>
<div class="controls"><button id="replay">両方を最初から再生</button><button id="speed">0.5倍速</button><a href="blend-trails-clear-native.mp4" download>実画面のMP4を保存</a><a href="validation-summary.json">検証結果</a></div>
<p>約3.2秒で次ステージを操作可能にし、見出し・バッジ・星・矢印の待機アニメーションを継続します。全60ステージへ反映し、問題条件は保持しています。</p>
<section><h2>3種類のステージ配色</h2><a href="palettes.png"><img class="wide" src="palettes.png" alt="赤青・赤黄・青黄の実装画面"></a></section>
<section class="screens"><article><h2>縦長の画面</h2><a href="native-tall.png"><img src="native-tall.png" alt="縦長スマートフォンの表示"></a></article><article><h2>タブレットの比率</h2><a href="native-wide.png"><img src="native-wide.png" alt="タブレット比率の表示"></a></article></section>
<section><h2>実画面のコマ送り</h2><a href="native-storyboard.png"><img class="wide" src="native-storyboard.png" alt="Unity演出のコマ送り"></a></section>
</main><script>const videos=[document.getElementById('native'),document.getElementById('proposal')];document.getElementById('replay').onclick=()=>videos.forEach(v=>{v.currentTime=0;v.play()});document.getElementById('speed').onclick=e=>{let rate=videos[0].playbackRate===1?.5:1;videos.forEach(v=>v.playbackRate=rate);e.target.textContent=rate===1?'0.5倍速':'通常速度'};</script></html>'''
(root/'index.html').write_text(page,encoding='utf-8')
print(json.dumps(metadata,ensure_ascii=False))
