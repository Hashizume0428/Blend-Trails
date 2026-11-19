import {createCanvas,loadImage} from 'file:///C:/Users/robor/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas/index.js';
import fs from 'node:fs/promises';
import path from 'node:path';
const sourceRoot = path.resolve('Assets/UI/ToonBold');
for (const file of ['ResetArrowGenerated.png', 'BlendTrailsLogo.png', 'BlendTrailsMix.png']) {
  const image = await loadImage(path.join(sourceRoot,file));
  const info={width:image.width,height:image.height};
  const canvas=createCanvas(info.width,info.height), ctx=canvas.getContext('2d');
  ctx.drawImage(image,0,0);
  const {data}=ctx.getImageData(0,0,info.width,info.height);
  let transparent=0, full=0, edgeMax=0, outside=0;
  for(let y=0;y<info.height;y++) for(let x=0;x<info.width;x++) {
    const alpha=data[(y*info.width+x)*4+3];
    if(alpha===0)transparent++; if(alpha===255)full++;
    if(x===0||y===0||x===info.width-1||y===info.height-1)edgeMax=Math.max(edgeMax,alpha);
    if(alpha>0&&(x<8||y<8||x>=info.width-8||y>=info.height-8))outside++;
  }
  console.log(JSON.stringify({file,width:info.width,height:info.height,transparent,full,edgeMax,outerEightPixelsWithAlpha:outside}));
  const preview=createCanvas(600,Math.round(600*info.height/info.width)), p=preview.getContext('2d');
  p.fillStyle='#2db5f3';p.fillRect(0,0,preview.width,preview.height);p.drawImage(image,0,0,preview.width,preview.height);
  await fs.writeFile(path.resolve('output/ui-polish',file.replace('.png','-on-sky.png')),preview.toBuffer('image/png'));
}
