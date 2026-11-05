"""Build the three practice sections and two portrait PDFs from course.json."""
import argparse,hashlib,html,json,shutil,zipfile
from pathlib import Path
from urllib.parse import quote
from design import pdf
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'site'
ESC=html.escape
def load_course():
    d=json.loads((ROOT/'content/course.json').read_text(encoding='utf-8'))
    for page in d['guide']:
        for b in page['blocks']:
            if 'code_file' in b:
                item=next(i for i in d['code_files'] if i['name']==b['code_file'])
                lines=(ROOT/item['path']).read_text(encoding='utf-8').splitlines()
                start,end=b['code_lines'];b['text']='\n'.join(lines[start:end])
    return d
def validate(d,release=False):
    assert len(d['areas'])==30 and [a['id'] for a in d['areas']]==list(range(1,31))
    for key in ['assignment','files','guide','areas']:
        for p in d[key]:
            assert p['title'] and p['blocks']
            for b in p['blocks']:assert b['title'] and b['text']
def link(name,label):return f'<a href="downloads/{quote(name)}">{ESC(label)}</a>'
def html_page(d,key):
    routes=[('assignment','index.html','Задание'),('guide','lessons.html','Инструкция и образец'),('areas','areas.html','Предметные области')]
    nav=''.join(f'<a href="{url}" '+('aria-current="page"' if key==sec else '')+f'>{label}</a>' for sec,url,label in routes)
    content=[]
    if key=='assignment':content.append('<div class="downloads">'+link('assignment.pdf','Методические рекомендации (PDF)')+link('guide.pdf','Список предметных областей (PDF)')+link('Отчет о выполнении.docx','Форма отчёта (DOCX)')+link('PP12-CrystalRoute-UnityProject.zip','Контрольный пример для необязательной сверки (ZIP)')+'</div>')
    specs=d[key]+(d['files'] if key=='assignment' else [])
    if key=='guide':content.append('<nav aria-label="Разделы инструкции">'+''.join(f'<a href="#page-{i}">{ESC(p["title"])}</a>' for i,p in enumerate(specs,1) if not any(b['kind']=='code' for b in p['blocks']))+'</nav>')
    for i,p in enumerate(specs,1):
        anchor=f'variant-{p["id"]:02}' if 'id' in p else f'page-{i}';blocks=[]
        if 'id' in p:blocks.append('<p class="context"><b>Индивидуальная работа:</b> '+ESC(p['subject'])+'. <b>Образец:</b> «Кристальный маршрут»; читать и переносить приём в свой проект.</p>')
        if p in d['files']:blocks.append('<div class="downloads">'+link('PP12-CrystalRoute-UnityProject.zip','Скачать контрольный пример для сверки (ZIP)')+link('Отчет о выполнении.docx','Скачать форму отчёта (DOCX)')+'</div>')
        for n,b in enumerate(p['blocks'],1):
            if b['kind']=='image':body=f'<figure><img src="{ESC(b["path"])}" alt="{ESC(b["text"])}"><figcaption>{ESC(b["text"])}</figcaption></figure>'
            elif b['kind']=='code':body='<pre>'+ESC(b['text'])+'</pre>'
            else:body=''.join('<p>'+ESC(t).replace('\n','<br>')+'</p>' for t in b['text'].split('\n\n'))
            if b.get('code_file'):body+=link(b['code_file'],'Скачать полный файл '+b['code_file'])
            blocks.append(f'<div class="step"><span class="number">{n}</span><section class="card"><h3>{ESC(b["title"])}</h3>{body}</section></div>')
        content.append(f'<article class="chapter" id="{anchor}"><header class="doc-head"><p class="section-id">ПП ПМ.12 · {i}</p><h2>{ESC(p["title"])}</h2></header><div class="steps">'+''.join(blocks)+'</div></article>')
    return '<!doctype html><html lang="ru"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>'+ESC(d['meta']['title'])+'</title><link rel="icon" href="assets/favicon.svg"><link rel="stylesheet" href="assets/style.css"></head><body class="'+('subject-areas' if key=='areas' else 'method')+'"><a class="skip" href="#main">К содержанию</a><nav aria-label="Разделы практики">'+nav+'</nav><main id="main">'+''.join(content)+'</main></body></html>'
