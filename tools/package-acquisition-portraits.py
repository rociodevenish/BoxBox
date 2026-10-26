"""Package supplied reserve-driver portraits without modifying the originals."""
import hashlib
import json
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parent.parent
manifest_path = root / "src/frontend/public/portraits/manifest.json"
manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
reserves = {
    "yuki-tsunoda": "Yuki Tsunoda",
    "patricio-oward": "Pato O’Ward",
    "zhou-guanyu": "Zhou Guanyu",
    "stoffel-vandoorne": "Stoffel Vandoorne",
    "antonio-giovinazzi": "Antonio Giovinazzi",
    "jack-doohan": "Jack Doohan",
}
for slug, name in reserves.items():
    source = root / "assets/driver images" / f"{slug}.png"
    runtime = f"portraits/{slug}-384.webp"
    target = root / "src/frontend/public" / runtime
    with Image.open(source) as image:
        image.resize((384, 576), Image.Resampling.LANCZOS).save(target, "WEBP", quality=82)
    manifest["portraits"] = [p for p in manifest["portraits"] if p["driverId"] != slug]
    manifest["portraits"].append({"driverId": slug, "name": name,
        "source": source.relative_to(root).as_posix(),
        "sourceSha256": hashlib.sha256(source.read_bytes()).hexdigest(),
        "url": runtime, "objectPosition": "center 22%", "runtimeUrl": runtime,
        "runtimeSize": {"width": 384, "height": 576, "bytes": target.stat().st_size}})
manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
