#!/usr/bin/env python3
"""Build Data/categories.json from wiki data (cascade synthesis).

Sources (scripts/wiki_cache/, produced by fetch_wiki_data.py):
  A. luadata.json       — wiki's game-derived per-item database (fields per id)
  B. category_members/  — editor-curated category member pages
Plus scripts/category_overrides.json (curated corrections, internal-keyed).

Cascade per item (first stage that decides wins):
  0. explicit override
  1. strong game fields: Mount / Armor (slots, vanity-aware) / Accessory /
     Ammo / Potion / Dye
  2. wiki placement & curated categories, in fixed order (light sources beat
     tools per user decision; first match wins)
  3. weak game fields: Tool powers (pick/hammer/axe/fishingPole), Weapon
     (damage) — only for items with no wiki category signal
  4. weak wiki categories (Consumable / Crafting material / Bar / Potion
     ingredients / Novelty / Instruments / Unobtainable / Miscellaneous) —
     only for NON-placeable items (the wiki's Consumable category includes
     every placeable, so it is meaningless as a signal for them)
  5. placement fallback from fields: createWall -> Wall, createTile -> Furniture
  6. legacy items.json category (retired corrupt values -> Misc)
"""
import json
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
CACHE = os.path.join(HERE, 'wiki_cache')
ITEMS_JSON = os.path.join(ROOT, 'Data', 'items.json')
OUT_JSON = os.path.join(ROOT, 'Data', 'categories.json')
OVERRIDES_JSON = os.path.join(HERE, 'category_overrides.json')
REPORT_TXT = os.path.join(HERE, 'categories_report.txt')

VALID_KEYS = {'Mount', 'Armor', 'Accessory', 'Tool', 'Ammo', 'Weapon', 'Potion',
              'Consumable', 'Material', 'Block', 'Wall', 'Furniture', 'Dye',
              'Vanity', 'Misc', 'None'}
RETIRED = {'Mount', 'Minecart', 'Vanity', 'Hook', 'Wall'}  # corrupt legacy values

# Stage 2: wiki categories in first-match-wins order.
WIKI_ORDER = [
    # placement families first; light sources beat tools (user decision)
    ('Light source items', 'Furniture'),
    ('Block items', 'Block'),
    ('Minecart track items', 'Block'),
    ('Wall items', 'Wall'),
    ('Furniture items', 'Furniture'),
    ('Storage items', 'Furniture'),
    ('Mechanism items', 'Tool'),
    ('Tool items', 'Tool'),
    ('Ammunition items', 'Ammo'),
    ('Potion items', 'Potion'),
    ('Accessory items', 'Accessory'),
    ('Scope', 'Accessory'),
    ('Informational items', 'Accessory'),
    ('Armor items', 'Armor'),
    ('Weapon items', 'Weapon'),
    ('Dye items', 'Dye'),
    ('Hair dye items', 'Dye'),
    ('Vanity items', 'Vanity'),
    ('Developer items', 'Vanity'),
    ('Bar items', 'Material'),
    ('Bait items', 'Consumable'),
    ('Seeds items', 'Consumable'),
    ('Key items', 'Consumable'),
    ('Grab bag items', 'Consumable'),
    ('Boss summon items', 'Consumable'),
    ('Paints', 'Consumable'),
    ('Pet summon items', 'Mount'),
    ('Light pet items', 'Mount'),
]
# 'Healing items' is deliberately absent: it mixes potions with food (the
# potion flag in game data + the Potion items category cover actual potions).
WIKI_KEYS = dict(WIKI_ORDER)

# Stage 4: weak wiki categories (only for non-placeable items).
WEAK_WIKI = {'Consumable items': 'Consumable', 'Crafting material items': 'Material',
             'Bar items': 'Material', 'Potion ingredients': 'Material',
             'Novelty items': 'Misc', 'Instruments': 'Misc',
             'Unobtainable items': 'Misc', 'Miscellaneous items': 'Misc'}
WEAK_ORDER = ['Consumable', 'Material', 'Misc']


def parse_luadata(text):
    """Parse the generated Lua table in Module:Iteminfo/luadata.

    Format (regular, generated):  ["0"] = { ["Field"] = value, ... },
    Values are booleans, numbers (possibly negative/float), or strings.
    """
    items = {}
    for m in re.finditer(r'\["(\d+)"\]\s*=\s*\{', text):
        item_id = int(m.group(1))
        start = m.end()
        depth = 1
        i = start
        while depth > 0 and i < len(text):
            if text[i] == '{':
                depth += 1
            elif text[i] == '}':
                depth -= 1
            i += 1
        body = text[start:i - 1]
        fields = {}
        for fm in re.finditer(r'\["([A-Za-z0-9_]+)"\]\s*=\s*(-?\d+(?:\.\d+)?|true|false|"[^"]*")', body):
            key, raw = fm.group(1), fm.group(2)
            if raw == 'true':
                val = True
            elif raw == 'false':
                val = False
            elif raw.startswith('"'):
                val = raw[1:-1]
            else:
                val = float(raw) if '.' in raw else int(raw)
            fields[key] = val
        items[item_id] = fields
    return items


