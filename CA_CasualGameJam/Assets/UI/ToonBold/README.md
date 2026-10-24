# B / Toon Bold UI assets

The game is **Blend Trails: Color Puzzle**. `BlendTrailsTitle.png` replaces the previous wording using the built-in image generation tool while retaining the approved red + yellow / orange composition. `ApprovedTitle.png` is preserved as the original source. The title uses only its upper artwork and lower sky regions; the source's fixed STAGE 6 button is excluded. A real button, play icon and progress-driven number are drawn separately. The exact edit prompt is in `output/ui-refresh-b-revisions/title-logo-prompt.txt`.

The title's logo and mixing motif stay red/yellow/orange regardless of progression. Gameplay uses the existing 5-stage red/blue/purple, red/yellow/orange, blue/yellow/green cycle.

The remaining assets are rounded panels, tile faces/frames, primary/reset buttons and icons authored as code-native primitives by `Assets/Editor/ToonSkinBuilder.cs`. They use full-rectangle sprites with uncompressed alpha, no mipmaps, and sliced borders for responsive panels. No screenshot is used as the interactive board.

`Assets/Resources/ToonSkin.asset` references the shared assets. `StagePaletteVisuals.asset` references the three new start markers. Every tile retains its authored coordinates, start color roles, target colors, colliders and existing state logic.

The background is a separate camera-space Canvas, sorted behind the world-space board. It has no raycaster. Buttons and other controls remain on the existing interactive Canvas, and decorative graphics do not intercept input.

The clear card has a separate camera-space backdrop below the live solved board, with its heading, palette-colored success badge and next-stage action on the existing overlay Canvas. Its button label follows the next display number: Stage4 after clearing Stage3, including Stage31 and higher when authored puzzles are reused. The completed board retains its colors and lines at 60% size. No HOME action is present. The card and heading follow the board animation and reset together when a clear is interrupted. The board backing now leaves 0.35 units of horizontal padding per side, plus vertical space for the hard shadow; tile frames do not touch its outer rim. Reset arrows use a continuous rounded arc joined to a filled arrowhead.

UI labels use Lilita One from the [official Google Fonts repository](https://github.com/google/fonts/tree/main/ofl/lilitaone). The font and OFL license are included in `Assets/Fonts/LilitaOne/`. The shared static TMP atlas contains the UI's ASCII glyphs; its outlined material is local to this skin.

To reapply the skin to the existing title and all thirty authored stages, stop Play mode, save open scenes, then use **Color Puzzle / Apply B Toon UI To All Screens**. This updates appearance and layout without regenerating puzzle conditions or resetting progress. The historical Figma skin command still applies its historical design if explicitly invoked.

See `Assets/Documents/ToonUISkin.md` for behavior and validation notes.
