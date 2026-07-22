import struct, sys
sys.stdout.reconfigure(encoding='utf-8')
from Crypto.Cipher import AES

def read_str1(b, o):
    """Read 1-byte length prefix + UTF-8 string"""
    length = b[o]; o += 1
    s = b[o:o+length].decode('utf-8')
    return s, o + length

with open('DemoFile/4OL.plr', 'rb') as f:
    data = f.read()
key = 'h3y_gUyZ'.encode('utf-16-le')
cipher = AES.new(key, AES.MODE_CBC, iv=key)
d = cipher.decrypt(data)
pad = d[-1]; d = d[:-pad]

class P:
    def __init__(self, b): self.b = b; self.o = 0
    def ri(self): v = struct.unpack_from('<i', self.b, self.o)[0]; self.o += 4; return v
    def rl(self): v = struct.unpack_from('<q', self.b, self.o)[0]; self.o += 8; return v
    def rb(self): v = self.b[self.o]; self.o += 1; return v
    def rbool(self): v = self.b[self.o] != 0; self.o += 1; return v
    def rs(self): s, self.o = read_str1(self.b, self.o); return s
    def rshort(self): v = struct.unpack_from('<h', self.b, self.o)[0]; self.o += 2; return v
    def rbytes(self, n): v = list(self.b[self.o:self.o+n]); self.o += n; return v
    def skip(self, n): self.o += n
    def pos(self): return self.o
    def rstr_fixed(self, n):
        s = self.b[self.o:self.o+n].decode('utf-8'); self.o += n; return s
    def remaining(self): return len(self.b) - self.o

p = P(d)
print(f'File={len(data)}B Decrypted={len(d)}B')

v = p.ri(); print(f'Version: {v}')
# "relogic" is fixed 7 bytes, NO prefix!
magic = p.rstr_fixed(7); print(f'Magic: {magic}')
ft = p.rb(); print(f'FileType: {ft}')
rev = p.ri(); print(f'Revision: {rev}')
fav = p.rl(); print(f'Favorite: {fav}')

name = p.rs(); print(f'Name: "{name}"')
diff = p.rb(); print(f'Difficulty: {diff}')
pt = p.rl(); print(f'PlayTime: {pt}')

hair = p.ri(); print(f'HairStyle: {hair}')
hd = p.rb(); print(f'HairDye: {hd}')
hv = [p.rbool() for _ in range(10)]; print(f'HideVisual: {hv}')
hm = [p.rbool() for _ in range(5)]; print(f'HideMisc: {hm}')
sv = p.rb(); print(f'SkinVariant: {sv}')
for cn in ['Hair','Skin','Eyes','Shirt','UnderShirt','Pants','Shoes']:
    c = p.rbytes(3); print(f'{cn}Color: {c}')

hl = p.ri(); print(f'Health: {hl}')
mh = p.ri(); print(f'MaxHealth: {mh}')
mn = p.ri(); print(f'Mana: {mn}')
mm = p.ri(); print(f'MaxMana: {mm}')

ea = p.rbool(); print(f'ExtraAccessory: {ea}')
dd2 = p.rbool(); print(f'downedDD2: {dd2}')
ubt = p.rbool(); print(f'UnlockedBiomeTorches: {ubt}')
using_bt = p.rbool(); print(f'UsingBiomeTorches: {using_bt}')
aab = p.rbool(); print(f'AteArtisanBread: {aab}')
uac = p.rbool(); print(f'UsedAegisCrystal: {uac}')
uaf = p.rbool(); print(f'UsedAegisFruit: {uaf}')
uarc = p.rbool(); print(f'UsedArcaneCrystal: {uarc}')
ugp = p.rbool(); print(f'UsedGalaxyPearl: {ugp}')
ugw = p.rbool(); print(f'UsedGummyWorm: {ugw}')
uamb = p.rbool(); print(f'UsedAmbrosia: {uamb}')
usc = p.rb(); print(f'UnlockedSuperCart: {usc}')
esc = p.rbool(); print(f'EnabledSuperCart: {esc}')
tm = p.ri(); print(f'TaxMoney: {tm}')
print(f'---o={p.pos()}---')

# Equipment
print('\n=== Equipment ===')
for i in range(3):
    aid = p.ri(); apr = p.rb(); print(f'Armor[{i}]: id={aid} prefix={apr}')
for i in range(3):
    vid = p.ri(); vpr = p.rb(); print(f'VanityArmor[{i}]: id={vid} prefix={vpr}')
acc_count = p.ri(); print(f'AccessoryCount: {acc_count}')
for i in range(acc_count):
    aid = p.ri(); apr = p.rb(); print(f'Accessory[{i}]: id={aid} prefix={apr}')
vacc_count = p.ri(); print(f'VanityAccCount: {vacc_count}')
for i in range(vacc_count):
    vid = p.ri(); vpr = p.rb(); print(f'VanityAcc[{i}]: id={vid} prefix={vpr}')
for i in range(10):
    did = p.ri(); dpr = p.rb(); print(f'ArmorDye[{i}]: id={did} prefix={dpr}')
print(f'---o={p.pos()}---')

