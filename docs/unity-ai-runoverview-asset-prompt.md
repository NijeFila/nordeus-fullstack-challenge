# Run Overview — Asset Generation Prompts

These prompts are only for generating image assets (background and frame textures), not scene logic. Drop them into Unity's image-generation panel one at a time and import the result under `Assets/Art/UI/`.

The current scene already uses `RunOverviewBackground.png`, `ParchmentPanel.png`, `FantasyPanelFrame.png`, `ButtonFrame.png`, `GoldIcon.png`, `NodeToken.png`, and `MapRouteLine.png`. Only regenerate them if you specifically want a different visual direction.

## 1. Full-screen background

Filename suggestion: `Assets/Art/UI/RunOverviewBackground.png`

```
A torchlit fantasy war-room or dungeon planning chamber. A long oak table dominates the lower third with rolled parchment maps, scattered candles, and a brass compass. The back wall is dark stone with two iron sconces casting a warm orange glow that fades into deep shadow at the edges. Composition is symmetrical and calm. Hand-painted oil-on-canvas style, dark fantasy, moody but readable. The center of the image is darker and uncluttered so UI panels and a route map can sit on top without competing for attention. 16:9 aspect ratio, 1920x1080, no characters, no on-screen text.
```

## 2. Parchment panel texture (sliced 9-patch)

Filename suggestion: `Assets/Art/UI/ParchmentPanel.png`

```
An aged parchment sheet seen from above, slightly torn corners, faint ink stains, warm cream tone. Edges have a darker ink wash so the texture holds up when sliced as a 9-patch UI panel. No drawings, no text, no maps printed on it. Square, 512x512, transparent background outside the parchment shape, soft shadow under the parchment.
```

## 3. Iron / wood panel frame (sliced 9-patch)

Filename suggestion: `Assets/Art/UI/FantasyPanelFrame.png`

```
A rectangular dark-iron frame with hammered metal corners and rounded rivets, wrapping a hollow center. Cool gunmetal grey with subtle reddish rust highlights. Designed to be sliced as a 9-patch UI border around panels. Square, 512x512, transparent center and transparent outside, no text.
```

## 4. Map route line tile

Filename suggestion: `Assets/Art/UI/MapRouteLine.png`

```
A weathered ink line drawn on parchment, slightly uneven, a single horizontal stroke that tiles cleanly along its length. Dark sepia ink, faint feathering at the edges. 256x32 pixels, transparent background, no decorations.
```

## 5. Map node token (frame around an icon)

Filename suggestion: `Assets/Art/UI/NodeToken.png`

```
A circular metal token, hammered iron rim, dark slate center where a small icon will be placed at runtime. Subtle rivets on the rim. Square, 256x256, transparent background, no text, no icon in the middle. The slate center should be a clean flat surface (light texture only) so a battle / shop / boss icon can sit on top.
```

## Notes

- Keep the colour family consistent: deep stone, warm amber/torchlight, faded parchment.
- Avoid characters, on-screen text, watermarks, or "AAA" lighting effects that compete with the UI overlay.
- Export PNG with transparency where applicable.
- After importing into Unity, set the Texture Type to "Sprite (2D and UI)", and for the parchment / frame textures set the Sprite Mesh Type to "Full Rect" and define 9-slice borders in the inspector so the assets stretch cleanly behind any panel size.
