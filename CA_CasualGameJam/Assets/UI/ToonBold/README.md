# B / Toon Bold UI assets

The game is **Blend Trails: Color Puzzle**. `BlendTrailsLogo.png` and `BlendTrailsMix.png` are separate transparent sprites generated from the approved red + yellow / orange title. The original `ApprovedTitle.png` and rebranded `BlendTrailsTitle.png` are preserved as sources. The title's sky and clouds come entirely from the full-screen `ToonSkyGraphic`; no rectangular sky image is layered over it. A real button, play icon and progress-driven number are drawn separately. The cutout prompts are in `output/ui-polish/image-prompts.json`.

The title's logo and mixing motif stay red/yellow/orange regardless of progression. Gameplay uses the existing 5-stage red/blue/purple, red/yellow/orange, blue/yellow/green cycle.

`ResetArrowGenerated.png` is a standalone white circular arrow generated with genuine transparent alpha, shared by all three reset buttons and tinted navy for yellow. Unity imports it at a maximum of 256 pixels to retain smooth edges at its small UI display size. The other panels, tile faces/frames, buttons and navigation icons are code-native primitives authored by `Assets/Editor/ToonSkinBuilder.cs`. They use full-rectangle sprites with uncompressed alpha, no mipmaps, and sliced borders for responsive panels. No screenshot is used as the interactive board.

`Assets/Resources/ToonSkin.asset` references the shared assets. `StagePaletteVisuals.asset` references the three new start markers. Every tile retains its authored coordinates, start color roles, target colors, colliders and existing state logic.

The background is a separate camera-space Canvas, sorted behind the world-space board. It has no raycaster. Buttons and other controls remain on the existing interactive Canvas, and decorative graphics do not intercept input.

The clear card has a separate camera-space backdrop below the live solved board, with its heading, palette-colored success badge and next-stage action on the existing overlay Canvas. Its button label follows the next display number: Stage4 after clearing Stage3, including Stage31 and higher when authored puzzles are reused. The completed board retains its colors and lines at 60% size. No HOME action is present. The card and heading follow the board animation and reset together when a clear is interrupted. The board backing now leaves 0.35 units of horizontal padding per side, plus vertical space for the hard shadow; tile frames do not touch its outer rim. Reset arrows use a continuous rounded arc joined to a filled arrowhead.

The approved clear motion starts with a board pulse and sheen, an animated palette-colored check, staggered TMP glyphs, and two finite paper bursts. At 1.98 seconds the badge and heading move upward while the real board shrinks to 60%. The next-stage button enters at 2.82 seconds; the main sequence finishes at 3.18 seconds and enables navigation. Idle heading/badge/button/arrow motion and sparkles continue independently. `ClearTitle.mat` adds a cyan underlay to the clear heading. Restart, disable and scene unload restore glyphs, renderer property blocks and transforms.

The Stage1 blue guide starts about 0.5 seconds after the red stroke is released; this deadline includes the existing 120 ms commitment delay. Focus/pause handling and other resume delays retain their existing behavior. Android vibration verification remains deferred.

UI labels use Lilita One from the [official Google Fonts repository](https://github.com/google/fonts/tree/main/ofl/lilitaone). The font and OFL license are included in `Assets/Fonts/LilitaOne/`. The shared static TMP atlas contains the UI's ASCII glyphs; its outlined material is local to this skin.

To reapply the whole skin, stop Play mode, save open scenes, then use **Color Puzzle / Apply B Toon UI To All Screens**. To apply only clear motion, use **Color Puzzle / Apply Approved Clear Motion**. The latter validates existing stage scenes and checks that puzzle conditions are unchanged. Both retain puzzle data and progress. The historical Figma skin command still applies its historical design if explicitly invoked.

See `Assets/Documents/ToonUISkin.md` for behavior and validation notes.