# Inventory (50)
print('\n=== Inventory ===')
for i in range(50):
    iid = p.ri(); ist = p.rshort(); ipr = p.rb(); ifav = p.rbool()
    if iid != 0: print(f'Inv[{i}]: id={iid} stack={ist} pref={ipr} fav={ifav}')
print(f'---o={p.pos()}---')

# Coins 4
for i in range(4):
    cid = p.ri(); cst = p.rshort(); cpr = p.rb(); cfav = p.rbool()
    if cid != 0: print(f'Coin[{i}]: id={cid} stack={cst}')
# Ammo 4
for i in range(4):
    aid = p.ri(); ast = p.rshort(); apr = p.rb(); afav = p.rbool()
    if aid != 0: print(f'Ammo[{i}]: id={aid} stack={ast}')
print(f'---o={p.pos()}---')

# Misc equip 5
for i in range(5):
    mid = p.ri(); mpr = p.rb()
    if mid != 0: print(f'MiscEquip[{i}]: id={mid} prefix={mpr}')
for i in range(5):
    mdid = p.ri(); mdpr = p.rb()
    if mdid != 0: print(f'MiscEquipDye[{i}]: id={mdid}')
print(f'---o={p.pos()}---')

# Storage 4x40
for name, count in [('Piggy',40),('Safe',40),('Forge',40),('Void',40)]:
    for i in range(count):
        sid = p.ri(); sst = p.rshort(); spr = p.rb()
        if (i < 5 or sid != 0) and sid != 0:
            print(f'{name}[{i}]: id={sid} stack={sst} pref={spr}')
print(f'---o={p.pos()}---')

# Trash
tid = p.ri(); tst = p.rshort(); tpr = p.rb()
print(f'Trash: id={tid} stack={tst} pref={tpr}')
print(f'---o={p.pos()}---')

# Buffs
bc = 44
print('\n=== Buffs ===')
for i in range(bc):
    bt = p.ri()
    if bt != 0: print(f'BuffType[{i}]: {bt}')
for i in range(bc):
    btm = p.ri()
    if i < 5 or btm != 0:
        if btm != 0: print(f'BuffTime[{i}]: {btm}')
print(f'---o={p.pos()}---')

# Spawn points
spc = p.ri(); print(f'SpawnPointCount: {spc}')
for i in range(spc):
    sx = p.ri(); sy = p.ri(); swid = p.ri(); swn = p.rs()
    print(f'Spawn[{i}]: x={sx} y={sy} world={swid} name="{swn}"')
print(f'---o={p.pos()}---')

# Flags
hbl = p.rbool(); print(f'HotbarLocked: {hbl}')
hi = [p.rbool() for _ in range(13)]; print(f'HideInfo: {hi}')
aq = p.ri(); print(f'AnglerQuests: {aq}')
sb = p.ri(); print(f'SavedBartender: {sb}')
gs = p.ri(); print(f'GolferScore: {gs}')
print(f'---o={p.pos()}---')

bt = [p.rbool() for _ in range(12)]; print(f'BuilderToggles: {bt}')
dp = [p.ri() for _ in range(4)]; print(f'DPadBindings: {dp}')
ba = [p.ri() for _ in range(4)]; print(f'BuilderAccStatus: {ba}')
bql = p.ri(); print(f'BartenderQuestLog: {bql}')
print(f'---o={p.pos()}---')

dpve = p.ri(); print(f'DeathsPvE: {dpve}')
dpp = p.ri(); print(f'DeathsPvP: {dpp}')
pod = p.ri(); print(f'PotionDelay: {pod}')
mpd = p.ri(); print(f'ManaPotionDelay: {mpd}')
rcd = p.ri(); print(f'RestorationCd: {rcd}')
print(f'---o={p.pos()}---')

# Emotes
emc = p.ri(); print(f'EmoteCount: {emc}')
p.skip(emc * 4)
print(f'---o={p.pos()}---')

# Loadouts
cl = p.ri(); print(f'CurrentLoadout: {cl}')
# Skip loadout 2 + 3
def skip_loadout():
    for _ in range(3): p.ri(); p.rb()
    for _ in range(3): p.ri(); p.rb()
    count = p.ri(); [p.ri() or p.rb() for _ in range(count)]
    count = p.ri(); [p.ri() or p.rb() for _ in range(count)]
    for _ in range(10): p.ri(); p.rb()
    for _ in range(5): p.ri(); p.rb()
    for _ in range(5): p.ri(); p.rb()
skip_loadout()
skip_loadout()
print(f'---o={p.pos()} after loadouts---')

# Research
rc = p.ri(); print(f'ResearchCount: {rc}')
for i in range(min(rc, 5)):
    rn = p.rs(); rcnt = p.ri(); print(f'  {rn}: {rcnt}')
if rc > 5:
    for i in range(rc - 5):
        p.rs(); p.ri()
print(f'---o={p.pos()}---')

print(f'\nTotal: {len(d)} Read: {p.pos()} Remaining: {p.remaining()}')
if p.remaining() > 0:
    print(f'Remaining bytes hex: {" ".join(f"{b:02x}" for b in d[p.pos():])}')
