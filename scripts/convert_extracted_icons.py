"""
Convert extracted Terraria icons from DemoFile/Item/ and DemoFile/Buff/
into base64-encoded JSON files for embedding (Data/icons_items.json, Data/icons_buffs.json).

The DemoFile directory is .gitignored — this script reads the PNGs from disk
and produces the JSON files that ARE tracked in the project.

Usage: python scripts/convert_extracted_icons.py
"""
import json
import base64
import os
import sys
import re
from io import BytesIO

try:
    from PIL import Image
except ImportError:
    print("ERROR: Pillow not installed. Run: pip install Pillow")
    sys.exit(1)

BASE_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ITEM_DIR = os.path.join(BASE_DIR, "DemoFile", "Item")
BUFF_DIR = os.path.join(BASE_DIR, "DemoFile", "Buff")
DATA_DIR = os.path.join(BASE_DIR, "Data")

OUT_ITEMS = os.path.join(DATA_DIR, "icons_items.json")
OUT_BUFFS = os.path.join(DATA_DIR, "icons_buffs.json")


def png_to_base64(path):
    """Read a PNG at native dimensions, return base64 string.
    No resizing — IconService handles contain-scaling at load time."""
    with Image.open(path) as img:
        img = img.convert("RGBA")
        buf = BytesIO()
        img.save(buf, format="PNG")
        return base64.b64encode(buf.getvalue()).decode("utf-8")


def find_png_files(directory):
    """Find all PNG files, return dict mapping {id_int: path}."""
    result = {}
    if not os.path.isdir(directory):
        print(f"  WARNING: Directory not found: {directory}")
        return result
    for fname in os.listdir(directory):
        if not fname.endswith(".png"):
            continue
        # Extract numeric ID: "Item_123.png" → 123, "Buff_45.png" → 45, "Buff.png" → 0
        match = re.search(r'(\d+)', fname)
        if match:
            result[int(match.group(1))] = os.path.join(directory, fname)
        elif fname.lower().startswith("buff") and fname.lower().endswith(".png"):
            # "Buff.png" with no number = ID 0
            result[0] = os.path.join(directory, fname)
    return result


def process_items():
    """Convert Item/*.png → icons_items.json."""
    print("\n=== Processing Item Icons ===")
    print(f"  Source: {ITEM_DIR}")

    pngs = find_png_files(ITEM_DIR)
    print(f"  Found {len(pngs)} PNG files")

    # Also read existing JSON to preserve any icons we don't have PNGs for
    existing = {}
    if os.path.exists(OUT_ITEMS):
        with open(OUT_ITEMS, "r", encoding="utf-8") as f:
            existing = json.load(f)
        print(f"  Existing JSON has {len(existing)} entries")

    # Add -1 (Unknown) as transparent placeholder if not in PNGs
    if -1 not in pngs and "-1" in existing:
        # Keep existing -1 entry
        pass

    icons = {}
    updated = 0
    kept = 0
    failed = 0

    for id_int, path in sorted(pngs.items()):
        try:
            b64 = png_to_base64(path)
            icons[str(id_int)] = b64
            updated += 1
        except Exception as e:
            failed += 1
            if failed <= 5:
                print(f"  ERROR ID {id_int}: {e}")
            # Fall back to existing entry if available
            key = str(id_int)
            if key in existing:
                icons[key] = existing[key]
                kept += 1

    # Write output
    with open(OUT_ITEMS, "w", encoding="utf-8") as f:
        json.dump(icons, f, ensure_ascii=False, separators=(",", ":"))

    size_kb = os.path.getsize(OUT_ITEMS) / 1024
    print(f"  Output: {OUT_ITEMS}")
    print(f"  Updated: {updated}, Failed: {failed}, Size: {size_kb:.0f} KB")


def process_buffs():
    """Convert Buff/*.png → icons_buffs.json."""
    print("\n=== Processing Buff Icons ===")
    print(f"  Source: {BUFF_DIR}")

    pngs = find_png_files(BUFF_DIR)
    print(f"  Found {len(pngs)} PNG files")

    existing = {}
    if os.path.exists(OUT_BUFFS):
        with open(OUT_BUFFS, "r", encoding="utf-8") as f:
            existing = json.load(f)
        print(f"  Existing JSON has {len(existing)} entries")

    icons = {}
    updated = 0
    failed = 0

    for id_int, path in sorted(pngs.items()):
        try:
            b64 = png_to_base64(path)
            icons[str(id_int)] = b64
            updated += 1
        except Exception as e:
            failed += 1
            if failed <= 5:
                print(f"  ERROR ID {id_int}: {e}")
            key = str(id_int)
            if key in existing:
                icons[key] = existing[key]

    with open(OUT_BUFFS, "w", encoding="utf-8") as f:
        json.dump(icons, f, ensure_ascii=False, separators=(",", ":"))

    size_kb = os.path.getsize(OUT_BUFFS) / 1024
    print(f"  Output: {OUT_BUFFS}")
    print(f"  Updated: {updated}, Failed: {failed}, Size: {size_kb:.0f} KB")


def main():
    print("=" * 60)
    print("Terraria Icon Converter")
    print("Converts extracted PNGs → base64 JSON")
    print("=" * 60)

    if not os.path.isdir(ITEM_DIR):
        print(f"\nERROR: Item directory not found: {ITEM_DIR}")
        print("Extract item icons to DemoFile/Item/ first.")
        sys.exit(1)

    process_items()
    process_buffs()

    # Clean up download progress file (no longer needed with extracted icons)
    progress_file = os.path.join(DATA_DIR, ".download_progress.json")
    if os.path.exists(progress_file):
        os.remove(progress_file)
        print("\nRemoved old download progress file.")

    print("\nDone! Rebuild the project: dotnet build")


if __name__ == "__main__":
    main()
