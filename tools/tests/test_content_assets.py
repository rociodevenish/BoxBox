"""Public CLI acceptance: real temporary files, images, subprocesses and safe manifests."""
import hashlib,json,shutil,subprocess,sys,tempfile,unittest
from pathlib import Path
from PIL import Image,ImageDraw
SCRIPT=Path(__file__).resolve().parents[1]/'package-content-assets.py'
class ContentAssetCliTests(unittest.TestCase):
 def setUp(self):
  base=Path(__file__).resolve().parents[2]/'.tools/asset-cli-fixtures';base.mkdir(parents=True,exist_ok=True);self.temp=tempfile.TemporaryDirectory(dir=base);self.assertTrue(Path(self.temp.name).resolve().is_relative_to(base.resolve()));self.addCleanup(self.temp.cleanup);self.root=Path(self.temp.name)/'repo';self.root.mkdir();self.output=self.root/'src/frontend/public/content';self.registry=self.root/'sources.json'
  for folder in ['assets/driver images','.docs/data','src/frontend/public/portraits','src/frontend/public/tracks']:(self.root/folder).mkdir(parents=True)
  (self.root/'.docs/data/f1_strategy_driver_cards_2026.json').write_text(json.dumps({'drivers':[{'id':'driver-fixture','identity':{'displayName':'Fixture Driver'}}]}))
  (self.root/'.docs/data/f1_strategy_track_cards_2026.json').write_text(json.dumps({'tracks':[]}))
  self.source=self.root/'assets/driver images/fixture.png';im=Image.new('RGBA',(768,1152));ImageDraw.Draw(im).ellipse((96,96,672,1056),fill=(70,140,220,255));im.save(self.source)
  self.row={'assetId':'asset-driver-fixture','entityId':'driver-fixture','name':'Fixture Driver','kind':'portrait','source':'assets/driver images/fixture.png','sourceSha256':hashlib.sha256(self.source.read_bytes()).hexdigest(),'objectPosition':'center 22%','status':'available','provenance':'Explicit synthetic CLI fixture mapping','rights':'Generated test fixture','intendedUse':'Test only'}
  self.write_registry();self.original=self.source.read_bytes();self.legacy=self.root/'src/frontend/public/portraits/legacy.webp';self.legacy.write_bytes(b'unchanged-legacy-fixture')
 def write_registry(self):self.registry.write_text(json.dumps({'schemaVersion':'content-asset-sources-v1','sources':[self.row]}))
 def run_cli(self,*extra,success=True):
  result=subprocess.run([sys.executable,str(SCRIPT),'--source-root',str(self.root),'--registry',str(self.registry),'--output',str(self.output),'--audit',str(self.root/'.tools/audit.json'),*extra],capture_output=True,text=True,timeout=30)
  if success:self.assertEqual(0,result.returncode,result.stderr)
  else:self.assertNotEqual(0,result.returncode)
  return result
 def test_generation_is_safe_alpha_preserving_reproducible_and_leaves_originals_and_legacy_bytes(self):
  self.run_cli();manifest=(self.output/'manifest.json').read_bytes();asset=json.loads(manifest)['assets'][0]
  self.assertEqual({'assetId','entityId','runtimeUrl','objectPosition','width','height','status'},set(asset));self.assertNotIn('provenance',manifest.decode());self.assertNotIn('source',manifest.decode())
  path=self.output/Path(asset['runtimeUrl']).name;self.assertEqual(hashlib.sha256(path.read_bytes()).hexdigest()+'.webp',path.name)
  with Image.open(path) as im:self.assertEqual((384,576),im.size);self.assertEqual(0,im.convert('RGBA').getpixel((0,0))[3]);self.assertEqual(255,im.convert('RGBA').getpixel((192,288))[3])
  before={p.name:p.read_bytes() for p in self.output.iterdir()};self.run_cli();self.run_cli('--check');self.assertEqual(before,{p.name:p.read_bytes() for p in self.output.iterdir()})
  self.assertEqual(self.original,self.source.read_bytes());self.assertEqual(b'unchanged-legacy-fixture',self.legacy.read_bytes())
 def test_unmapped_source_image_is_rejected_before_any_public_output(self):
  Image.new('RGB',(20,20),'red').save(self.root/'assets/driver images/unmapped.png')
  result=self.run_cli(success=False);self.assertIn('Unmapped source image',result.stderr);self.assertFalse(self.output.exists())
 def test_unsafe_paths_and_unknown_identities_do_not_create_public_files(self):
  original=dict(self.row)
  for change in [{'source':'../outside.png'},{'entityId':'driver-unverified','assetId':'asset-driver-unverified'}]:
   with self.subTest(change=change):
    self.row=original|change;self.write_registry();self.run_cli(success=False);self.assertFalse(self.output.exists())
 def test_authoring_audit_cannot_overwrite_an_original_or_publish_private_metadata(self):
  for target in [self.source,self.root/'src/frontend/public/content/private-audit.json']:
   with self.subTest(target=target):
    self.run_cli('--audit',str(target),success=False);self.assertEqual(self.original,self.source.read_bytes());self.assertFalse(self.output.exists())
 def test_approved_source_change_creates_new_url_and_retains_every_old_byte_and_manifest(self):
  self.run_cli();old=json.loads((self.output/'manifest.json').read_text())['assets'][0];old_path=self.output/Path(old['runtimeUrl']).name;old_bytes=old_path.read_bytes();snapshots={p.name:p.read_bytes() for p in self.output.glob('manifest-*.json')}
  with Image.open(self.source) as im:
   changed=im.copy();ImageDraw.Draw(changed).rectangle((200,200,500,800),fill=(30,210,60,255));changed.save(self.source)
  self.row['sourceSha256']=hashlib.sha256(self.source.read_bytes()).hexdigest();self.write_registry();self.run_cli();new=json.loads((self.output/'manifest.json').read_text())['assets'][0]
  self.assertEqual(old['assetId'],new['assetId']);self.assertNotEqual(old['runtimeUrl'],new['runtimeUrl']);self.assertEqual(old_bytes,old_path.read_bytes());self.assertTrue(all((self.output/name).read_bytes()==value for name,value in snapshots.items()))
 def test_corrupted_immutable_variant_is_rejected_without_silent_overwrite(self):
  self.run_cli();asset=json.loads((self.output/'manifest.json').read_text())['assets'][0];path=self.output/Path(asset['runtimeUrl']).name;path.write_bytes(b'corrupted-fixture')
  self.run_cli(success=False);self.assertEqual(b'corrupted-fixture',path.read_bytes());self.assertEqual(self.original,self.source.read_bytes())
 def test_current_primary_additions_and_changes_require_explicit_review(self):
  primary=Path(self.temp.name)/'primary';shutil.copytree(self.root,primary);self.run_cli('--compare-root',str(primary));before=(self.output/'manifest.json').read_bytes()
  Image.new('RGB',(20,20),'red').save(primary/'assets/unmapped-later.png');result=self.run_cli('--compare-root',str(primary),success=False);self.assertIn('Unmapped primary image',result.stderr);self.assertEqual(before,(self.output/'manifest.json').read_bytes())
 def test_changed_original_requires_new_reviewed_hash_before_runtime_can_change(self):
  self.run_cli();before={p.name:p.read_bytes() for p in self.output.iterdir()}
  Image.new('RGBA',(768,1152),(10,40,120,100)).save(self.source);result=self.run_cli(success=False)
  self.assertIn('Original hash changed',result.stderr);self.assertEqual(before,{p.name:p.read_bytes() for p in self.output.iterdir()})
 def test_reviewed_legacy_bytes_must_match_before_packaging_a_new_manifest(self):
  data=json.loads(self.registry.read_text());data['retainedLegacyFiles']=[{'path':'src/frontend/public/portraits/legacy.webp','sha256':hashlib.sha256(self.legacy.read_bytes()).hexdigest(),'bytes':self.legacy.stat().st_size}];self.registry.write_text(json.dumps(data));self.legacy.write_bytes(b'legacy-was-changed')
  result=self.run_cli(success=False);self.assertIn('Retained legacy bytes changed',result.stderr);self.assertFalse(self.output.exists())
class RepositoryAssetBytesTests(unittest.TestCase):
 def test_hash_addressed_json_keeps_exact_bytes_in_windows_style_git_checkout(self):
  root=Path(__file__).resolve().parents[2];snapshot=next((root/'src/frontend/public/content').glob('manifest-*.json'));relative=snapshot.relative_to(root).as_posix()
  result=subprocess.run(['git','-c','core.autocrlf=true','cat-file','--filters','--path='+relative,'HEAD:'+relative],cwd=root,capture_output=True,timeout=15)
  self.assertEqual(0,result.returncode,result.stderr.decode(errors='replace'));self.assertEqual(snapshot.stem.removeprefix('manifest-'),hashlib.sha256(result.stdout).hexdigest())
if __name__=='__main__':unittest.main()
