# Stage 1 finger guide

StageOneTutorial.prefab is a visual-only overlay beneath GameCanvas/SafeArea/StageContent/BoardArea in SampleScene.
StageOneTutorialGuide observes the existing puzzle and never paints, commits input, or changes clear rules.
Every Graphic has raycastTarget disabled and the CanvasGroup does not block input.

The red example covers the upper two rows; the blue example covers the lower two rows. The prefab stores tile coordinates, not fixed screen pixels. A different committed color partition suppresses conflicting guidance while normal play continues. Blue-first play is supported.

Timing and hand size are editable on the prefab. The hand RectTransform pivot identifies the visible fingertip. StageOneTutorialBuilder computes it from the source alpha when first creating the prefab. TouchHalo and DirectionArrow are neutral white procedural shapes, tinted at runtime. The dashed trail uses a fixed pool of UI Images.

Use Color Puzzle/Apply Stage 1 Tutorial to configure only Stage 1. ReferenceStageBuilder also applies this configuration after assigning each puzzle ID, removing this feature's root from any other scene cloned from Stage 1. Runtime guards independently require PuzzleId == 1.

HandPointer.png was generated with the built-in image_gen tool (transparent background). Prompt:

> A single isolated friendly white cartoon pointing hand, index finger extended straight up, other fingers folded, navy outline, white fill and a small pale blue shadow, readable at small size. One hand and short wrist on a genuinely transparent square canvas, no halo, arrows, text, grid or background.

No instructional captions are included. The review sheet's TUTORIAL, RED GUIDE and BLUE GUIDE headings are not game UI.
