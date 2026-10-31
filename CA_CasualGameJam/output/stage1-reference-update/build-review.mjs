import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';
import {createCanvas,loadImage,GlobalFonts} from 'file:///C:/Users/robor/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas/index.js';
const root=path.dirname(fileURLToPath(import.meta.url));
const read=async file=>JSON.parse((await fs.readFile(path.join(root,file),'utf8')).replace(/^\uFEFF/,''));
const assert=(value,message)=>{if(!value)throw new Error(message);};
const manual=await read('manual-checks.json');
assert(manual.state==='Cleared'&&manual.correctTiles===20&&manual.nextStage===2,'The supplied paths must clear all 20 tiles and unlock stage 2.');
const cells=color=>manual.cells.filter(c=>c.required===color).map(c=>`${c.row}${c.column}`).sort().join(' ');
assert(cells('Red')==='15 22'&&cells('Blue')==='34 41','Frames must match the supplied image.');
const starts=manual.cells.filter(c=>c.start).map(c=>`${c.startColor}:${c.row}${c.column}`).sort().join(' ');
assert(starts==='Blue:45 Red:11','Starting points must match the supplied image.');
const results=await Promise.all(['editmode-tests.json','tutorial-tests.json','navigation-test.json'].map(read));
const tests=results.map(r=>r.data.result);assert(tests.every(t=>t.status==='completed'&&t.summary.failed===0),'All requested checks must pass.');
const sceneHashes=await read('scene-hashes-before.json'),changed=[];
for(const scene of sceneHashes){const hash=crypto.createHash('sha256').update(await fs.readFile(scene.path)).digest('hex').toUpperCase();if(hash!==scene.hash)changed.push(scene.path);}
assert(changed.length===1&&changed[0].replaceAll('\\','/').endsWith('/SampleScene.unity'),'Only the stage 1 scene may change.');
const restored=await read('restored-editor-state.json');assert(restored.progressRestored&&restored.stateRestored&&!restored.playing,'Restore the original saved progress and Editor state.');
GlobalFonts.registerFromPath('C:/Windows/Fonts/YuGothM.ttc','Yu Gothic');
const canvas=createCanvas(1056,968),ctx=canvas.getContext('2d');ctx.fillStyle='#071631';ctx.fillRect(0,0,1056,968);
ctx.font='bold 26px "Yu Gothic"';ctx.fillStyle='#fff6d8';
for(const[i,[file,label]]of [['stage1-puzzle.png','変更後の盤面'],['stage1-solution.png','画像の正解ルート']].entries()){
  const x=24+i*516;ctx.fillText(label,x+6,44);ctx.drawImage(await loadImage(path.join(root,file)),x,70,492,874.667);
}
await fs.writeFile(path.join(root,'comparison.png'),canvas.toBuffer('image/png'));
const summary={status:'passed',reference:'IMG_0899.PNG',board:{columns:5,rows:4,redStart:[1,1],blueStart:[4,5],redFrames:[[1,5],[2,2]],blueFrames:[[3,4],[4,1]]},tests:{editMode:tests[0].summary,tutorial:tests[1].summary,navigation:tests[2].summary,totalPassed:tests.reduce((sum,t)=>sum+t.summary.passed,0)},manual:{correctTiles:manual.correctTiles,nextStage:manual.nextStage,guideVerified:manual.guideVerified},sceneChanges:changed,otherScenesUnchanged:true,originalProgressRestored:true};
await fs.writeFile(path.join(root,'validation-summary.json'),JSON.stringify(summary,null,2));
await fs.writeFile(path.join(root,'README.md'),'# ステージ1 画像への反映\n\n5列×4行。座標は上から行、左から列で1始まり。赤開始点は1行1列、青開始点は4行5列。赤枠は1行5列・2行2列、青枠は3行4列・4行1列。\n\n赤は上2行、青は下2行を折り返して塗る既存の指ガイドを維持し、実際のガイド経路でクリアできることを確認しました。枠の移動に伴う4マスの条件と枠画像のみを変更しています。\n\nEditMode 48件、チュートリアル7件、ステージ2への遷移1件、合計56件通過。Unity実画面でも全20マスのクリアとStage2への進行を確認済み。検証前のEditorシーン・表示サイズ・進行情報を復元しました。\n\n[盤面と正解ルートの比較](comparison.png) / [指ガイド](stage1-guide.png) / [検証結果](validation-summary.json)\n');
console.log(JSON.stringify({status:'passed',totalTests:summary.tests.totalPassed,changedScenes:changed,correctTiles:manual.correctTiles,nextStage:manual.nextStage,progressRestored:true},null,2));
