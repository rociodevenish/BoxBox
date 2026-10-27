Standards review of `54c36ddb126f166a4b03aab544d93d023c33c10b...8458f00dd8b43725395f8746811e371e0a100f72`.

No material documented-standard breach found. Reviewed repository agent/domain/tracker/verification guidance, glossary and locked architecture/concept guidance; no independent coding-standards or contributing document exists. Tooling-enforced concerns excluded.

Judgement calls under the review skill's smell baseline:

- **[P3] Possible Duplicated Code — `src/frontend/src/app/campaign/campaign-page.ts:59–72`.** Hire and Scout independently implement the same durable command workflow: `let saved=localStorage.getItem(key)`, persist a new command, POST the saved payload, check `generation===this.personnelGeneration`, clear storage on a definitive 4xx, and retain it on ambiguous failure. This is the recovery/privacy-sensitive portion of both operations; changing its policy requires synchronized edits. Extract a small shared personnel-command helper accepting the command kind and initial payload, leaving Hire's campaign refresh explicit.

- **[P3] Possible Duplicated Code — `src/backend/Game.Domain/PersonnelAcquisition.cs:35–47,171–175`.** Opening and validating a save repeat the Scout allocation bounds and the allocation formula: `Math.Min(allocation.MaximumTokens,allocation.BaseTokens+(carousel.InitialOrder.Length>1&&index>=(carousel.InitialOrder.Length+1)/2?allocation.LaterPriorityBonus:0))`. A future compensation adjustment can make newly opened campaigns fail validation unless both implementations change together. Share allocation validation and an allocated-token calculation between Open and Validate while preserving their different error messages.

Both findings are maintainability heuristics, not hard violations or demonstrated behavioural failures. No tests or services were run and no repository files were changed.
