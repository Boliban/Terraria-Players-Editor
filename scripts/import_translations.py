#!/usr/bin/env python3
"""Import item translations from a CSV into Data/Locale/items-zh.json.

CSV columns (header required): ID, Item-en-us, Item-ch-en, Internal
  ID        — items.json id (authoritative key)
  Internal  — items.json internal name
  Item-en-us — English display name (validated against items.json, warn only)
  Item-ch-en — Chinese translation (required)

Resolution order: ID -> Internal. Rows that resolve to neither are skipped
with a warning. Existing entries in items-zh.json are preserved; explicit
conflicts are overwritten and reported. --dry-run reports without writing.
"""
import argparse
import csv
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
ITEMS_JSON = os.path.join(ROOT, 'Data', 'items.json')
ZH_JSON = os.path.join(ROOT, 'Data', 'Locale', 'items-zh.json')


def main():
    ap = argparse.ArgumentParser(description='Import item translations CSV -> items-zh.json')
    ap.add_argument('csv', help='path to the translation CSV (ID, Item-en-us, Item-ch-en, Internal)')
    ap.add_argument('--dry-run', action='store_true', help='report only, do not write')
    args = ap.parse_args()

    with open(ITEMS_JSON, encoding='utf-8') as f:
        items = json.load(f)
    by_id = {str(i['id']): i for i in items}
    by_internal = {i['internal']: i for i in items}

    with open(ZH_JSON, encoding='utf-8') as f:
        zh = json.load(f)
    original_keys = set(zh)

    added, updated, skipped, en_mismatch = [], [], [], []
    rows = 0
    with open(args.csv, encoding='utf-8-sig', newline='') as f:
        rd = csv.DictReader(f)
        for row in rd:
            rows += 1
            iid = (row.get('ID') or '').strip()
            internal = (row.get('Internal') or '').strip()
            en = (row.get('Item-en-us') or '').strip()
            zh_name = (row.get('Item-ch-en') or '').strip()

            if not zh_name:
                skipped.append((iid, internal, 'empty translation'))
                continue

            item = by_id.get(iid) or by_internal.get(internal)
            if item is None:
                skipped.append((iid, internal, 'no matching item'))
                continue
            if not internal:
                internal = item['internal']

            if en and en != item['name']:
                en_mismatch.append((iid, internal, en, item['name']))

            if internal in original_keys:
                if zh.get(internal) != zh_name:
                    updated.append((internal, zh.get(internal), zh_name))
                zh[internal] = zh_name
            else:
                added.append((internal, zh_name))
                zh[internal] = zh_name

    # coverage report
    all_internal = sorted({i['internal'] for i in items})
    missing = [x for x in all_internal if x not in zh]
    print(f'rows read:            {rows}')
    print(f'added:                {len(added)}')
    print(f'updated (conflict):   {len(updated)}')
    print(f'skipped:              {len(skipped)}')
    print(f'en-name mismatches:   {len(en_mismatch)}')
    print(f'items-zh entries now: {len(zh)}')
    print(f'still missing:        {len(missing)} of {len(all_internal)}')
    if missing:
        print('  missing:', ', '.join(missing))

    if en_mismatch:
        print('\n-- en-name mismatches (CSV name vs items.json, first 20) --')
        for m in en_mismatch[:20]:
            print(f'  id={m[0]} {m[1]}: "{m[2]}" != "{m[3]}"')

    if args.dry_run:
        print('\nDRY RUN — no changes written')
        return 0

    with open(ZH_JSON, 'w', encoding='utf-8') as f:
        json.dump(zh, f, ensure_ascii=False, indent=2, sort_keys=True)
    print(f'\nwrote {ZH_JSON}')
    return 0


if __name__ == '__main__':
    sys.exit(main())
