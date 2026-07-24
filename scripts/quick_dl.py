"""Quick download all missing item icons from Terraria Wiki."""
import json, base64, urllib.request, urllib.parse, gzip, re, html, time, os
from PIL import Image
from io import BytesIO

ICONS_FILE = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), 'Data', 'icons_items.json')
PROGRESS_FILE = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), 'Data', '.download_progress.json')
WIKI_API = 'https://terraria.wiki.gg/api.php'

with open(ICONS_FILE, 'r') as f:
    icons = json.load(f)

placeholder_b64 = icons['-1']
missing = sorted([int(k) for k, v in icons.items() if v == placeholder_b64 and int(k) > 0])

progress = {}
if os.path.exists(PROGRESS_FILE):
    with open(PROGRESS_FILE) as f:
        progress = json.load(f)

remaining = [i for i in missing if str(i) not in progress]
print(f'Total missing: {len(missing)}, done: {len(progress)}, remaining: {len(remaining)}')

def api(params):
    url = WIKI_API + '?' + urllib.parse.urlencode(params)
    time.sleep(0.3)
    try:
        req = urllib.request.Request(url, headers={'User-Agent': 'TPE/1.0', 'Accept-Encoding': 'gzip'})
        resp = urllib.request.urlopen(req, timeout=10)
        data = resp.read()
        if resp.headers.get('Content-Encoding') == 'gzip':
            data = gzip.decompress(data)
        return json.loads(data)
    except:
        return {}

def get_fn(item_id):
    try:
        r = api({'action': 'cargoquery', 'format': 'json', 'tables': 'Items', 'fields': 'image', 'where': f'itemid={item_id}', 'limit': '1'})
        if r.get('cargoquery'):
            m = re.search(r'\[\[File:([^|\]]+)', r['cargoquery'][0]['title'].get('image', ''))
            if m: return html.unescape(m.group(1))
    except: pass
    return None

def dl(filename):
    url = f'https://terraria.wiki.gg/wiki/Special:FilePath/{urllib.parse.quote(filename)}'
    time.sleep(0.2)
    try:
        req = urllib.request.Request(url, headers={'User-Agent': 'TPE/1.0'})
        resp = urllib.request.urlopen(req, timeout=10)
        data = resp.read()
        if len(data) < 50: return None
        img = Image.open(BytesIO(data)).convert('RGBA')
        img = img.resize((32, 32), Image.LANCZOS)
        buf = BytesIO()
        img.save(buf, format='PNG')
        return base64.b64encode(buf.getvalue()).decode('utf-8')
    except: return None

ok = 0; fail = 0
try:
    for n, item_id in enumerate(remaining):
        fn = get_fn(item_id)
        if fn:
            b64 = dl(fn)
            if b64:
                icons[str(item_id)] = b64
                progress[str(item_id)] = b64
                ok += 1
            else: fail += 1
        else: fail += 1

        if (ok + fail) % 20 == 0:
            print(f'  [{n+1}/{len(remaining)}] ok={ok} fail={fail}')
        if ok > 0 and ok % 50 == 0:
            with open(ICONS_FILE, 'w') as f: json.dump(icons, f, ensure_ascii=False, separators=(',', ':'))
            with open(PROGRESS_FILE, 'w') as f: json.dump(progress, f)
            print(f'  [SAVED]')
except KeyboardInterrupt:
    print(f'INTERRUPTED')

with open(ICONS_FILE, 'w') as f: json.dump(icons, f, ensure_ascii=False, separators=(',', ':'))
with open(PROGRESS_FILE, 'w') as f: json.dump(progress, f)

# Cleanup if all done
new_missing = sum(1 for k,v in icons.items() if int(k) > 0 and v == placeholder_b64)
print(f'DONE: {ok} ok, {fail} fail. Still missing: {new_missing}')
if new_missing == 0 and os.path.exists(PROGRESS_FILE):
    os.remove(PROGRESS_FILE)
    print('ALL ICONS DOWNLOADED!')
