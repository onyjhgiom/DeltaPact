"""Verify one built player's pursuit, targeting and zero-HP runtime evidence."""
import csv
import hashlib
import json
import re
from datetime import datetime, timezone
from pathlib import Path
from PIL import Image

ROOT=Path(__file__).resolve().parents[1]
DEST=ROOT/'Validation/BossPursuit_20261029_Delivery'
DEST.mkdir(parents=True,exist_ok=True)
EXE=ROOT/'Builds/Windows/MoriEchoes_20261029_173048_021/MoriEchoes.exe'
BACKUP=ROOT/'Validation/BossPursuit_20261029_092628_002'
LOG=ROOT/'Logs/BossPursuitZeroHPFixBuild_20261029.log'
SPECS=[('BossPursuit_','boss-pursuit-smoke.json',150),('BossTechniques_','boss-techniques-smoke.json',896),
       ('Swim_','swim-campaign-smoke.json',193),('Encounter_','stage-smoke.json',420),('Vertical_','vertical-campaign-smoke.json',108)]

def digest(path):
    value=hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda:stream.read(1024*1024),b''):value.update(chunk)
    return value.hexdigest()

def require(condition,message):
    if not condition:raise ValueError(message)

evidence=[]
for prefix,name,minimum in SPECS:
    matches=[]
    for folder in (ROOT/'TestResults').glob(prefix+'*'):
        process=folder/'process-evidence.json';report=folder/name
        if not process.is_file() or not report.is_file():continue
        summary=json.loads(process.read_text(encoding='utf-8-sig'))
        if Path(summary['Executable']).resolve()!=EXE.resolve():continue
        result=json.loads(report.read_text(encoding='utf-8-sig'))
        if summary['Passed'] and summary['ExitCode']==0 and result['passed'] and not result['errors']:
            matches.append((process.stat().st_mtime,folder,summary,result))
    require(bool(matches),'No successful report for current build: '+prefix)
    _,folder,summary,result=max(matches,key=lambda value:value[0])
    require(len(result['checks'])>=minimum,'Incomplete check coverage: '+prefix)
    pictures=sorted(folder.glob('*.png'))
    for picture in pictures:
        image=Image.open(picture).convert('RGB')
        require(image.width>=1280 and image.height>=720,'Undersized evidence: '+str(picture))
        require(len(image.resize((40,24)).getcolors(961) or [])>=8,'Blank evidence: '+str(picture))
    evidence.append(dict(report=(folder/name).relative_to(ROOT).as_posix(),checks=len(result['checks']),screenshots=len(pictures),passed=True,exitCode=0))

prior=json.loads((BACKUP/'before-hashes.json').read_text(encoding='utf-8-sig'))
before={entry['path']:entry['sha256'].lower() for entry in prior}
require(digest(ROOT/'Assets/Settings/MoriEchoesConfig.asset')==before['Assets/Settings/MoriEchoesConfig.asset'],
        'Character/weapon/stage configuration was changed')
art_count=0
for folder in ['Assets/Art/Production/SwimAnimeV2','Assets/Art/Production/BossTechniquesV1']:
    directory=ROOT/folder
    for entry in json.loads((directory/'art-manifest.json').read_text(encoding='utf-8'))['assets']:
        require(digest(directory/entry['path'])==entry['sha256'],'Existing artwork changed: '+entry['path'])
        art_count+=1
sources=sorted((ROOT/'Assets').rglob('*.cs'))
require(all(path.stat().st_mtime<=LOG.stat().st_mtime for path in sources),'Source changed after latest build')
(DEST/'compiled-source-hashes.json').write_text(json.dumps([dict(path=p.relative_to(ROOT).as_posix(),sha256=digest(p)) for p in sources],indent=2)+'\n',encoding='utf-8')

names=[
 'Assets/Scripts/Enemies/BossPursuitController.cs','Assets/Scripts/Enemies/BossController.cs','Assets/Scripts/Enemies/BossField.cs',
 'Assets/Scripts/Enemies/BossTechniqueController.cs','Assets/Scripts/Enemies/SideBossController.cs',
 'Assets/Scripts/Enemies/VerticalBossController.cs','Assets/Scripts/Enemies/SwimBossController.cs','Assets/Scripts/Enemies/EnemyAI.cs',
 'Assets/Scripts/Roguelite/ExperiencePickup.cs','Assets/Scripts/Diagnostics/BossPursuitSmokeTest.cs',
 'Assets/Scripts/Diagnostics/BossTechniqueSmokeTest.cs','Assets/Scripts/Diagnostics/SwimCampaignSmokeTest.cs',
 'Assets/Scripts/Diagnostics/SideScrollerSmokeTest.cs','Assets/Scripts/Diagnostics/VerticalCampaignSmokeTest.cs',
 'Tools/Test-BossPursuit.ps1','Tools/Verify-BossPursuitDelivery.py','README.md','HOW_TO_PLAY.md','PLAY_MoriEchoes.cmd',
 'docs/BOSS_PURSUIT_AND_TARGETING_ACCEPTANCE.md','docs/PROGRESS.md',
 'docs/BuildReports/Windows_20261029_173048_021.md','Logs/BossPursuitZeroHPFixBuild_20261029.log']
files=[]
for name in names:
    path=ROOT/name
    require(path.is_file(),'Missing delivered file: '+name)
    backup=BACKUP/name.replace('/','_')
    original=before.get(name,digest(backup) if backup.is_file() else '')
    for item in [path,path.with_name(path.name+'.meta')]:
        if not item.is_file():continue
        data=item.read_text(encoding='utf-8-sig',errors='replace')
        require(not re.search(r'sk-[A-Za-z0-9]{24,}',data),'Credential found in deliverable: '+name)
        files.append(dict(path=item.relative_to(ROOT).as_posix(),bytes=item.stat().st_size,sha256=digest(item),beforeSha256=original if item==path else ''))
(DEST/'file-changes.json').write_text(json.dumps(files,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
with (DEST/'file-changes.csv').open('w',encoding='utf-8-sig',newline='') as stream:
    writer=csv.DictWriter(stream,fieldnames=['path','bytes','sha256','beforeSha256']);writer.writeheader();writer.writerows(files)
result=dict(verifiedAt=datetime.now(timezone.utc).isoformat(),executable=str(EXE),executableSha256=digest(EXE),
            buildReport='docs/BuildReports/Windows_20261029_173048_021.md',buildSucceeded=True,buildErrors=0,buildWarnings=0,
            runtime=evidence,runtimeChecks=sum(entry['checks'] for entry in evidence),sourceFiles=len(sources),files=len(files),
            configurationPreserved=True,existingArtHashesVerified=art_count,newImageRequests=0,
            bossDeathRule='Actual damage must reduce primary health to zero. No automatic 15-percent finisher damage; death presentation only after IsDead. One remaining HP blocks victory.',
            scope='Assisted local Windows runtime; no human default-difficulty campaign, controller endurance or cross-machine performance certification. No ZIP.')
(DEST/'delivery-verification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(passed=True,checks=result['runtimeChecks'],files=len(files),executable=str(EXE),report=str(DEST/'delivery-verification.json')),ensure_ascii=False))