def load_wiki_categories():
    """Return {page_title: [wiki category names]} from the member caches."""
    result = {}
    cat_dir = os.path.join(CACHE, 'category_members')
    if not os.path.isdir(cat_dir):
        return result
    for fname in os.listdir(cat_dir):
        cat = fname[:-5].replace('_', ' ')
        if cat not in WIKI_KEYS and cat not in WEAK_WIKI:
            continue
        with open(os.path.join(cat_dir, fname), encoding='utf-8') as f:
            for title in json.load(f):
                result.setdefault(title, []).append(cat)
    return result


def load_overrides():
    if not os.path.exists(OVERRIDES_JSON):
        return {}
    with open(OVERRIDES_JSON, encoding='utf-8') as f:
        return json.load(f)


def strong_fields(fields):
    """Stage 1: unambiguous game-field signals. Returns a key or None."""
    if fields.get('mountType', -1) > -1 or fields.get('cartTrack'):
        return 'Mount'
    slots = (fields.get('headSlot', -1) > -1 or fields.get('bodySlot', -1) > -1
             or fields.get('legSlot', -1) > -1)
    if slots:
        return 'Vanity' if fields.get('vanity') else 'Armor'
    # Music boxes carry the accessory flag but are placeable furniture —
    # the accessory signal only counts for non-placeable items.
    if fields.get('accessory') and not placeable(fields):
        return 'Accessory'
    # Ale carries an ammo type in the game data but is a drink (buffType) —
    # real ammo never grants a buff.
    if fields.get('ammo', 0) > 0 and not fields.get('buffType'):
        return 'Ammo'
    # The potion flag only covers potion-sickness potions; buff potions are
    # identified by buffType. Food also grants buffs (Well Fed), so the
    # Potion items wiki category gate below is what separates them.
    if fields.get('potion') or fields.get('healLife', 0) > 0 or fields.get('healMana', 0) > 0:
        return 'Potion'
    if fields.get('dye', -1) > -1 or fields.get('hairDye', -1) > -1:
        return 'Dye'
    return None


def weak_fields(fields):
    """Stage 3/5: field signals that need no wiki corroboration."""
    if (fields.get('pick', 0) > 0 or fields.get('hammer', 0) > 0
            or fields.get('axe', 0) > 0 or fields.get('fishingPole', 0) > 0):
        return 'Tool'
    if fields.get('damage', 0) > 0:
        return 'Weapon'
    return None


def placeable(fields):
    return fields.get('createTile', 0) > 0 or fields.get('createWall', 0) > 0


