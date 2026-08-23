# ComfortMe

**Stand in a room. Open the hammer. See which furniture would raise *this* room’s comfort.**

ComfortMe does **not** change comfort values, recipes, or rested time. It only marks the build menu.

**Current version:** 0.1.0  
**Requires:** BepInEx Pack for Valheim  
**Install:** **Client-side only.** No server install. Other players do not need the mod.

**Links**
- **[Team Extreme Discord](https://discord.gg/cCNG8xKXMn)** — setup help, bug reports, and updates
- **[GitHub — ComfortMe](https://github.com/cdjensen99-sudo/ComfortMe)** — source and issues

---

## How to use it

1. Stand in the room you care about (comfort is a **10 m** bubble around *you*).
2. Equip the **hammer** and **right-click** to open the piece grid.
3. Look at **This room: N** under the **bottom-right** of the grid. That is vanilla comfort covering you right now (base + shelter + furniture).
4. Scan the **`+N` chips** on the icons. The number is that piece’s comfort. The **color** is compared to *this room*, not to the best piece in the whole game.
5. Hover or select a comfort piece. The same panel lists every **discovered** piece in that group (other tabs named, e.g. Hearth on Misc).

Unknown recipes stay hidden, same as vanilla. A greyed-out icon (you cannot afford it yet) can still wear a green chip: that is the shopping hint.

---

## The `+N` chip

Every comfort piece on **every hammer tab** gets a small chip in the **top-right** of its icon.

| Chip | Example | Meaning |
|------|---------|---------|
| **`+3` green** | Throne while this room only has a chair | Placing this **would raise** this room’s comfort |
| **`+2` grey** | Hearth while a hearth (or equal fire) is already counting | Same as what already covers you — **no gain** from placing another |
| **`+1` red** | Campfire while a hearth `+2` is already counting | **Worse** than the current group max — placing it does **not** raise the total |
| **`+2` grey** (hot tub / fire in range but cold) | Hot tub with no wood | The piece is **already here**. Lighting it adds comfort; building a second one does not |

The chip is **not** the word “Comfort”. It is only `+N`. Vanilla already greys unaffordable icons and paints the selection cell **blue** — ComfortMe does not tint the artwork and never uses blue.

---

## Color codes (same on the chip and in the room list)

| Color | On the grid | In the room list |
|-------|-------------|------------------|
| **Green** | Would raise this room | Missing unique piece, or a better piece in that group |
| **Grey** | Equal to what’s already covering you, **or** already in range | Current group max / already placed |
| **Red** | Lower than this room’s group max | Would not beat what’s already counting |
| **Amber** | (chip stays grey) | **Unlit** — in range, but not contributing until you add fuel / light it |

Vanilla only counts the **best** piece in a group (Fire, Bed, Chair, Table, Banner, Carpet). Unique pieces (hot tub, armour stand, …) stack **once each**. Unlit fires and a cold hot tub contribute **0** until they are lit.

---

## Screenshots

Furniture tab — chips on every comfort piece. Green armour stand = not in this room yet. Grey hot tub = already here, unlit until you add wood. **This room: 11** is the live total.

![Furniture tab with color-coded +N chips](https://raw.githubusercontent.com/cdjensen99-sudo/ComfortMe/main/Images/ComfortLevels.png)

Fire group — only the best fire counts. Hearth is grey `+2` because fire here is already `+2`. Campfire and bonfire are red `+1`. Hover the hearth to see the whole fire list (including Hanging Brazier on Furniture).

![Misc tab fire pieces: red +1 campfire/bonfire, grey +2 hearth](https://raw.githubusercontent.com/cdjensen99-sudo/ComfortMe/main/Images/Comfortfires.png)

---

## What this is not

| Mod | Job |
|-----|-----|
| **Hygge** | Stats for the *selected* piece (group, value, nearest same-group). Green there means “best in the whole game,” not “helps *here*.” |
| **GetComfortable** | F4 list of pieces *already contributing* where you stand. |
| **ComfortMe** | Which **hammer icons** would raise **this** room. |

You can run all three. ComfortMe does not clone Hygge’s inspector or GetComfortable’s F4 list.

---

## Config (`BepInEx/config/hardwire99.comfortme.cfg`)

| Setting | Default | Effect |
|---------|---------|--------|
| `Enabled` | true | Kill switch |
| `ShowValueBadge` | true | `+N` chips on the grid |
| `BadgeUpgradesOnly` | **false** | `true` = only green chips. Default = green / grey / red on every comfort piece |
| `ShowGroupCatalog` | true | Room total + group list when a comfort piece is selected |
| `HighlightPolicy` | AnyIncrease | AnyIncrease / BestAvailable / UpgradeOnly (green chips only) |
| `RequireMaterials` | false | `true` = hide chips you cannot afford. Default leaves the chip on greyed icons |
| `DebugLogging` | false | Extra BepInEx log spam |

---

## Requirements

- BepInEx Pack for Valheim
