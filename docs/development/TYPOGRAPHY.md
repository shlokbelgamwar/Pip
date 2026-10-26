# Pip typography

Use one runtime family: Helvetica Neue, with Arial as a system fallback. Normal and bold are styles of the same family, not separate named font assets. This avoids an enumerated but unloadable SF Pro bold face encountered during Editor validation. No OS font files are copied into the project.

The scale below uses the existing 390-wide portrait design coordinate system; sizes and line boxes scale with the safe-area layout. This is not an implementation of iOS Dynamic Type.

| Role | Size / line box | Weight | Use |
| --- | --- | --- | --- |
| Heading | 24 / 30 | Bold | Walkthrough headline |
| Speech | 17 / 24 | Normal | Pip's replies and explanations |
| Body | 16 / 23 | Normal | Walkthrough instructions |
| Action | 16 / 20 | Bold | Main buttons, including Tell me more |
| Secondary action | 16 / 20 | Normal | Menu rows and Skip |
| Label | 13 / 18 | Bold | Pip's name and section label |
| Caption | 12 / 17 | Normal | Privacy note, contextual feedback and reading hint |

Align paragraphs left. Center primary button labels in their full touch target. Menu text aligns to a shared left edge after the icons. Keep Skip visually secondary to the main action. Avoid changing a shared style's font size temporarily for a heading.

PipTypography measures full text into explicit line boxes and renders the visible typewriter prefix within those same runs. Thus words do not jump between lines as a response is revealed. Walkthrough height follows the measured heading, paragraph and disclosure heights. Long speech scrolls instead of shrinking the font. Native font metrics can increase a line box to avoid overlap.

Implementation: Assets/Pip/Runtime/PipTypography.cs. Regression checks: Assets/Pip/Editor/PipTypographyReview.cs. Actual Unity previews: Evidence/Typography/Final-Speech.png and Final-Walkthrough.png.


## Scroll-edge fading
PipTypography.Draw accepts fadeTop, fadeBottom and fadeLength in rect coordinates. A line touching an edge keeps 35% opacity and any portion hidden past the edge lowers it further. Speech bubbles pass the visible scroll window so long replies fade at the edge that has more text. Wrapping and line boxes are unchanged.
