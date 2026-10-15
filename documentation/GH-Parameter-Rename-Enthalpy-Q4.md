# Psychrometrics `enthalpy` → `Enthalpy` rename pilot (2026-Q4)

**Status:** implementation complete; local Rhino 8 validation green; **draft PR, not merged** (owner review
required). Do not release and do not repin an installer.

- Repository: `SAM_Psychrometrics` (the one controlled rename). Shared mechanism: `SAM`.
- PR: **SAM-BIM/SAM_Psychrometrics#12** -> `sow/2026-Q4` (**draft**, not merged).
- Branch: `feature/gh-parameter-rename-migration-q4`, base `sow/2026-Q4` `c35f0f4` (closeout of PR #11).
- Validated content SHA: `b6c61a55d520f2c1a5142b247dbfc7eccfba35d0` (the PR record is a docs-only commit on top).
- Dependency: the shared `SAM` PR `feature/gh-parameter-rename-migration-q4`
  (`d32e70061ed9e97b9c42f12819bd53fe814416d5`). The two branches deliberately share a name so the
  downstream build's dependency resolution clones that `SAM` branch instead of falling back to
  `sow/2026-Q4`. **Merge `SAM` first, then this.**

## The single rename

Exactly one published parameter is renamed, in one existing component:

| Component | Side | Before | After | Version |
|---|---|---|---|---|
| `SAMPsychrometrics.Psychrometrics` | output | `enthalpy` | `Enthalpy` | 1.0.6 → 1.0.7 |

The other 22 naming-standard deviations are **not** touched - they remain the documented legacy exception
list in `docs/GH-Component-Metadata-Standard.md`. Component GUIDs, parameter order, access, optionality,
visibility, defaults, descriptions, engineering calculations and every other published name are unchanged.

## How the rename stays safe

Grasshopper restores a component's parameters from the definition, so a definition saved before the rename
keeps carrying `enthalpy`. Three things make that definition keep working and upgrade safely:

1. **SolveInstance compatibility.** The enthalpy output is read with
   `IndexOfOutputParameter("Enthalpy")`, which falls back to the declared legacy name `enthalpy`. An
   unmodified definition therefore still computes, before any update.
2. **Declared migration.** `ParameterRenames` declares `output enthalpy → Enthalpy`, so the SAM updater
   resolves the old name when restoring wires and persistent values (see the `SAM` PR).
3. **Explicit upgrade only.** `LatestComponentVersion` is bumped to `1.0.7`, which makes an older placed
   component *obsolete* and offers the *Update component* action; nothing changes when a file is opened.

## Files changed

| File | Reason |
|---|---|
| `Grasshopper/SAM.Core.Grasshopper.Psychrometrics/Component/SAMPsychrometricsPsychrometrics.cs` | the rename: declared `ParameterRenames`, output `Name`/`NickName` → `Enthalpy`, `IndexOfOutputParameter` in `SolveInstance`, version 1.0.7 |
| `design/grasshopper-icons/param_contract.json` | approved contract update: version 1.0.7, output name `Enthalpy`, the `…output.enthalpy` naming exception removed (now 22) |
| `docs/GH-Component-Metadata-Standard.md` | records the implemented migration and that 22 renames remain deferred |
| `Grasshopper/SAM.Core.Grasshopper.Psychrometrics.Tests/MetadataContractTests.cs` | runtime contract updated for `Enthalpy` / 1.0.7 |
| `Grasshopper/SAM.Core.Grasshopper.Psychrometrics.Tests/ParameterRenameMigrationTests.cs` | new: the six migration tests (below) |
| `Grasshopper/SAM.Core.Grasshopper.Psychrometrics.Tests/SAM.Core.Grasshopper.Psychrometrics.Tests.csproj` | adds the `SAM.Core` reference the updater's `Log` needs |

No algorithm, GUID, category, subcategory, parameter order, access, optionality, persistent default or
workflow changed; no other repository file was touched.

## Validation (local, Windows, Rhino 8 `8.35.26251`, `Release`)

| Check | Command | Result |
|---|---|---|
| Product build | `dotnet build SAM_Psychrometrics.sln -c Release` | succeeded, 0 warnings, 0 errors |
| Metadata + definition + migration tests (real headless Rhino 8) | `dotnet test Grasshopper/SAM.Core.Grasshopper.Psychrometrics.Tests` | **14 passed / 0 failed** (1 `[Explicit]` fixture generator skipped) |
| Whitespace | `git diff --check` | clean |

New `ParameterRenameMigrationTests` (real Rhino 8, the committed pre-pilot fixture
`Fixtures/Psychrometrics-Legacy.ghx`, component version 1.0.5):

1. the component declares exactly one rename (`output enthalpy → Enthalpy`);
2. the **unmodified** legacy definition still computes before any update (the declared lookup falls back);
3. the explicit updater detects the older version and migrates: the output becomes `Enthalpy`, the
   `enthalpy` wire is restored onto it, `_dryBulbTemperature` 20 / `_relativeHumidity_` 50 / `_pressure_`
   101325 are all preserved, the component GUID is unchanged, the replacement is the live document object,
   and it recomputes the same number;
4. the upgraded definition saves, reopens with `Enthalpy` (12 outputs), keeps the wire and the group, and
   recomputes identically;
5. a second update is a no-op (idempotent);
6. an **undeclared** legacy name is reported as dropped, not silently rerouted to another output.

The group is added to a copy of the legacy definition, saved and reopened, so membership is exercised
through serialization exactly as a grouped definition would be; after the update the group holds the
replacement's `InstanceGuid` and not the old one.

**Hosted checks (GitHub), at the content head `b6c61a55d520f2c1a5142b247dbfc7eccfba35d0`** (PR
SAM_Psychrometrics#12): build **pass** (run `38755069972`, 2m44s), spdx **pass** (run `38755070009`). The
build log confirms the dependency resolution cloned `SAM-BIM/SAM @ feature/gh-parameter-rename-migration-q4`
— the intended merged dependency, not the `sow/2026-Q4` fallback ("Cloning SAM-BIM/SAM @
feature/gh-parameter-rename-migration-q4"). A docs-only record commit follows.

**NOT TESTED / known gap:** `python design/grasshopper-icons/tools/param_contract.py --check` was **not**
run - no Python is available on this machine (the workflow note says the Rhino-bundled Python; RhinoCode
here exposes no standalone interpreter). `param_contract.json` was updated by hand for exactly the approved
change (version, one output name, one exception removed) and the runtime `MetadataContractTests` pin the
same values (1.0.7 / `Enthalpy`), but the source-vs-manifest checker should be run by the owner before
merge. No interactive mouse-driven Rhino session was run.

## Unresolved issues, risks and blockers

- The hosted `build` check for this repository clones `SAM` for its dependency. It will select
  `feature/gh-parameter-rename-migration-q4` from `SAM-BIM/SAM` because the two branches share a name;
  if that branch is ever renamed or deleted before this merges, the build resolves `SAM@sow/2026-Q4`, which
  does not contain the new API, and this build fails. The `SAM` PR must not be cleaned up before this one
  merges.
- Renaming a published parameter is breaking for any consumer that looks the old name up outside the SAM
  updater (for example a C# script component in a definition). The `SolveInstance` fallback only covers
  this component's own lookups.
- The other 22 renames remain deferred and each needs its own review.

## Exact next step

1. Push this branch and open one **draft** PR into `sow/2026-Q4`, cross-referencing the `SAM` PR.
2. Confirm the hosted `build` log shows the dependency clone selected the `SAM`
   `feature/gh-parameter-rename-migration-q4` branch.
3. Owner review and, once both PRs pass, merge `SAM` first, then this; no release and no installer repin.
