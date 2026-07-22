"""
Extract item and buff icons from WinTerrEdit data files.
Reads DemoFile/WinTerrEdit/itemIDs.txt and buffIDs.txt,
extracts base64-encoded 32x32 PNG icons,
outputs as JSON dictionaries for embedding in the app.
"""
import json
import os
import sys

BASE_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC_ITEMS = os.path.join(BASE_DIR, "DemoFile", "WinTerrEdit", "itemIDs.txt")
SRC_BUFFS = os.path.join(BASE_DIR, "DemoFile", "WinTerrEdit", "buffIDs.txt")
OUT_ITEMS = os.path.join(BASE_DIR, "Data", "icons_items.json")
OUT_BUFFS = os.path.join(BASE_DIR, "Data", "icons_buffs.json")

def extract_items():
    """Parse itemIDs.txt: ID,Name,Internal,base64PNG"""
    icons = {}
    missing = 0
    with open(SRC_ITEMS, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            parts = line.split(",", 3)  # Max 3 splits: ID, Name, Internal, base64
            if len(parts) < 4:
                continue
            try:
                item_id = int(parts[0])
                base64_data = parts[3].strip()
                if base64_data:
                    icons[str(item_id)] = base64_data
                else:
                    missing += 1
            except ValueError:
                continue

    print(f"Items extracted: {len(icons)} icons, {missing} missing")
    return icons

def extract_buffs():
    """Parse buffIDs.txt: ID,Name,Internal,Type,base64PNG"""
    icons = {}
    missing = 0
    with open(SRC_BUFFS, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            parts = line.split(",", 4)  # Max 4 splits: ID, Name, Internal, Type, base64
            if len(parts) < 5:
                continue
            try:
                buff_id = int(parts[0])
                base64_data = parts[4].strip()
                if base64_data:
                    icons[str(buff_id)] = base64_data
                else:
                    missing += 1
            except ValueError:
                continue

    print(f"Buffs extracted: {len(icons)} icons, {missing} missing")
    return icons

def main():
    print("Extracting item icons...")
    item_icons = extract_items()
    with open(OUT_ITEMS, "w", encoding="utf-8") as f:
        json.dump(item_icons, f, ensure_ascii=False)
    print(f"  -> {OUT_ITEMS} ({os.path.getsize(OUT_ITEMS) / 1024:.0f} KB)")

    print("Extracting buff icons...")
    buff_icons = extract_buffs()
    with open(OUT_BUFFS, "w", encoding="utf-8") as f:
        json.dump(buff_icons, f, ensure_ascii=False)
    print(f"  -> {OUT_BUFFS} ({os.path.getsize(OUT_BUFFS) / 1024:.0f} KB)")

    print("Done!")

if __name__ == "__main__":
    main()
