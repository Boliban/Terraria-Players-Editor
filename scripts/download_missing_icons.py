"""
Improved icon downloader for Terraria Players Editor.
Downloads missing item icons from Terraria Wiki (terraria.wiki.gg).
- Reads Data/icons_items.json to find placeholder icons
- Queries Wiki Cargo API for exact filenames
- Downloads + resizes to 32x32 PNG, encodes as base64
- Saves progress incrementally (every 50 icons)
- Processes ALL missing items
- Regenerates icons_items.json on completion

Requirements: Python 3.10+, pip install Pillow requests
Usage: python scripts/download_missing_icons.py
"""
import json
import base64
import urllib.request
import urllib.parse
import gzip
import re
import html
import time
import os
import sys
from io import BytesIO
from collections import Counter

try:
    from PIL import Image
except ImportError:
    print("ERROR: Pillow not installed. Run: pip install Pillow")
    sys.exit(1)

try:
    import requests
except ImportError:
    print("ERROR: requests not installed. Run: pip install requests")
    sys.exit(1)

BASE_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ICONS_FILE = os.path.join(BASE_DIR, "Data", "icons_items.json")
PROGRESS_FILE = os.path.join(BASE_DIR, "Data", ".download_progress.json")

# Terraria Wiki API
WIKI_API = "https://terraria.wiki.gg/api.php"
USER_AGENT = "TerrariaPlayersEditor/1.0"

# Rate limiting (be nice to the wiki)
API_DELAY = 0.5      # seconds between cargo API calls
DOWNLOAD_DELAY = 0.3  # seconds between image downloads
SAVE_INTERVAL = 50    # save progress every N icons


def wiki_api(params):
    """Call the MediaWiki API with rate limiting."""
    url = WIKI_API + "?" + urllib.parse.urlencode(params)
    time.sleep(API_DELAY)
    try:
        req = urllib.request.Request(url, headers={"User-Agent": USER_AGENT,
                                                    "Accept-Encoding": "gzip"})
        resp = urllib.request.urlopen(req, timeout=15)
        data = resp.read()
        if resp.headers.get("Content-Encoding") == "gzip":
            data = gzip.decompress(data)
        return json.loads(data)
    except Exception as e:
        print(f"  API error: {e}")
        return {}


def get_wiki_filename(item_id):
    """Get the exact wiki filename for an item from the Cargo API."""
    try:
        result = wiki_api({
            "action": "cargoquery",
            "format": "json",
            "tables": "Items",
            "fields": "image",
            "where": f"itemid={item_id}",
            "limit": "1"
        })
        if result.get("cargoquery"):
            image_wiki = result["cargoquery"][0]["title"].get("image", "")
            match = re.search(r"\[\[File:([^|\]]+)", image_wiki)
            if match:
                return html.unescape(match.group(1))
    except Exception as e:
        print(f"  Cargo query failed for item {item_id}: {e}")
    return None


def download_icon(filename):
    """Download an icon from the wiki and return as base64 PNG."""
    encoded = urllib.parse.quote(filename)
    url = f"https://terraria.wiki.gg/wiki/Special:FilePath/{encoded}"
    time.sleep(DOWNLOAD_DELAY)
    try:
        req = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
        resp = urllib.request.urlopen(req, timeout=10)
        img_data = resp.read()
        if len(img_data) < 50:
            return None
        img = Image.open(BytesIO(img_data)).convert("RGBA")
        img = img.resize((32, 32), Image.LANCZOS)
        buf = BytesIO()
        img.save(buf, format="PNG")
        return base64.b64encode(buf.getvalue()).decode("utf-8")
    except Exception as e:
        print(f"  Download failed for {filename}: {e}")
        return None


def load_progress():
    """Load progress from previous run for resumability."""
    if os.path.exists(PROGRESS_FILE):
        with open(PROGRESS_FILE, "r", encoding="utf-8") as f:
            return json.load(f)
    return {}


def save_progress(downloaded_ids):
    """Save downloaded IDs so we can resume if interrupted."""
    with open(PROGRESS_FILE, "w", encoding="utf-8") as f:
        json.dump(list(downloaded_ids), f)


