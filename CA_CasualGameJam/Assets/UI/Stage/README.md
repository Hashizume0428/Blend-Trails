# Stage screen assets

Source: Figma `CA_CasualGameJam`, stage frame `9:207`.
https://www.figma.com/design/ng7iMmp8bKzUcDibEPRfqr/CA_CasualGameJam?node-id=9-207

These local PNG exports retain the original layers, icon proportions, corners and shadows. They are static backgrounds or button artwork; the interactive board continues to use the existing tile sprites and game state.

| File | Figma node | Export geometry in design units |
|---|---|---|
| BoardPanel.png | 9:209 | 1200 × 1200, including shadow; background itself 1192 × 1192 |
| ReloadPanel.png | 9:247 | 1200 × 429, including shadow; background itself 1192 × 421 |
| ReloadAll.png | 9:257 | 260 × 260, including shadow; button itself 252 × 252 |
| Home.png | 9:339 | 191 × 191, including shadow; button itself 183 × 183 |
| ReloadRed.png | 9:248 | 260 × 260, including shadow; button itself 252 × 252 |
| ReloadBlue.png | 9:251 | 260 × 260, including shadow; button itself 252 × 252 |
| MixPurple.png | 9:267 | 470 × 110 |

The file's export settings produce 4× renders except the legend. Import sizes are capped at 2048 for this portrait prototype. StageScreenBuilder assigns full-rectangle sprites and preserves aspect ratios and export padding. No remote Figma asset URL is used at runtime.

The stage label uses Inter Regular from the official Inter 4.1 distribution. Its font and license are in `Assets/Fonts/Inter/`.
https://github.com/rsms/inter/releases/tag/v4.1

## Runtime palette rotation

The original PNGs remain the shared templates. `StagePaletteUI.shader` replaces the red, blue and purple ink with the current stage's first, second and mixed colors, preserving alpha, white icons, gray symbols, antialiasing and shadows. Red/blue stages retain the original default UI material. Each other stage creates one shared material for its two reload buttons and legend, then destroys it with the HUD.

`StagePaletteVisuals.asset` references all three existing start and ban sprites and the shader, so they are included in player builds. The yellow reload and orange/green legends are rendered from the templates; separate bitmap variants are unnecessary. The historical object names `ReloadRed`, `ReloadBlue` and `MixPurple` remain to preserve scene references, but their displayed colors and actions follow the active palette. The right button uses the first color and the middle button the second.

Use **Color Puzzle/Apply Stage Color Rotation** to bind the shared asset and legend references in all thirty saved stages without regenerating puzzle conditions. StageScreenBuilder, PrototypeSceneBuilder and ReferenceStageBuilder also keep these bindings when generating or updating stages.