def main():
    if not os.path.isdir(CACHE):
        print('ERROR: wiki_cache missing — run scripts/fetch_wiki_data.py first')
        return 1

    with open(ITEMS_JSON, encoding='utf-8') as f:
        items = json.load(f)
    with open(os.path.join(CACHE, 'luadata.json'), encoding='utf-8') as f:
        lua_raw = json.load(f)
    lua_text = lua_raw['query']['pages'][0]['revisions'][0]['slots']['main']['content']
    luadata = parse_luadata(lua_text)
    print(f'luadata parsed: {len(luadata)} items')

    wiki_cats = load_wiki_categories()
    overrides = load_overrides()

    def norm(s):
        return re.sub(r'[^a-z0-9]', '', s.lower())

    by_name = {}
    for item in items:
        by_name.setdefault(item['name'], []).append(item)
    norm_map = {}
    for item in items:
        norm_map.setdefault(norm(item['name']), []).append(item)

    def resolve_wiki(title):
        if title in by_name:
            return by_name[title]
        n = norm(title)
        if n in norm_map:
            return norm_map[n]
        return None

    unmatched_pages = set()
    out = {}
    legacy_change = []
    override_used = []
    stage_used = {}
    no_signal = []

    for item in sorted(items, key=lambda x: x['id']):
        iid = item['id']
        internal = item['internal']
        legacy = item.get('category') or 'None'
        if iid == 0:
            out[internal] = 'None'
            continue

        fields = luadata.get(iid)
        if fields is None:
            no_signal.append((iid, item['name'], 'no luadata'))

        # page titles for this item from all wiki categories
        pages = []
        found = resolve_wiki(item['name'])
        if found is None:
            unmatched_pages.add(item['name'])
        else:
            for f in found:
                pages.extend(wiki_cats.get(f['name'], []))

        def decide(stage, key):
            out[internal] = key
            stage_used[key] = stage_used.get(key, 0) + 1

        # stage 0: override
        if internal in overrides:
            ov = overrides[internal]
            if ov not in VALID_KEYS:
                raise ValueError(f'override {internal} -> invalid key {ov}')
            decide('override', ov)
            override_used.append((internal, legacy, ov))
            continue

        # stage 1: strong fields
        if fields is not None:
            strong = strong_fields(fields)
            if strong:
                decide('strong-fields', strong)
                continue

        # stage 2: wiki placement/curated categories (first match wins)
        stage2 = None
        for cat in WIKI_ORDER:
            if cat[0] not in pages:
                continue
            # The wiki's "Potion items" category also contains food (e.g.
            # Marshmallow, Cooked Fish). Food shows the eating animation
            # (useStyle 2) or grants the Well Fed family of buffs; potions
            # drink (useStyle 9) and grant other buffs.
            if cat[0] == 'Potion items' and fields is not None:
                food_buffs = {26, 384, 385}  # Well Fed / Plenty Satisfied / Exquisitely Stuffed
                potionish = fields.get('potion') or fields.get('healLife', 0) > 0 \
                    or fields.get('healMana', 0) > 0 or fields.get('buffType')
                is_food = fields.get('useStyle') == 2 or fields.get('buffType') in food_buffs
                if not potionish or is_food:
                    continue
            # The wiki's "Ammunition items" category includes Ale, which has
            # an ammo type in the game data but is a drink (buffType).
            if cat[0] == 'Ammunition items' and fields is not None and fields.get('buffType'):
                continue
            stage2 = cat[1]
            break
        if stage2:
            decide('wiki-placement', stage2)
            continue

        # stage 3: weak fields (no wiki signal)
        if fields is not None:
            weak = weak_fields(fields)
            if weak:
                decide('weak-fields', weak)
                continue

        # stage 4: weak wiki categories — only for non-placeable items
        if fields is not None and not placeable(fields):
            for cat, key in WEAK_WIKI.items():
                if cat in pages:
                    decide('weak-wiki', key)
                    break
            else:
                # nothing in stage 4 either
                pass
            if internal in out:
                continue

        # stage 5: placement fallback from fields
        if fields is not None:
            if fields.get('createWall', 0) > 0:
                decide('placement-fallback', 'Wall')
                continue
            if fields.get('createTile', 0) > 0:
                # Placeable crafting materials (ores, wood, ...) that no
                # placement category claims are materials, not furniture.
                # (Light sources are already decided in stage 2 / overrides.)
                if fields.get('material') and 'Crafting material items' in pages:
                    decide('placement-fallback', 'Material')
                else:
                    decide('placement-fallback', 'Furniture')
                continue

        # stage 6: legacy fallback
        cat = legacy if legacy in VALID_KEYS and legacy not in RETIRED else 'Misc'
        decide('legacy', cat)
        if legacy != cat:
            legacy_change.append((internal, legacy, cat))

    # ---- integrity ----
    if len(out) != len(items):
        print(f'ERROR: output count {len(out)} != items count {len(items)}')
        return 1
    counts = {}
    for v in out.values():
        counts[v] = counts.get(v, 0) + 1
    for k in VALID_KEYS - {'None'}:
        if k not in counts:
            print(f'WARNING: category {k} is EMPTY')
    print('counts:', dict(sorted(counts.items())))

    with open(OUT_JSON, 'w', encoding='utf-8') as f:
        json.dump(out, f, ensure_ascii=False, indent=2, sort_keys=True)
    print('wrote', OUT_JSON)

    # ---- report ----
    with open(REPORT_TXT, 'w', encoding='utf-8') as f:
        f.write('== category counts ==\n')
        for k in sorted(counts):
            f.write(f'  {k}: {counts[k]}\n')
        f.write('\n== stage usage (how each item got its category) ==\n')
        for k in sorted(stage_used):
            f.write(f'  {k}: {stage_used[k]}\n')
        f.write('\n== wiki pages not matched to items.json ==\n')
        for p in sorted(unmatched_pages):
            f.write(f'  {p}\n')
        f.write(f'\n  ({len(unmatched_pages)} total)\n')
        f.write('\n== overrides applied ==\n')
        for internal, legacy, ov in override_used:
            f.write(f'  {internal}: {legacy} -> {ov}\n')
        f.write('\n== legacy category changes ==\n')
        for internal, old, new in legacy_change:
            f.write(f'  {internal}: {old} -> {new}\n')
        f.write(f'\n  ({len(legacy_change)} total)\n')
        f.write('\n== items without luadata ==\n')
        for iid, name, why in no_signal:
            f.write(f'  {iid} {name} ({why})\n')
        f.write('\n== leftover Misc (curated review list) ==\n')
        for internal, cat in sorted(out.items()):
            if cat == 'Misc':
                item = next((i for i in items if i['internal'] == internal), None)
                f.write(f'  {internal} ({item["name"] if item else "?"})\n')
    print('report written:', REPORT_TXT)
    return 0


if __name__ == '__main__':
    sys.exit(main())
