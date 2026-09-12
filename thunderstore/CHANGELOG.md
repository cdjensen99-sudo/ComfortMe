# Changelog

## 1.1.0 — Comfort category

- **Comfort** row on the 1.0 hammer **Categories** list. It is a virtual usage tag, not a 10th native `PieceCategory`, so Furniture and Lighting stay where vanilla put them.
- That list is a this-room shopping order: **upgrades first**, then **unlit**, then already counting, then lower pieces.
- **Amber `+N` chips** for pieces that are already in range but not counting until you add fuel or light them.
- **This room** panel is slim: Base, Shelter, and only the pieces that currently count. The hover group ladder is gone; use the Comfort row for that.

## 1.0.0 — Valheim 1.0

- Requires **Valheim 1.0** (tested on 1.0.7) and BepInEx Pack 5.4.2350.
- **1.0 hammer HUD** — `+N` chips on every comfort piece across Categories, usage tags, Materials, Recent, and Favorites.
- **This room** panel sits to the **right** of the hammer and stays open with the menu. It lists Base, Shelter, and the pieces that currently count toward vanilla comfort.
- Hovering a comfort piece still appends that group's ladder (off-tab labels and **(unlit)** included).
- Follows vanilla `SE_Rested.CalculateComfortLevel`, including 1.0 comfort groups (Decor, Display, Garland, Lantern, Leisure, and the rest).
- Does **not** change comfort values, recipes, or rested time.

## 0.1.0 — Initial release

- **This room: N** — live vanilla comfort covering you, under the bottom-right of the hammer grid.
- **`+N` chips** on every discovered comfort piece, on every hammer tab. The number is that piece’s comfort.
- **Green / grey / red** vs *this room*: green would raise comfort, grey is equal or already in range, red is worse than the current group max.
- **Group list** when you hover or select a comfort piece (stool vs chair vs throne, fire ladder, and so on). Off-tab pieces are labeled (e.g. Hearth — Misc).
- **Unlit / unfueled** pieces in range (campfire, hearth, hot tub) are treated as already placed, not as something you still need to build. The list marks them **(unlit)**.
- Does **not** change comfort values, recipes, or rested time.
