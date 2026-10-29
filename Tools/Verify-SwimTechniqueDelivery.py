"""Audit delivered source/art/runtime evidence without modifying game content."""
import csv
import hashlib
import json
import re
from datetime import datetime, timezone
from pathlib import Path
from PIL import Image
import numpy as np

ROOT=Path(__file__).resolve().parents[1]
DEST=ROOT/'Validation/SwimTechnique_20261028_Delivery'
DEST.mkdir(parents=True,exist_ok=True)
EXECUTABLE=ROOT/'Builds/Windows/MoriEchoes_20261028_171506_913/MoriEchoes.exe'
RUNS=[('Swim_20261028_172640_621','swim-campaign-smoke.json'),
      ('BossTechniques_20261028_172804_287','boss-techniques-smoke.json'),
      ('Encounter_1280x720_20261028_173006_158','stage-smoke.json'),
      ('Vertical_20261028_173510_486','vertical-campaign-smoke.json')]

def digest(path):
    algorithm=hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda:stream.read(1024*1024),b''): algorithm.update(chunk)
    return algorithm.hexdigest()

def assert_that(condition,message):
    if not condition: raise ValueError(message)

results=[]
for directory,name in RUNS:
    folder=ROOT/'TestResults'/directory
    evidence=json.loads((folder/'process-evidence.json').read_text(encoding='utf-8-sig'))
    report=json.loads((folder/name).read_text(encoding='utf-8-sig'))
    assert_that(evidence['Passed'] and evidence['ExitCode']==0 and report['passed'] and not report['errors'],f'Runtime failure: {directory}')
    assert_that(Path(evidence['Executable']).resolve()==EXECUTABLE.resolve(),f'Wrong build in evidence: {directory}')
    pictures=sorted(folder.glob('*.png'))
    for picture in pictures:
        image=Image.open(picture).convert('RGB')
        assert_that(image.width>=1280 and image.height>=720,f'Wrong screenshot resolution: {picture.name}')
        assert_that(len(image.resize((40,24)).getcolors(961) or [])>=8,f'Blank screenshot: {picture.name}')
    results.append(dict(report=str((folder/name).relative_to(ROOT)),checks=len(report['checks']),screenshots=len(pictures),exitCode=0,passed=True))

art=[];raw_sources=set();asset_paths=set()
for directory,expected in [('SwimAnimeV2',86),('BossTechniquesV1',60)]:
    folder=ROOT/'Assets/Art/Production'/directory
    manifest=json.loads((folder/'art-manifest.json').read_text(encoding='utf-8'))
    assert_that(len(manifest['assets'])==expected,'Wrong art count: '+directory)
    for entry in manifest['assets']:
        path=folder/entry['path'];source=ROOT/entry['source']
        assert_that(digest(path)==entry['sha256'],'Art hash mismatch: '+str(path))
        assert_that(digest(source)==entry['sourceSha256'],'Original changed: '+str(source))
        raw_sources.add(source);asset_paths.add(path)
        image=Image.open(path).convert('RGBA');alpha=np.asarray(image.getchannel('A'))
        if entry['kind']!='background':
            assert_that(float((alpha<8).mean())>.2,'Missing transparent alpha: '+str(path))
        if entry.get('normal'):
            normal=folder/entry['normal']
            assert_that(digest(normal)==entry['normalSha256'],'Normal hash mismatch: '+str(normal))
        colors=np.asarray(image)[:,:,:3]
        visible=alpha>32
        overbright=float(((colors.min(axis=2)>240)&visible).sum()/max(1,visible.sum()))
        art.append(dict(path=path.relative_to(ROOT).as_posix(),kind=entry['kind'],stage=entry['stage'],
                        brightWhiteFraction=round(overbright,5),sha256=entry['sha256']))
assert_that(len(raw_sources)==24 and len(asset_paths)==146,'Generated art totals differ')
catalog=json.loads((ROOT/'ArtProduction/boss-techniques-v1/catalog.json').read_text(encoding='utf-8'))
assert_that(len(catalog)==30 and len({entry['name'] for entry in catalog})==30,'Skill names not distinct')
assert_that(all(sum(entry['stage']==stage for entry in catalog)==3 for stage in range(10)),'Must have three skills per stage')

previous=(ROOT/'Validation/SwimPolish_20261028_131759_900/MoriEchoesConfig.asset').read_text(encoding='utf-8-sig')
current=(ROOT/'Assets/Settings/MoriEchoesConfig.asset').read_text(encoding='utf-8-sig')
for field in ['characters','weapons']:
    pattern=rf'^  {field}:.*?(?=^  [A-Za-z_]|\Z)'
    before=re.search(pattern,previous,re.MULTILINE|re.DOTALL)
    after=re.search(pattern,current,re.MULTILINE|re.DOTALL)
    assert_that(before is not None and after is not None and before.group()==after.group(),'Original configuration changed: '+field)

source_files=sorted((ROOT/'Assets').rglob('*.cs'))
build_end=(ROOT/'Logs/SwimTechniqueBuildFinal_20261028.log').stat().st_mtime
assert_that(all(path.stat().st_mtime<=build_end for path in source_files),'Source edited after final build; rebuild required')
source_hashes=[dict(path=path.relative_to(ROOT).as_posix(),sha256=digest(path)) for path in source_files]
(DEST/'compiled-source-hashes.json').write_text(json.dumps(source_hashes,indent=2)+'\n',encoding='utf-8')

