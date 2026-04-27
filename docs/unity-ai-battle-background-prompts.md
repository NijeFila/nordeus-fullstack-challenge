# Battle Background Asset Prompts

These are focused prompts for generating one battle background per environment. The current backend ships eight environments; their ids match the entries below. Use one prompt at a time, import the resulting PNG under `Assets/Art/UI/Battle/` (or wherever you store battle art), and drop the imported sprite into the matching entry on the `BattleEnvironmentVisualCatalog` asset.

## Shared style guidance

Every prompt should include the same baseline style direction so backgrounds feel like they belong to the same project:

- 16:9 landscape, 1920x1080
- dark fantasy, painted look (oil-on-canvas / matte painting feel)
- moody but readable; the centre of the frame is calmer so combatant portraits and HP bars sit cleanly on top
- warm torchlight or moonlight accents on dark stone, foliage, or fabric
- no characters, no creatures, no weapons in the foreground
- no UI, no on-screen text, no watermarks
- composition is symmetrical and stable; avoid busy diagonals
- leave a darker band along the bottom third for the move buttons

## 1. `training_fields`

```
A wide grass training ground at dusk. Wooden practice posts and straw dummies lined up symmetrically in the mid-ground, simple wooden fence, distant tree line. Soft warm sunset behind a low hill. The grass is slightly trampled. Calm and inviting; this is the easiest fight of the run. Painted dark fantasy style, 16:9 landscape, no characters, no UI, no text. The centre and bottom of the frame are calmer than the edges so combatant UI sits on top cleanly.
```

## 2. `arcane_library`

```
The interior of a vast arcane library. Tall dark wood shelves filled with worn books reach out of frame on both sides, warm candle glow on parchment, faint motes of magical dust drifting in the air. A central reading lectern stands empty in the foreground, slightly angled. Subtle blue magical glow mixed with warm candlelight. Dark fantasy painted style, 16:9 landscape, no characters, no UI, no text, calmer centre and bottom for UI overlays.
```

## 3. `spider_nest`

```
A tangled cavern interior strewn with thick web. Sticky web strands stretch from wall to wall, partly catching dim greenish light from glowing fungi on the rocks. Damp stone, deep shadow, the suggestion of dark holes in the back wall. No spiders visible. Painted dark fantasy, 16:9 landscape, moody and oppressive but the centre and bottom remain readable for combatant UI. No characters, no UI, no text.
```

## 4. `dark_altar`

```
A ruined ritual chamber with a low stone altar in the centre, blood-stained but no figures present. Black candles ring the altar. Carved runes on the floor pulse faintly red. Tall, broken pillars on either side. Cold purple-blue moonlight from a hole in the ceiling mixes with the warm red rune glow. Painted dark fantasy, 16:9 landscape, ominous and quiet. No characters, no UI, no text. Bottom of the frame stays uncluttered.
```

## 5. `dragon_peak`

```
A high mountain peak above the clouds. A flat stone plateau in the foreground, jagged dark rock spires behind, and a deep red sunset breaking through low storm clouds. Faint embers drift through the air. A subtle hint of scorched ground in the centre. No dragon visible. Painted dark fantasy, 16:9 landscape, dramatic but readable. No characters, no UI, no text.
```

## 6. `crypt`

```
A long stone crypt corridor seen head-on. Ancient sarcophagi line the side walls, half in shadow. A narrow shaft of pale moonlight enters from a high crack overhead. Cold blue-grey palette with faint green moss in the corners. Slightly damp, oppressive. Painted dark fantasy, 16:9 landscape, no characters, no UI, no text, calmer centre and bottom.
```

## 7. `ancient_forest`

```
The depths of an ancient forest at twilight. Massive twisted tree trunks frame either side of the composition, with knotted roots crawling across the ground. Soft mist clings to the moss-covered floor. A faint warm shaft of sunlight breaks through high branches. Deep green and brown palette with hints of amber. Painted dark fantasy, 16:9 landscape, no characters, no UI, no text. Reads readable in the centre and bottom for UI overlays.
```

## 8. `ember_chamber`

```
A vaulted underground chamber with a glowing crater of embers in the centre of the floor. Iron-banded walls, soot-darkened stone, faint heat haze rising. Warm orange and red light bouncing off the walls, with deep black shadows in the corners. The frame is composed so the embers sit slightly low, leaving room above for combatant portraits. Painted dark fantasy, 16:9 landscape, no characters, no UI, no text.
```

## After import

1. In Unity, set each imported texture's *Texture Type* to **Sprite (2D and UI)**.
2. Make sure *Mesh Type* is **Full Rect** so the sprite stretches cleanly behind the Battle Canvas.
3. Open the `BattleEnvironmentVisualCatalog` asset (created via `Assets > Create > Nordeus > Battle Environment Visual Catalog`).
4. For each entry, set:
   - `environmentId` to one of the ids above (lowercase, exact match).
   - `backgroundSprite` to the imported sprite.
   - `overlayColor` to white `(1,1,1,1)` unless you want to tint the scene (e.g. `(0.85, 0.85, 1, 1)` for a cool lean on `crypt`, `(1, 0.85, 0.7, 1)` for a warm lean on `ember_chamber`).
5. Drag the catalog asset into `BattleController.environmentVisualCatalog` in the Battle scene's inspector.
