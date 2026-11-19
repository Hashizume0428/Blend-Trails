# Stage 1 tutorial: specification review and image preview

Status: awaiting image review; no implementation authorized yet.
Updated: 2026-10-24 (Asia/Tokyo).

## Confirmed requirements

- Add finger guidance to Stage 1 along the existing example solution.
- Accept other routes whenever the existing stage clear conditions are satisfied.
- Do not add Japanese to the game UI.
- Remove the instructional caption below the stage title. Do not replace it with an English caption.
- Do not modify runtime code, scenes, prefabs, settings, or game assets during this review.
- Discuss the detailed implementation after the user approves the image. Retain these investigation notes so later implementation can start smoothly.

## Existing Stage 1

Actual scene: Assets/Scenes/SampleScene.unity (not Stage01.unity).
StageCatalog maps puzzle 1 to SceneNavigator.StageScene = SampleScene.
The board has five columns, four rows, twenty cells. Coordinates below use 1-based row/column.

- Red start: r1c1.
- Blue start: r4c5.
- Required red: r1c5, r2c1.
- Required blue: r4c1, r3c5.
- No purple targets or forbidden cells.

Example solution:
- Red: r1c1 -> r1c5 -> r2c5 -> r2c1 (expand straight segments through every intermediate cell).
- Blue: r4c5 -> r4c1 -> r3c1 -> r3c5.

GameManager.StageClearCheck checks each tile's final color. It does not compare paths with an example answer.
PuzzleRules.IsTileCorrect requires every tile to be painted, required colors to match exactly, and ordinary cells to be pure red or pure blue. Purple is invalid anywhere on Stage 1.
Input currently allows orthogonal strokes, immediate backtracking, and starting either color first. Finger guidance must not add route restrictions or ordering requirements.

Two manually checked route pairs cover all 20 cells, have no red/blue overlap, and meet all four required colors:
- Example red: 11 12 13 14 15 25 24 23 22 21.
- Example blue: 45 44 43 42 41 31 32 33 34 35.
- Alternative red: 11 21 22 23 24 25 15 14 13 12.
- Alternative blue: 45 35 34 33 32 31 41 42 43 44.
This was a coordinate consistency check only, not a Unity runtime test.

## Image proposal (not yet approved)

Use the approved Toon Bold / pop design recorded in output/stage-ui-concepts/SELECTED.md.
The current serialized Stage 1 still has the earlier Figma skin; the preview applies the approved future visual style to the actual Stage 1 puzzle layout. It is not a screenshot of an implemented tutorial.

- White pointing hand with navy outline and a translucent contact halo.
- Red demonstration followed by blue demonstration.
- Pale dashed guide trails and small directional arrows are additional visual proposals, not fixed requirements.
- Left preview: unpainted board showing red guidance.
- Right preview: player has drawn the example red stroke, blue guidance is visible.
- No instructional caption inside the screen. Existing English labels remain.
- Hide the hand/trail during player drawing and resolving, and stop on clear/scene exit.
- Guidance is purely visual: it must not paint cells, submit a stroke, or change completion state.

Decide after image approval:
- Whether to keep the dashed trail or show only the hand.
- Loop speed, pauses, and delay before redisplaying guidance.
- Reset / per-color reset / interrupted input behavior.
- How to stop or adapt canonical guidance when a different partial coloring is incompatible with the example completion, without rejecting that play.
- Whether to show guidance on every entry to Stage 1. Do not introduce persistence before this is decided.

## Investigation notes for later implementation

These are candidate integration points, not an implementation plan approved for execution.

- New dedicated guide component and overlay: keep demonstration drawing separate from LineController / BoardLineRenderer and the actual tile colors.
- Assets/Scenes/SampleScene.unity: the Stage 1 overlay belongs under the existing HUD / SafeArea hierarchy.
- Assets/Scripts/GameManager.cs: Changed, State, IsInitialized, GetTile, HasCommittedLine already expose the state needed to observe drawing, commits and resets. Keep StageClearCheck unchanged.
- Assets/Scripts/StageClearNavigation.cs: use PuzzleId == 1 when guarding Stage 1 behavior. StageCatalog.Kind == Tutorial includes Stages 1-4 and would be too broad.
- Assets/Scripts/InputTouchManager.cs: BeginPointer rejects ANY EventSystem hit. Every guide Image / trail / optional label must be non-raycastable; an overlay CanvasGroup must not block raycasts.
- Assets/Scripts/StageScreenLayout.cs: the board uses world-space tile transforms while HUD is screen-space overlay. Position the fingertip from actual tile transforms and the existing board camera; avoid fixed screen pixels. Account for safe areas and resizing.
- Assets/Editor/StageScreenBuilder.cs and ReferenceStageBuilder.cs: preserve the overlay when rebuilding UI; ReferenceStageBuilder can clone Stage 1 as a template for new stages, so ensure the guide cannot become active in other puzzles.
- Assets/Tests/PlayMode/StageTestReference.cs contains a test-only transcription of the example path. Runtime guidance must not depend on the test assembly or output files. Choose runtime-authored route data later.
- Assets/Documents/StageProgression.md currently says no operation guides are added. When implementation is requested, update that statement to reflect the Stage 1 exception.

Future checks, after implementation is authorized:
- Example route and alternative route both clear using the unchanged game rules.
- Blue-first input remains usable.
- Overlay never intercepts touch or mouse input.
- No guide in Stages 2-30 or extras.
- Input, reset, cancellation, clear animation, scene exit, and portrait safe-area changes do not leave a hand or trail behind.

No Unity build, Editor script, or test suite was run for this review.
Only review artifacts in this output folder are created.


## Review artifacts

Final preview: stage1-tutorial-preview-v2.png
Final edit prompt: image-prompt-v2.txt
Generated with the built-in image_gen tool; image inspected for caption removal, English-only labels, and preserved 5x4 board geometry.


## Implementation design follow-up

A concrete design has now been drafted in [implementation-plan.md](implementation-plan.md) in response to the follow-up request. It records recommended timings, state handling, file changes, alternate-solution behavior, and future verification. The preview remains a proposal; no runtime code, scene, prefab, settings, or game asset was changed.