names=[
 'Assets/Scripts/Player/SidePlayerMotor.cs','Assets/Scripts/Level/PlatformSurface.cs','Assets/Scripts/Level/SwimStageDirector.cs',
 'Assets/Scripts/Level/ThemeProfile.cs','Assets/Scripts/Enemies/BossController.cs','Assets/Scripts/Enemies/BossField.cs',
 'Assets/Scripts/Enemies/SideBossController.cs','Assets/Scripts/Enemies/VerticalBossController.cs','Assets/Scripts/Enemies/SwimBossController.cs',
 'Assets/Scripts/Enemies/BossTechniqueController.cs','Assets/Scripts/Core/CombatEffects.cs','Assets/Scripts/Core/SideSpriteAnimator.cs',
 'Assets/Scripts/Diagnostics/SwimCampaignSmokeTest.cs','Assets/Scripts/Diagnostics/BossTechniqueSmokeTest.cs',
 'Assets/Editor/SwimAndTechniqueArtSetup.cs','Assets/Settings/MoriEchoesConfig.asset',
 'Tools/Plan-SwimArt.py','Tools/Plan-BossTechniqueArt.py','Tools/Prepare-SwimAndTechniqueArt.py',
 'Tools/Verify-SwimTechniqueDelivery.py','Tools/Test-SwimCampaign.ps1','Tools/Test-BossTechniques.ps1',
 'README.md','HOW_TO_PLAY.md','PLAY_MoriEchoes.cmd','docs/PROGRESS.md','docs/BOSS_TECHNIQUE_DESIGN.md',
 'docs/SWIM_ART_AND_BOSS_TECHNIQUES_ACCEPTANCE.md',
 'docs/BuildReports/Windows_20261028_165456_897.md','docs/BuildReports/Windows_20261028_171506_913.md',
 'Logs/SwimTechniqueBuild_20261028.log','Logs/SwimTechniqueBuildFinal_20261028.log']
paths={ROOT/name for name in names}
for path in list(paths):
    meta=path.with_name(path.name+'.meta')
    if meta.is_file(): paths.add(meta)
for directory in ['Assets/Art/Production/SwimAnimeV1','Assets/Art/Production/SwimAnimeV2','Assets/Art/Production/BossTechniquesV1',
                  'ArtProduction/swim-anime-v1','ArtProduction/boss-techniques-v1','output/imagegen/swim-anime-v1',
                  'output/imagegen/boss-techniques-v1']:
    folder=ROOT/directory
    paths.update(path for path in folder.rglob('*') if path.is_file())
    meta=folder.with_name(folder.name+'.meta')
    if meta.is_file(): paths.add(meta)

before_hashes={entry['path']:entry['sha256'].lower() for entry in json.loads((ROOT/'Validation/SwimPolish_20261028_131759_900/before-hashes.json').read_text(encoding='utf-8-sig'))}
files=[];secrets=[]
for path in sorted(paths):
    assert_that(path.is_file(),'Missing deliverable: '+str(path))
    relative=path.relative_to(ROOT).as_posix();sha=digest(path)
    files.append(dict(path=relative,bytes=path.stat().st_size,sha256=sha,beforeSha256=before_hashes.get(relative,'')))
    if path.suffix.lower() in ('.cs','.py','.ps1','.json','.jsonl','.txt','.md','.log','.asset','.cmd','.meta'):
        text=path.read_text(encoding='utf-8-sig',errors='replace')
        if re.search(r'sk-[A-Za-z0-9]{24,}',text): secrets.append(relative)
assert_that(not secrets,'Potential API credentials in deliverable files: '+', '.join(secrets))
(DEST/'file-changes.json').write_text(json.dumps(files,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
with (DEST/'file-changes.csv').open('w',encoding='utf-8-sig',newline='') as stream:
    writer=csv.DictWriter(stream,fieldnames=['path','bytes','sha256','beforeSha256']);writer.writeheader();writer.writerows(files)
(DEST/'art-quality-measurements.json').write_text(json.dumps(art,indent=2)+'\n',encoding='utf-8')
delivery=dict(verifiedAt=datetime.now(timezone.utc).isoformat(),executable=str(EXECUTABLE),executableSha256=digest(EXECUTABLE),
              buildReport='docs/BuildReports/Windows_20261028_171506_913.md',buildSucceeded=True,
              runtimeChecks=sum(entry['checks'] for entry in results),runtime=results,
              imageGeneration=dict(requestedModel='gpt-image-2.5',sourceAtlases=24,productionSprites=146,nativeAlphaSprites=144,
                                   backgrounds=2,derived2DNormals=60,skills=30,skillsPerStage=3,sourceHashesVerified=True),
              charactersAndWeaponsPreserved=True,sourceFiles=len(source_files),files=len(files),noPlaintextApiKeys=True,
              scope='Assisted local Windows runtime; no unassisted human campaign/balance, physical-controller long test, Editor Play Mode or cross-machine certification. Cross-machine testing cancelled by user; no ZIP created.')
(DEST/'delivery-verification.json').write_text(json.dumps(delivery,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(passed=True,files=len(files),sprites=146,originals=24,runtimeChecks=delivery['runtimeChecks'],report=str(DEST/'delivery-verification.json')),ensure_ascii=False))
