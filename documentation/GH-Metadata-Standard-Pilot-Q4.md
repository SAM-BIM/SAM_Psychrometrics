# Grasshopper component metadata standard - safe pilot (2026-Q4)

**Status:** implementation complete; local validation green; **not merged** (owner review required).
Open the PR, confirm the hosted `build` and `spdx` checks, then merge.

- Repository: `SAM_Psychrometrics` (change repository). The shared `SAM` repository was **not** modified.
- Branch: `feature/gh-component-metadata-standard-q4`, base `sow/2026-Q4`.
- Validated content SHA: `c1a517e496aa9f2c2738cab7f6d4cb98e273fc06` (this record is a docs-only commit on top).
- Detail and the standard: `docs/GH-Component-Metadata-Standard.md`.

## Work completed

1. Inventoried the three active Grasshopper components, their GUIDs, versions and full parameter
   metadata (preserved in `design/grasshopper-icons/param_contract.json`).
2. Improved metadata only: descriptions (typos, grammar, units, documented `101325 Pa` default,
   the humidity-input exclusivity) and the per-component patch version.
   - `SAMPsychrometrics.Psychrometrics` 1.0.5 -> 1.0.6
   - `SAMPsychrometrics.DryBulbTemperature` 1.0.1 -> 1.0.2
   - `SAMPsychrometrics.DewPointTemperature` 1.0.1 -> 1.0.2
3. Added a source-level metadata contract (`param_contract.py` + `param_contract.json`) that fails on
   any change to GUID, name, nickname, order, access, optionality, visibility or persistent default,
   and on description-quality regressions, while holding the naming-standard deviations in an explicit
   23-entry legacy-exception list.
4. Added a Rhino 8 test project asserting the runtime parameter contract and reopening a pre-pilot
   definition fixture (`Fixtures/Psychrometrics-Legacy.ghx`).

## Important decisions and assumptions

- **No parameter `Name` was changed, and none should be without the deferred migration.** Grasshopper
  restores a variable-parameter component from the parameter archive in the definition, so a reopened
  component keeps the **file's** names. After a rename, every `Params.IndexOfInputParam` /
  `IndexOfOutputParam` lookup in `SolveInstance` returns -1 for every existing definition - no compile
  error, no build failure. Measured in real Rhino 8 (see below and the docs).
- The standard's spelling for 23 identifiers (16 camelCase outputs, 7 `_name_` optional inputs without a
  persistent default) is a **breaking change** and is documented as a deferred migration, not applied.
- Descriptions are not frozen in the contract; only their quality is checked, so they can still be
  improved without regenerating the manifest.
- The patch version bump is the repository's mechanism for offering the improved metadata through
  *Update component*; it does not change input/output behaviour.
- `NickName = Name` already held for every parameter, so nothing was changed there.

## Files changed

| File | Reason |
|---|---|
| `Grasshopper/SAM.Core.Grasshopper.Psychrometrics/Component/SAMPsychrometricsPsychrometrics.cs` | descriptions + version |
| `Grasshopper/SAM.Core.Grasshopper.Psychrometrics/Component/SAMPsychrometricsDryBulbTemperature.cs` | descriptions + version |
| `Grasshopper/SAM.Core.Grasshopper.Psychrometrics/Component/SAMPsychrometricsDewPointTemperature.cs` | descriptions + version |
| `design/grasshopper-icons/tools/param_contract.py` | new source contract checker (reuses the icon `inventory.py` scanner) |
| `design/grasshopper-icons/param_contract.json` | new reviewed baseline + naming-exception registry |
| `docs/GH-Component-Metadata-Standard.md` | the standard, the evidence, the deferred migration |
| `Grasshopper/SAM.Core.Grasshopper.Psychrometrics.Tests/**` | new Rhino 8 metadata + definition-compatibility tests and their fixture |

No algorithm, GUID, category, subcategory, parameter order, access, optionality, persistent default,
workflow or dependency changed. No shared SAM serialization infrastructure changed.

## Validation

Local (Windows, Rhino 8 `8.35.26251`, .NET SDK 10, `Release`/`Debug`):

| Check | Command | Result |
|---|---|---|
| Product build | `dotnet build SAM_Psychrometrics.sln -c Release` | succeeded, 0 warnings, 0 errors |
| Metadata tests (Rhino 8) | `dotnet test Grasshopper/SAM.Core.Grasshopper.Psychrometrics.Tests` | **8 passed / 0 failed** (1 `[Explicit]` fixture generator skipped) |
| Source contract | `python design/grasshopper-icons/tools/param_contract.py --check` | `3 components checked: OK` |
| Whitespace | `git diff --check` | clean |

Regression-detection proof for the new checker (temporary source mutations, then restored):
lowercase description -> FAIL; renamed output -> FAIL (names/order + naming exceptions); flipped
`Optional` -> FAIL.

Backward-compatibility evidence (real Rhino 8, `SavedDefinitionCompatibilityTests`):

- `Fixtures/Psychrometrics-Legacy.ghx` was saved with the **pre-pilot** build (1.0.5 / 1.0.1 / 1.0.1):
  a Psychrometrics component (20 °C, 50 %rh, inserted voluntary `_pressure_`), a DewPoint component,
  and two wired number recipients.
- Reopened with the post-pilot build: structure, the voluntary parameter, its typed-in values, both
  wires and every name lookup are intact.
- Recomputed numbers are **bit-identical** before and after the change:
  `enthalpy = 38.55174137998151`, `dewPointTemperature = 9.272392288187499`.
- Rename mechanism reproduced: parameter count and type unchanged, the component keeps the file's
  name, and the declared name no longer resolves (`IndexOfInputParam(...) = -1`).

Machine-local prerequisites: a Rhino 8 install (path is in `Rhino.Testing.Configs.xml`, as in the
existing `GhIconTest`), Python 3 for the checker (any Python 3 works; validated with the Python bundled
with Rhino). Nothing else is required.

**Hosted checks:** PASS at the final head `76a94860ed7633bfe817d6e8cb872cc1ec0a25c9` - `build`
(run 38735870267, 3m14s) and `spdx` (run 38735870441).

**NOT TESTED:** no manual interactive Rhino session (mouse-driven open/recompute) was performed; the
acceptance run is headless Rhino 8 via `Rhino.Testing`, which loads the real installed runtime.

## Unresolved issues, risks and blockers

- The rename `Name`-normalisation (23 identifiers) is deliberately **not** done. Doing it safely needs
  old+new names accepted simultaneously in `SolveInstance` and in `Inputs`/`Outputs`, plus a version
  bump and owner approval - a change to shared reconciliation behaviour that this pilot's boundary
  excludes.
- The test project is not part of `SAM_Psychrometrics.sln` (it needs Rhino 8), the same as the existing
  `GhIconTest`; it must be run explicitly.
- No blocker. `SPDX`, `build` and the new checks are expected to pass; confirm on the final head.

## Exact next step

1. ~~Push the branch, open one PR into `sow/2026-Q4`, confirm the hosted checks at the final head.~~
   Done: PR #11, final head `76a94860ed7633bfe817d6e8cb872cc1ec0a25c9`, `build` and `spdx` PASS.
2. Owner review and explicit merge approval. Do **not** merge automatically and do not publish a release.
3. After merge: docs-only `[skip ci]` closeout on `sow/2026-Q4` with the merge SHA, then lease-guarded
   deletion of the merged feature branch.
