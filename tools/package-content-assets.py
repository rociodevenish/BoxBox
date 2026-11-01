"""Deterministic, manifest-driven runtime assets. Originals and retained URLs are never rewritten."""
import argparse,hashlib,io,json,math,re,sys
from pathlib import Path
import PIL
from PIL import Image,ImageOps,ImageDraw,ImageFont,features
ROOT=Path(__file__).resolve().parent.parent
SETTINGS={'format':'WEBP','quality':82,'method':4,'lossless':False,'exact':True,'alpha_quality':100,'resize':'LANCZOS contain; no crop or upscale','metadata':'EXIF/ICC/XMP removed'}
def digest(data):return hashlib.sha256(data).hexdigest()
def encoded_json(data):return (json.dumps(data,ensure_ascii=False,indent=2,sort_keys=True)+'\n').encode('utf8')
def relative_source(root,value):
 p=Path(value)
 if p.is_absolute() or '..' in p.parts or not p.parts or p.parts[0]!='assets':raise ValueError('Source path must stay within assets: '+value)
 resolved=(root/p).resolve()
 if not resolved.is_relative_to(root/'assets'):raise ValueError('Source path escapes assets: '+value)
 return resolved

def private_output(root,path):
 resolved=path.resolve()
 if not resolved.is_relative_to(root) or not resolved.relative_to(root).parts or resolved.relative_to(root).parts[0] not in {'.scratch','.tools'}:raise ValueError('Authoring output must stay in private .scratch or .tools, never originals/public runtime paths.')
 return resolved

def image_paths(root):
 return {p.relative_to(root).as_posix() for p in (root/'assets').rglob('*') if p.is_file() and p.suffix.lower() not in {'.md','.txt','.json','.license'}}

def background(size,striped):
 im=Image.new('RGBA',size,'#101214')
 if striped:
  draw=ImageDraw.Draw(im)
  for x in range(-size[1],size[0]+size[1],124):
   draw.polygon([(x,0),(x+32,0),(x+size[1]+32,size[1]),(x+size[1],size[1])],fill='#64676a')
   draw.polygon([(x+62,0),(x+94,0),(x+size[1]+94,size[1]),(x+size[1]+62,size[1])],fill='#d84d49')
 return im

