# Content asset packaging

Run from the repository root with Python 3.12, Pillow 12.3.0 and libwebp 1.6.0. `content-assets-requirements.txt` pins Pillow; the CLI also checks the encoder version because encoded bytes define immutable URLs.

```powershell
python -m pip install -r tools/content-assets-requirements.txt
python tools/package-content-assets.py --compare-root C:/Projects/F1Game
python tools/package-content-assets.py --check --compare-root C:/Projects/F1Game
python -m unittest discover -s tools/tests -p test_content_assets.py
```

`content-assets.sources.json` is the reviewed authoring registry. It explicitly maps stable identities to source hashes and pins retained legacy files. New or changed media require an explicit identity/provenance review and registry update. Never infer a person or rights from a filename. `--compare-root` reads another checkout's current originals and rejects unreviewed additions or changes.

The public output is `src/frontend/public/content/manifest.json`, an immutable `manifest-<sha256>.json` copy, and `<sha256>.webp` files. The manifest schema is `content-assets-v1`; each asset has only assetId, entityId, runtimeUrl, objectPosition, width, height, status. Backend consumers may embed the exact safe JSON at build time. Runtime consumers need no authoring paths or repository filesystem. Available URLs use the complete encoded-byte SHA256. Reserved fallback rows use null runtimeUrl and zero dimensions; missing entities can use local readable text/initials. A consuming release must freeze its selected manifest and retain old URLs.

Portraits contain within 384x576; circuits within 960x640. Encoding uses LANCZOS, no crop/upscale, WebP quality82/method4, exact RGB for transparent pixels, alpha quality100; EXIF/ICC/XMP are removed. Alpha is compared byte-for-byte after decoding against the resized input. Current sources have no colour/EXIF metadata. Public directory attributes disable Git text conversions so immutable JSON hashes survive Windows checkouts.

Originals and existing legacy assets are never rewritten. Existing immutable files must match their names/content and are retained when new variants are generated. All validation completes before public writes. Authoring output is confined to private .scratch/.tools paths; runtime output cannot target original or legacy directories.

Private audit options: `--audit <json>`, `--contact-sheets <directory>`, repeated `--review-asset <assetId>`. The default audit holds full source/legacy hashes, provenance/rights limitations, dimensions, alpha measurements and encoder settings. Contact sheets compare each original (left) with its runtime image (right); individual reviews include dark and contrasting alpha backgrounds. These files are evidence, never a runtime payload.
