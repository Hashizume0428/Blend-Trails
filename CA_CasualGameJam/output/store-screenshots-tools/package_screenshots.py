"""Validate and package twenty native Unity store screenshots."""
from pathlib import Path
import hashlib
import json
import zipfile
from PIL import Image, ImageDraw, ImageFont, ImageStat

TOOLS = Path(__file__).resolve().parent
ROOT = TOOLS.parents[1] / "StoreScreenshots"
manifest = json.loads((TOOLS / "capture-manifest.json").read_text(encoding="utf-8-sig"))
shots = manifest["shots"]
assert len(shots) == 20
assert manifest["endFrame"] > manifest["startFrame"]
assert len(list(ROOT.glob("*.png"))) == 20
hashes = set()
validation = []
font = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 20)
small = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 16)
pad, width, height, caption = 22, 270, 480, 54
sheet = Image.new("RGB", (4*width+5*pad, 5*(height+caption)+6*pad), "#e9eef5")
draw = ImageDraw.Draw(sheet)
for i, shot in enumerate(shots):
    path = ROOT / shot["file"]
    data = path.read_bytes()
    digest = hashlib.sha256(data).hexdigest()
    assert digest not in hashes, f"Duplicate: {path.name}"
    hashes.add(digest)
    with Image.open(path) as image:
        image.load()
        assert image.format == "PNG"
        assert image.size == (1080, 1920)
        assert image.mode == "RGB"
        assert len(data) < 8_000_000
        assert max(ImageStat.Stat(image).stddev) > 15, "Blank screenshot"
        thumb = image.resize((width,height), Image.Resampling.LANCZOS)
        x = pad+(i%4)*(width+pad)
        y = pad+(i//4)*(height+caption+pad)
        sheet.paste(thumb,(x,y))
        draw.text((x,y+height+6), f"{i+1:02d}  {shot['label'].split(' / ')[0]}", font=font, fill="#0c2348")
        draw.text((x,y+height+31), shot["state"].upper(), font=small, fill="#465a76")
    validation.append({"file":path.name,"width":1080,"height":1920,"bytes":len(data),"sha256":digest})

sheet.save(ROOT / "preview.jpg", quality=92, optimize=True)
archive = ROOT.with_suffix(".zip")
with zipfile.ZipFile(archive,"w",zipfile.ZIP_DEFLATED) as package:
    for shot in shots:
        package.write(ROOT / shot["file"], shot["file"])
with zipfile.ZipFile(archive) as package:
    assert len(package.namelist()) == 20
    assert package.testzip() is None
result = {
    "count":20,"dimensions":"1080x1920","format":"PNG","mode":"RGB",
    "uniqueImages":len(hashes),"maxBytes":max(v["bytes"] for v in validation),
    "totalBytes":sum(v["bytes"] for v in validation),"zipBytes":archive.stat().st_size,
    "palettes":sorted({s["palette"] for s in shots if s["palette"]}),
    "allChecksPassed":True,"files":validation,
}
(TOOLS / "validation.json").write_text(json.dumps(result,indent=2),encoding="utf-8")
print(json.dumps({k:v for k,v in result.items() if k!="files"},indent=2))
