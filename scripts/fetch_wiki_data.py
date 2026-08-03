#!/usr/bin/env python3
"""Fetch authoritative item data from terraria.wiki.gg into scripts/wiki_cache/.

Sources:
  A. Module:Iteminfo/luadata — the wiki's game-derived per-item database
     (Terraria 1.4.5.6); every item ID with its full game fields. 1 request.
  B. Category:Items content subcategories — editor-curated placement families.
     ~33 requests with pagination.

Raw responses are cached under scripts/wiki_cache/; re-runs skip anything
already cached (resume-friendly, no refetching).
"""
import json
import os
import sys
import time
import urllib.parse
import urllib.request

API = 'https://terraria.wiki.gg/api.php'
UA = 'TerrariaPlayersEditor/2.1.0 (research)'
CACHE = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'wiki_cache')

# Wiki Category:Items content subcategories -> our taxonomy key.
# Meta categories (Acquired through, Autoswing items, Hardmode-only items,
# Items of rarity, Set items, Boots items) are deliberately excluded.
CONTENT_CATEGORIES = {
    'Weapon items': 'Weapon',
    'Armor items': 'Armor',
    'Accessory items': 'Accessory',
    'Tool items': 'Tool',
    'Ammunition items': 'Ammo',
    'Potion items': 'Potion',
    'Healing items': 'Potion',
    'Consumable items': 'Consumable',
    'Bait items': 'Consumable',
    'Seeds items': 'Consumable',
    'Key items': 'Consumable',
    'Grab bag items': 'Consumable',
    'Summoning items': 'Consumable',
    'Paints': 'Consumable',
    'Crafting material items': 'Material',
    'Potion ingredients': 'Material',
    'Bar items': 'Material',
    'Block items': 'Block',
    'Minecart track items': 'Block',
    'Wall items': 'Wall',
    'Furniture items': 'Furniture',
    'Light source items': 'Furniture',
    'Storage items': 'Furniture',
    'Mechanism items': 'Tool',
    'Informational items': 'Accessory',
    'Scope': 'Accessory',
    'Dye items': 'Dye',
    'Hair dye items': 'Dye',
    'Vanity items': 'Vanity',
    'Developer items': 'Vanity',
    'Novelty items': 'Misc',
    'Instruments': 'Misc',
    'Unobtainable items': 'Misc',
    'Miscellaneous items': 'Misc',
    # top-level categories (not subcategories of Category:Items)
    'Pet summon items': 'Mount',
    'Light pet items': 'Mount',
    'Boss summon items': 'Consumable',
}


def api_get(params, cache_name):
    """GET the API, caching the raw JSON response. Returns parsed JSON."""
    path = os.path.join(CACHE, cache_name)
    if os.path.exists(path):
        with open(path, encoding='utf-8') as f:
            return json.load(f)

    url = API + '?' + urllib.parse.urlencode(params)
    last_err = None
    for attempt in range(3):
        try:
            req = urllib.request.Request(url, headers={'User-Agent': UA})
            with urllib.request.urlopen(req, timeout=60) as resp:
                data = json.load(resp)
            os.makedirs(CACHE, exist_ok=True)
            with open(path, 'w', encoding='utf-8') as f:
                json.dump(data, f, ensure_ascii=False)
            return data
        except Exception as exc:  # noqa: BLE001 - network retries
            last_err = exc
            print(f'  retry {attempt + 1}/3 for {cache_name}: {exc}')
            time.sleep(1.5 * (attempt + 1))
    raise RuntimeError(f'failed to fetch {url}: {last_err}')


def fetch_luadata():
    print('* fetching Module:Iteminfo/luadata ...')
    data = api_get(
        {
            'action': 'query',
            'prop': 'revisions',
            'rvprop': 'content',
            'rvslots': 'main',
            'titles': 'Module:Iteminfo/luadata',
            'format': 'json',
            'formatversion': 2,
        },
        'luadata.json',
    )
    text = data['query']['pages'][0]['revisions'][0]['slots']['main']['content']
    print(f'  luadata: {len(text)} chars')


def fetch_category_members():
    for cat in sorted(CONTENT_CATEGORIES):
        cache = os.path.join(CACHE, 'category_members', cat.replace(' ', '_') + '.json')
        if os.path.exists(cache):
            print(f'* {cat}: cached')
            continue
        print(f'* fetching Category:{cat} ...')
        members = []
        cont = None
        while True:
            params = {
                'action': 'query',
                'list': 'categorymembers',
                'cmtitle': f'Category:{cat}',
                'cmtype': 'page',
                'cmlimit': 500,
                'format': 'json',
                'formatversion': 2,
            }
            if cont:
                params.update(cont)
            data = api_get(params, '__paging__' + cat.replace(' ', '_') + (f'_{len(members)}' if cont else '.json'))
            # _paging files are transient; merge into the final cache below.
            for m in data['query']['categorymembers']:
                members.append(m['title'])
            if 'continue' in data:
                cont = data['continue']
            else:
                break
            time.sleep(0.3)
        os.makedirs(os.path.dirname(cache), exist_ok=True)
        with open(cache, 'w', encoding='utf-8') as f:
            json.dump(sorted(set(members)), f, ensure_ascii=False, indent=1)
        print(f'  {len(members)} members')
        time.sleep(0.5)


def main():
    os.makedirs(CACHE, exist_ok=True)
    fetch_luadata()
    fetch_category_members()
    print('DONE — data cached in', CACHE)


if __name__ == '__main__':
    sys.exit(main())
