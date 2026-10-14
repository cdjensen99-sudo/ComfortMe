# Hardwire99 translations

One JSON file per language covers **all Hardwire99 mods**. ComfortMe keys are `comfortme_*`. Later mods add their own prefixed keys to the same files (`othermod_*`, and so on). Do **not** translate Valheim piece names such as Bearskin Rug.

GitHub source of truth (all mods download from here):

https://github.com/cdjensen99-sudo/translations

Only `English.json` ships in the ComfortMe zip. Other languages are downloaded from GitHub into the plugin `translations` folder at runtime. Keep the full set of JSON files here as the source copies.

## How to review

1. Open the file for your language (`French.json`, `German.json`, …).
2. Change the text on the **right** of each colon. Leave the keys unchanged.
3. Keep the quotes and commas. UTF-8 is required.
4. Send the edited file back.

`English.json` is the source of truth. Missing keys in another language fall back to English.

| Key | What the player sees |
|-----|----------------------|
| `comfortme_category` | Hammer **Categories** row name |
| `comfortme_this_room` | Panel header, then `: N` |
| `comfortme_unsheltered` | After the total when you are not under a roof |
| `comfortme_base` | Always-on +1 |
| `comfortme_shelter` | Roof +1 |
| `comfortme_missing` | After Shelter when you are not sheltered |
| `comfortme_group_*` | Vanilla comfort group labels in the panel |