def main():
    print("=" * 60)
    print("Terraria Players Editor - Icon Downloader")
    print("Downloads missing item icons from terraria.wiki.gg")
    print("=" * 60)

    # Load icons
    print("\n[1/4] Loading icons_items.json...")
    with open(ICONS_FILE, "r", encoding="utf-8") as f:
        icons = json.load(f)
    print(f"  Loaded {len(icons)} item entries")

    # Find placeholder icon (item -1)
    placeholder_b64 = icons.get("-1", "")
    if not placeholder_b64:
        print("ERROR: Cannot find placeholder icon (item -1)")
        return

    # Find items that use the placeholder
    print("\n[2/4] Finding items with placeholder icons...")
    missing_ids = []
    for id_str, b64 in icons.items():
        item_id = int(id_str)
        if item_id <= 0:
            continue  # Skip -1 and 0
        if b64 == placeholder_b64:
            missing_ids.append(item_id)

    missing_ids.sort()
    print(f"  Found {len(missing_ids)} items needing icons")
    print(f"  ID range: {missing_ids[0]} - {missing_ids[-1]}")

    if not missing_ids:
        print("\nAll icons downloaded! Nothing to do.")
        if os.path.exists(PROGRESS_FILE):
            os.remove(PROGRESS_FILE)
        return

    # Load previous progress
    progress = load_progress()
    already_done = set(progress) if progress else set()
    remaining = [i for i in missing_ids if str(i) not in already_done]

    if already_done:
        print(f"  Resuming: {len(already_done)} already downloaded, {len(remaining)} remaining")

    if not remaining:
        print("\nAll items downloaded in previous session. Applying...")
        # Reapply previously downloaded icons
        for id_str, b64 in progress.items():
            icons[id_str] = b64
        with open(ICONS_FILE, "w", encoding="utf-8") as f:
            json.dump(icons, f, ensure_ascii=False)
        print(f"  Saved {len(progress)} icons to icons_items.json")
        os.remove(PROGRESS_FILE)
        return

    # Download
    downloaded = 0
    failed = 0
    downloaded_ids = dict(progress) if progress else {}

    total = len(remaining)
    print(f"\n[3/4] Downloading {total} missing icons...")
    print(f"  (Progress saved every {SAVE_INTERVAL} icons, Ctrl+C to stop)")

    try:
        for i, item_id in enumerate(remaining):
            # Get wiki filename
            filename = get_wiki_filename(item_id)

            if not filename:
                failed += 1
                if failed <= 5:
                    print(f"  [{i+1}/{total}] Item {item_id}: no wiki filename found")
                continue

            # Download icon
            icon_b64 = download_icon(filename)

            if icon_b64:
                icons[str(item_id)] = icon_b64
                downloaded_ids[str(item_id)] = icon_b64
                downloaded += 1
            else:
                failed += 1
                if failed <= 5:
                    print(f"  [{i+1}/{total}] Item {item_id}: download failed")

            # Progress report
            if (downloaded + failed) % 50 == 0:
                print(f"  Progress: {downloaded} downloaded, {failed} failed, "
                      f"{total - downloaded - failed} remaining")

            # Save incrementally
            if downloaded > 0 and downloaded % SAVE_INTERVAL == 0:
                save_progress(downloaded_ids)
                print(f"  [Saved {len(downloaded_ids)} icons to progress file]")

    except KeyboardInterrupt:
        print(f"\n\nInterrupted! Progress saved: {len(downloaded_ids)} icons")
        save_progress(downloaded_ids)
        print("Run this script again to resume.")
        return

    # Final save
    print(f"\n[4/4] Saving results...")
    print(f"  Downloaded: {downloaded}")
    print(f"  Failed: {failed}")

    # Save progress file
    save_progress(downloaded_ids)

    # Write updated icons_items.json
    with open(ICONS_FILE, "w", encoding="utf-8") as f:
        json.dump(icons, f, ensure_ascii=False, separators=(",", ":"))

    file_size_kb = os.path.getsize(ICONS_FILE) / 1024
    print(f"  Saved icons_items.json ({file_size_kb:.0f} KB)")

    # Clean up progress file if all done
    if failed == 0:
        os.remove(PROGRESS_FILE)
        print("  All icons downloaded! Progress file removed.")
    else:
        print(f"  {failed} items still need icons. Run again to retry failed items.")
        print(f"  Progress saved to Data/.download_progress.json")

    print("\nDone! Rebuild the project for changes to take effect.")
    print("  dotnet build")


if __name__ == "__main__":
    main()