def preview(path,size,striped=False):
 with Image.open(path) as image:
  image=ImageOps.contain(image.convert('RGBA'),size,Image.Resampling.LANCZOS);panel=background(size,striped);panel.alpha_composite(image,((size[0]-image.width)//2,(size[1]-image.height)//2));return panel.convert('RGB')

def contact_sheets(root,output,directory,planned,review_assets):
 directory=private_output(root,directory);directory.mkdir(parents=True,exist_ok=True)
 try:font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',14);small=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',12)
 except OSError:font=small=ImageFont.load_default()
 for kind,per_page,tile in [('portrait',14,(132,198)),('circuit',8,(192,128))]:
  rows=[p for p in planned if p[1] is not None and p[2]['kind']==kind];pair_width=tile[0]*2+24;row_height=tile[1]+58
  for page,start in enumerate(range(0,len(rows),per_page),1):
   selected=rows[start:start+per_page];sheet=Image.new('RGB',(pair_width*2+32,math.ceil(len(selected)/2)*row_height+74),'#17191b');draw=ImageDraw.Draw(sheet);draw.text((16,12),f'{kind.title()} original / runtime pairs — page {page}',font=font,fill='#f5f6f7');draw.text((16,35),'Contain; alpha preserved. Illustrations are context, never model geometry.',font=small,fill='#b5bdc4')
   labels=[]
   def wrapped(text,typeface):
    lines=['']
    for word in text.split():
     candidate=(lines[-1]+' '+word).strip()
     if lines[-1] and draw.textlength(candidate,font=typeface)>pair_width-16:lines.append(word)
     else:lines[-1]=candidate
    return lines
   for safe,_,row in selected:labels.append((wrapped(row['name'],font),wrapped(safe['entityId'],small)))
   label_height=max(len(a)*16+len(b)*14+8 for a,b in labels);row_height=tile[1]+label_height+20
   sheet=Image.new('RGB',(pair_width*2+32,math.ceil(len(selected)/2)*row_height+74),'#17191b');draw=ImageDraw.Draw(sheet);draw.text((16,12),f'{kind.title()} original / runtime pairs — page {page}',font=font,fill='#f5f6f7');draw.text((16,35),'Contain; alpha preserved. Illustrations are context, never model geometry.',font=small,fill='#b5bdc4')
   for i,(safe,_,row) in enumerate(selected):
    x=16+(i%2)*pair_width;y=66+(i//2)*row_height;text_y=y
    for line in labels[i][0]:draw.text((x,text_y),line,font=font,fill='#f5f6f7');text_y+=16
    for line in labels[i][1]:draw.text((x,text_y),line,font=small,fill='#b5bdc4');text_y+=14
    sheet.paste(preview(root/row['source'],tile,kind=='portrait'),(x,y+label_height));sheet.paste(preview(output/Path(safe['runtimeUrl']).name,tile,kind=='portrait'),(x+tile[0]+8,y+label_height))
   sheet.save(directory/f'{kind}-pairs-{page:02}.png')
 for asset_id in review_assets:
  matches=[p for p in planned if p[0]['assetId']==asset_id and p[1] is not None]
  if len(matches)!=1:raise ValueError('Unknown individual review asset: '+asset_id)
  safe,_,row=matches[0];size=(384,576) if row['kind']=='portrait' else (600,400);sheet=Image.new('RGB',(size[0]*2+48,size[1]*2+112),'#17191b');draw=ImageDraw.Draw(sheet);draw.text((16,12),row['name']+' — original / runtime',font=font,fill='#f5f6f7')
  for i,striped in enumerate([False,True]):
   y=48+i*(size[1]+24);sheet.paste(preview(root/row['source'],size,striped),(16,y));sheet.paste(preview(output/Path(safe['runtimeUrl']).name,size,striped),(size[0]+32,y))
  sheet.save(directory/(asset_id+'-review.png'))

def build(args):
 if PIL.__version__!='12.3.0' or features.version('webp')!='1.6.0':raise ValueError('Reproduction requires Pillow 12.3.0 / libwebp 1.6.0; install the pinned toolchain.')
 root=args.source_root.resolve();output=args.output.resolve();registry=json.loads(args.registry.read_text(encoding='utf-8-sig'))
 audit_path=private_output(root,args.audit)
 if audit_path==args.registry.resolve() or audit_path.is_relative_to(output):raise ValueError('Authoring audit must not overwrite an input or runtime artifact.')
 if output!=root/'src/frontend/public/content' and not output.is_relative_to(root/'.tools'):raise ValueError('Runtime output must be content or an owned .tools staging directory.')
 if args.contact_sheets:private_output(root,args.contact_sheets)
 if output.is_relative_to(root/'assets') or any(output.is_relative_to(root/'src/frontend/public'/p) for p in ['portraits','tracks']):raise ValueError('Output must not modify originals or legacy asset directories.')
 if registry.get('schemaVersion')!='content-asset-sources-v1':raise ValueError('Unsupported authoring registry schema.')
 for legacy in registry.get('retainedLegacyFiles',[]):
  relative=Path(legacy['path']);path=(root/relative).resolve()
  if relative.is_absolute() or '..' in relative.parts or not any(path.is_relative_to(root/'src/frontend/public'/part) for part in ['portraits','tracks']):raise ValueError('Legacy compatibility path must remain in a retained legacy directory.')
  if digest(path.read_bytes())!=legacy['sha256']:raise ValueError('Retained legacy bytes changed; restore the compatible asset: '+legacy['path'])
 mapped=[relative_source(root,r['source']).relative_to(root).as_posix() for r in registry['sources'] if r.get('source')]
 if len(mapped)!=len(set(mapped)):raise ValueError('One supplied image cannot silently represent multiple identities.')
 discovered=image_paths(root)
 unmapped=sorted(discovered-set(mapped))
 if unmapped:raise ValueError('Unmapped source image requires explicit identity/provenance review: '+', '.join(unmapped))
 comparison='not requested'
 if args.compare_root:
  primary=args.compare_root.resolve();primary_files=image_paths(primary);unknown=sorted(primary_files-set(mapped))
  if unknown:raise ValueError('Unmapped primary image requires explicit identity/provenance review: '+', '.join(unknown))
  if set(mapped)-primary_files:raise ValueError('Reviewed image missing from current primary collection.')
  for row in registry['sources']:
   if row.get('source') and digest(relative_source(primary,row['source']).read_bytes())!=row['sourceSha256']:raise ValueError('Primary image differs from reviewed source; identity/provenance review required: '+row['assetId'])
  comparison='all current primary images match reviewed explicit mappings and hashes'
 known={d['id'] for d in json.loads((root/'.docs/data/f1_strategy_driver_cards_2026.json').read_text(encoding='utf8'))['drivers']}|{t['id'] for t in json.loads((root/'.docs/data/f1_strategy_track_cards_2026.json').read_text(encoding='utf8'))['tracks']}
 planned=[];seen=set();entities=set()
 for row in sorted(registry['sources'],key=lambda r:r['assetId']):
  asset=row['assetId'];entity=row['entityId']
  if not re.fullmatch(r'[a-z0-9]+(?:-[a-z0-9]+)*',entity) or asset!='asset-'+entity or asset in seen or entity in entities:raise ValueError('Asset/entity identities must be stable, distinct and explicitly mapped.')
  seen.add(asset);entities.add(entity)
  position=row['objectPosition']
  if not re.fullmatch(r'(?:left|center|right|(?:100|[0-9]{1,2})%) (?:top|center|bottom|(?:100|[0-9]{1,2})%)',position):raise ValueError('Unsupported bounded objectPosition for '+asset)
  if row.get('status')=='fallback':
   planned.append((dict(assetId=asset,entityId=entity,runtimeUrl=None,objectPosition=position,width=0,height=0,status='fallback'),None,dict(row)));continue
  if row.get('status')!='available' or entity not in known or row['kind'] not in ['portrait','circuit']:raise ValueError('Unknown image identity or kind: '+entity)
  if (row['kind']=='portrait')!=entity.startswith('driver-'):raise ValueError('Image kind does not match its explicit entity identity: '+entity)
  if not row.get('provenance') or not row.get('rights'):raise ValueError('Authoring provenance/rights status required: '+asset)
  path=relative_source(root,row['source']);original=path.read_bytes()
  if digest(original)!=row['sourceSha256']:raise ValueError('Original hash changed; review the identity/provenance before adoption: '+asset)
  with Image.open(path) as source:
   source.load();mode=source.mode;source_size=source.size;im=ImageOps.exif_transpose(source).convert('RGBA')
   bounds=(384,576) if row['kind']=='portrait' else (960,640);scale=min(1,bounds[0]/im.width,bounds[1]/im.height)
   resized=im.resize((round(im.width*scale),round(im.height*scale)),Image.Resampling.LANCZOS);resized.info={}
   data=io.BytesIO();resized.save(data,'WEBP',quality=82,method=4,lossless=False,exact=True,alpha_quality=100,icc_profile=b'',exif=b'',xmp=b'');variant=data.getvalue()
   with Image.open(io.BytesIO(variant)) as decoded:
    alpha_exact=decoded.convert('RGBA').getchannel('A').tobytes()==resized.getchannel('A').tobytes()
    if not alpha_exact:raise ValueError('Resized alpha changed during WebP encoding: '+asset)
    alpha=decoded.convert('RGBA').getchannel('A');runtime_alpha=alpha.getextrema();transparent_fraction=alpha.histogram()[0]/(alpha.width*alpha.height)
   sha=digest(variant);safe=dict(assetId=asset,entityId=entity,runtimeUrl='/content/'+sha+'.webp',objectPosition=position,width=resized.width,height=resized.height,status='available')
   audit=dict(row,originalWidth=source_size[0],originalHeight=source_size[1],originalMode=mode,originalMetadataKinds=sorted(source.info),originalAlphaExtrema=im.getchannel('A').getextrema(),runtimeSha256=sha,runtimeBytes=len(variant),runtimeWidth=resized.width,runtimeHeight=resized.height,runtimeAlphaExtrema=runtime_alpha,resizedAlphaPreservedExactly=alpha_exact,transparentPixelFraction=transparent_fraction,settings=SETTINGS)
   planned.append((safe,variant,audit))
 manifest=encoded_json(dict(schemaVersion='content-assets-v1',assets=[p[0] for p in planned]));snapshot='manifest-'+digest(manifest)+'.json'
 files={Path(p[0]['runtimeUrl']).name:p[1] for p in planned if p[1] is not None}|{snapshot:manifest,'.gitattributes':b'* -text\n'}
 # Inspect all existing immutable destinations before writing anything or changing the build alias.
 for name,data in files.items():
  destination=output/name
  if not destination.resolve().is_relative_to(output):raise ValueError('Runtime destination escapes its owned directory.')
  if destination.exists() and destination.read_bytes()!=data:raise ValueError('Refusing to overwrite corrupted immutable runtime asset: '+name)
 for name in ['manifest.json','manifest.json.tmp']:
  if not (output/name).resolve().is_relative_to(output):raise ValueError('Manifest alias escapes its owned runtime directory.')
 if args.check:
  if not (output/'manifest.json').exists() or (output/'manifest.json').read_bytes()!=manifest:raise ValueError('Current safe manifest is not reproducible from reviewed sources/toolchain.')
  if any(not (output/name).exists() for name in files):raise ValueError('Missing immutable runtime file.')
 else:
  output.mkdir(parents=True,exist_ok=True)
  for name,data in files.items():
   if not (output/name).exists():
    with (output/name).open('xb') as stream:stream.write(data)
  temporary=output/'manifest.json.tmp';temporary.write_bytes(manifest);temporary.replace(output/'manifest.json')
  audit_path.parent.mkdir(parents=True,exist_ok=True)
  audit_path.write_bytes(encoded_json(dict(schemaVersion='content-asset-audit-v1',toolVersions=dict(python=sys.version.split()[0],pillow=PIL.__version__,libwebp=features.version('webp')),primaryComparison=comparison,safeManifestSha256=digest(manifest),retainedManifestUrl='/content/'+snapshot,retainedLegacyFiles=registry.get('retainedLegacyFiles',[]),sources=[p[2] for p in planned])))
 if args.contact_sheets:contact_sheets(root,output,args.contact_sheets,planned,args.review_asset)
 print(f"{'Checked' if args.check else 'Packaged'} {len(planned)} explicit assets; immutable manifest /content/{snapshot}")

def main():
 parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--source-root',type=Path,default=ROOT);parser.add_argument('--registry',type=Path,default=ROOT/'tools/content-assets.sources.json');parser.add_argument('--output',type=Path,default=ROOT/'src/frontend/public/content');parser.add_argument('--audit',type=Path,default=ROOT/'.scratch/full-calendar-content/evidence/04-assets/asset-inventory.json');parser.add_argument('--check',action='store_true');parser.add_argument('--compare-root',type=Path);parser.add_argument('--contact-sheets',type=Path);parser.add_argument('--review-asset',action='append',default=[]);args=parser.parse_args()
 try:build(args)
 except (ValueError,OSError,KeyError,TypeError) as error:parser.exit(1,'Content asset packaging rejected: '+str(error)+'\n')
if __name__=='__main__':main()
