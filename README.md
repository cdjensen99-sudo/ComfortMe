# ComfortMe

Valheim hammer helper: while you stand in a room, mark build-menu pieces that would **raise that room’s comfort**.

Does not change comfort values, recipes, or rested time. Complements **Hygge** (selected-piece stats) and **GetComfortable** (F4 contributor list). ComfortMe answers “which hammer icon would help *here*?”

**Current version:** 0.1.0

## What you should see

Open the hammer piece grid (right-click) while standing in a room:

1. **This room: N** on the right of the grid — vanilla comfort covering you (base + shelter + furniture).
2. Green **`+N`** chips on icons that would raise that score (throne `+3` if seating here is only a chair).
3. Hover or select a chair (or any comfort piece) — the panel lists discovered pieces in that group, colored vs this room (red lower, grey equal, green upgrade). Off-tab upgrades show a tab name, e.g. Hearth (Misc).

Unknown recipes stay hidden, same as vanilla. Greyed unaffordable icons can still wear a green chip.

## Build

```powershell
.\build.ps1 -Deploy
```

Output: `artifacts\ComfortMe.dll` (copied to the r2modman Testing profile when that folder exists).

## Requirements

- BepInEx Pack for Valheim
