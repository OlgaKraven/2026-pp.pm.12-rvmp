"""Check complete source coverage, links and issued artifacts."""
import hashlib,json,re,zipfile
from html.parser import HTMLParser
from urllib.parse import urlsplit,unquote
from pypdf import PdfReader
from build import ROOT,load_course,validate
d=load_course();validate(d);site=ROOT/'site'
compact=lambda x:re.sub(r'\s+','',x)
for name,specs in [('assignment',d['assignment']+d['files']+d['guide']),('guide',d['areas'])]:
    doc=PdfReader(site/f'downloads/{name}.pdf');text=compact(''.join(p.extract_text() for p in doc.pages))
    for p in doc.pages:assert abs(float(p.mediabox.width)-595.276)<1 and abs(float(p.mediabox.height)-841.89)<1
    for p in specs:
        for b in p['blocks']:
            for s in [p['title'],b['title'],*b['text'].splitlines()]:
                if s.strip():assert compact(s) in text,(name,s[:110])
    print(name,len(doc.pages),'portrait pages; every source line retained')
class Links(HTMLParser):
    def __init__(self):super().__init__();self.links=[];self.ids=set()
    def handle_starttag(self,tag,attrs):
        attrs=dict(attrs)
        if 'id' in attrs:assert attrs['id'] not in self.ids;self.ids.add(attrs['id'])
        for key in ['href','src']:
            if key in attrs:self.links.append(attrs[key])
parsers={}
for f in site.glob('*.html'):
    p=Links();p.feed(f.read_text(encoding='utf-8'));parsers[f.resolve()]=p
for f,p in parsers.items():
    for link in p.links:
        u=urlsplit(link);assert not u.scheme and not u.netloc
        target=(f.parent/unquote(u.path)).resolve() if u.path else f
        assert target.is_file(),link
        if u.fragment:assert u.fragment in parsers[target].ids,link
for item in d['code_files']:
    assert (ROOT/item['path']).read_text(encoding='utf-8')==(site/'downloads'/item['name']).read_text(encoding='utf-8')
    covered=[]
    for p in d['guide']:
        for b in p['blocks']:
            if b.get('code_file')==item['name']:covered.extend(range(*b['code_lines']))
    assert covered==list(range(len((ROOT/item['path']).read_text(encoding='utf-8').splitlines())))
student=json.dumps([d[k] for k in ['assignment','files','guide','areas']],ensure_ascii=False)
for forbidden in ['ПМ.13','баллов','ПП12_Группа_Фамилия','PDF-отчёт']:assert forbidden not in student,forbidden
for a in d['areas']:assert len(a['blocks'])==8 and 'Поражение:' in a['blocks'][-2]['text']
with zipfile.ZipFile(site/'downloads/PP12-CrystalRoute-UnityProject.zip') as z:assert not any(set(p.split('/')) & {'Library','Temp','Logs','obj','UserSettings','Build','_Recovery'} for p in z.namelist())
with zipfile.ZipFile(site/'downloads/Отчет о выполнении.docx') as z:
    xml=z.read('word/document.xml').decode();assert 'Ссылка на репозиторий проекта' in xml and 'TOC ' not in xml
for name,digest in json.loads((site/'manifest.json').read_text(encoding='utf-8')).items():assert hashlib.sha256((site/name).read_bytes()).hexdigest()==digest
print('PASS: full source coverage, C#, 30 variants, local links, clean archive, common report')