def main():
    parser=argparse.ArgumentParser();parser.add_argument('--release',action='store_true');parser.add_argument('--render-pdf',action='store_true');args=parser.parse_args()
    d=load_course();validate(d,args.release);OUT.mkdir(exist_ok=True);dest=OUT/'downloads';dest.mkdir(exist_ok=True)
    shutil.copytree(ROOT/'assets',OUT/'assets',dirs_exist_ok=True);shutil.copytree(ROOT/'scripts/assets/fonts',OUT/'assets/fonts',dirs_exist_ok=True)
    if not (ROOT/'forms/Отчет о выполнении.docx').is_file():raise FileNotFoundError('Build report explicitly before publishing')
    shutil.copyfile(ROOT/'forms/Отчет о выполнении.docx',dest/'Отчет о выполнении.docx')
    for key,name in [('assignment','index.html'),('guide','lessons.html'),('areas','areas.html')]: (OUT/name).write_text(html_page(d,key),encoding='utf-8')
    for item in d['code_files']:(dest/item['name']).write_bytes((ROOT/item['path']).read_text(encoding='utf-8').encode('utf-8'))
    with zipfile.ZipFile(dest/'PP12-CrystalRoute-UnityProject.zip','w',zipfile.ZIP_DEFLATED) as z:
        for path in sorted((ROOT/'unity-project').rglob('*'),key=lambda p:p.relative_to(ROOT/'unity-project').as_posix()):
            if not path.is_file():continue
            rel=path.relative_to(ROOT/'unity-project')
            if any(x in rel.parts for x in ['Library','Temp','Logs','obj','UserSettings','Build','.vs','_Recovery','Documentation','Editor']):continue
            if rel.as_posix()=='Assets/Editor.meta':continue
            data=path.read_bytes()
            if path.suffix.lower() in ['.cs','.meta','.asset','.unity','.json','.txt','.md'] or path.name=='.gitignore':data=data.replace(b'\r\n',b'\n')
            info=zipfile.ZipInfo('CrystalRoute/'+rel.as_posix(),(2026,11,5,0,0,0));info.compress_type=zipfile.ZIP_DEFLATED
            info.create_system=3;info.external_attr=0o100644<<16
            z.writestr(info,data)
    inputs=[ROOT/'content/course.json',ROOT/'scripts/design.py']+[ROOT/item['path'] for item in d['code_files']]
    source_digest=hashlib.sha256(''.join(p.read_text(encoding='utf-8') for p in inputs).encode('utf-8')).hexdigest()
    materials=ROOT/'materials';materials.mkdir(exist_ok=True)
    if args.render_pdf:
        pdf(ROOT,materials/'assignment.pdf',d,'assignment',True);pdf(ROOT,materials/'guide.pdf',d,'guide',True)
        (materials/'source-sha256.json').write_text(json.dumps({'source':source_digest,'files':{n:hashlib.sha256((materials/n).read_bytes()).hexdigest() for n in ['assignment.pdf','guide.pdf']}},indent=2)+'\n')
    pinned=json.loads((materials/'source-sha256.json').read_text())
    if pinned['source']!=source_digest:raise ValueError('Content changed: render PDFs and review before publishing')
    for name,digest in pinned['files'].items():
        if hashlib.sha256((materials/name).read_bytes()).hexdigest()!=digest:raise ValueError('Reviewed PDF changed')
        shutil.copyfile(materials/name,dest/name)
    (OUT/'.nojekyll').write_text('')
    manifest={p.relative_to(OUT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in OUT.rglob('*') if p.is_file() and p.name!='manifest.json'}
    (OUT/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print('Built method, 30 area cards, common report and optional reference project')
if __name__=='__main__':main()
