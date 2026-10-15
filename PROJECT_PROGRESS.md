# Project Progress - SAM_Psychrometrics (2026-Q4)

## Grasshopper component metadata standard pilot closeout - 2026-10-15

- SAM-BIM/SAM_Psychrometrics#11 merged as `86dbeb4ac3168aa8c3a99f0e045ccdcba624d4b8` (merge commit pinned to approved head `ec8fdd169fefc650156e4930c1bb276486d30251`; base `sow/2026-Q4` `4c91e532409589b7125a9b3a42ad2551aac4c1c7`; validated content head `c1a517e496aa9f2c2738cab7f6d4cb98e273fc06`). Owner-approved 2026-10-15 for merge subject to the repository-required checks and approvals.
- Change repository only; the shared `SAM` repository was not modified. Merge strategy: merge commit (repository practice), head pinned with `--match-head-commit`.
- Delivered: description-only metadata improvements and per-component patch versions (`SAMPsychrometrics.Psychrometrics` 1.0.5→1.0.6, `SAMPsychrometrics.DryBulbTemperature` 1.0.1→1.0.2, `SAMPsychrometrics.DewPointTemperature` 1.0.1→1.0.2). No component GUID, parameter `Name`/`NickName`, order, `Optional`, access, visibility, persistent default, category, subcategory or engineering algorithm changed; verified by a structured base-vs-head contract comparison that reported zero contract changes (only descriptions and `LatestComponentVersion` differ).
- New validation: source metadata contract `design/grasshopper-icons/tools/param_contract.py` + `design/grasshopper-icons/param_contract.json` (reuses the icon `inventory.py` scanner); Rhino 8 NUnit project `Grasshopper/SAM.Core.Grasshopper.Psychrometrics.Tests` (runtime parameter contract, voluntary-parameter insertability, and reopening the pre-pilot fixture `Fixtures/Psychrometrics-Legacy.ghx`).
- Validation (local, Windows, Rhino 8 `8.35.26251`): `dotnet build SAM_Psychrometrics.sln -c Release` 0 warnings/0 errors; `dotnet test Grasshopper/SAM.Core.Grasshopper.Psychrometrics.Tests` **8 passed / 0 failed** (1 `[Explicit]` fixture generator skipped); `param_contract.py --check` OK; checker regression detection proven by temporary mutations (lowercase description, renamed output, flipped `Optional`); reopened legacy definition recomputes bit-identical values (`enthalpy 38.55174137998151`, `dewPointTemperature 9.272392288187499`). Hosted `build` and `spdx` PASS at the approved head; post-merge `build` on `86dbeb4` run `38749212960` success.
- Authority for the naming decision: `docs/GH-Component-Metadata-Standard.md`. Published Grasshopper parameter `Name` values must remain unchanged unless a separately approved, runtime-tested compatibility migration is implemented: a reopened definition keeps the parameter names stored in the file, so a renamed declared name makes every `Params.IndexOfInputParam`/`IndexOfOutputParam` lookup in `SolveInstance` return -1 for existing definitions. The 23 naming-standard deviations are a documented legacy exception list with a deferred, breaking migration (not implemented).
- Cleanup: remote feature branch `feature/gh-component-metadata-standard-q4` deleted with an expected-SHA lease after confirming it is contained in `sow/2026-Q4` and no open PR uses it; the local branch is preserved. No other branch, worktree or frozen quarter was touched.
- Remaining limitations: no interactive mouse-driven Rhino session was run (acceptance was a headless Rhino 8 run via `Rhino.Testing` against the real installed runtime); no release or installer repin is included; the test project is not part of `SAM_Psychrometrics.sln` (it needs Rhino 8), as with the existing `GhIconTest`.
- Exact next step: carry the naming rule into the other repositories' rollouts as a separately approved task; the 23 identifier renames remain deferred and must not be implemented without an approved, runtime-tested compatibility migration.

## Q4 agent instructions rollout closeout - 2026-10-12

- SAM-BIM/SAM_Psychrometrics#10 merged as `d35ae5e2fdd5431b4ac0f2c43228f5fa6e7cedd3` (merge commit pinned to approved head `7fef2ad42aa3e20cc2a0d9e014978ceebfcf6595`; base `sow/2026-Q4` `21c485e942ce07ba09ca3806d0b5e1591e7c1dd3`; validated content `edb51ab36ae4bfe857a25afc0fd497b27a937695`). Owner-authorized 2026-10-12 after an independent review (`AGENT_ROLLOUT_REVIEW_2026-10-12.md`, local to the owner's workspace, not committed).
- Docs only: AGENTS.md, CLAUDE.md, plus the rollout record `documentation/Agent-Continuity-Rollout-Q4.md`. No product code, build, workflow, dependency pin or tool changed; this closeout is the only `PROJECT_PROGRESS.md` change.
- Validation: local documentation checks and commands are in `documentation/Agent-Continuity-Rollout-Q4.md` (`git diff --check`, file scope, UTF-8/LF/no BOM, import). Hosted checks at the approved head: build success (run 38258027190); spdx success (run 38258027029). Dependency selection: dependency clones in the build log: SAM @ sow/2026-Q4.
- Cleanup: remote docs/agent-instructions-rollout-q4 deleted with an expected-SHA lease on 7fef2ad42aa3e20cc2a0d9e014978ceebfcf6595 after confirming it is contained in sow/2026-Q4 and no open PR uses it as head or base; the local branch and its worktree are preserved (worktree in use). Other branches, worktrees and the frozen quarters were not touched.
- Remaining gaps: no agent-client behaviour or token benchmark has been run.
- Exact next step: Continue from the shared `AGENTS.md`. Open item carried from the pilots: verify on each installed agent client, in a fresh Q4 session, that it follows the instructions without loading the full progress history, and record the result.

## Icon migration closeout — 2026-10-11

- [SAM_Psychrometrics#9](https://github.com/SAM-BIM/SAM_Psychrometrics/pull/9) merged as `3bc39a92eadcb36d394a4ac0f18b5b3598df2f86`; owner authorized merging after green/safe validation. Feature branch preserved.
- Completed frozen SAM#179 glyph/palette/badge migration; component GUIDs, APIs, namespaces, metadata and behavior preserved. Icon resources, manifests/generation kit, token assignments and PR validation record changed; no unrelated production refactor.
- Fresh local `Release` build PASS; source guard 3 token swaps / 0 non-icon C# changes (standard SPDX headers and additive resource properties excepted); 3 resource references / 0 errors. Final-head GitHub checks passed before merge. Available regression test results are recorded in `documentation/Icon-Migration-Validation-2026-10-11.md`; hosted Rhino smoke is not claimed as a fresh pass.
- Revit-family additional builds: 2025/2027 passed where applicable. OCCT native build and native integration cases were not required for resource-only changes; skips remain documented.
- Remaining: no known blocker for this merged icon PR. SAM_Building and separate local test/environment failures elsewhere remain tracked in the organisation audit; unrelated PR9 is not merged by this task. All network operations target SAM-BIM; historical external integration/attribution text is unchanged.
- Exact next step: owner visual review of merged icons in the installed host, then continue other work from current Q4. No installer/submodule repin is included in this closeout.


## Branch

`sow/2026-Q4` - bootstrapped 2026-10-06 from `master` `e8b20498`. Frozen Q3 record: `sow/2026-Q3` @ `6b5bdb9d` (not modified).

## Last updated

2026-10-06 (Q4 icon-redesign pilot migration).

## Current status

Q4 branch cut from `master` `e8b20498`, which is the exact commit pinned in SAM_Deploy's frozen Q3 baseline (`v20261006.1`). Bootstrap added only internal docs (this file, `AGENTS.md`). No product source changed. No Q4 product work has started.

## Q4 priorities

Not yet set by the owner. Record them here at the first Q4 planning pass. Known carry-over work is listed below.

## Known carry-over work

- **SAM Grasshopper icon redesign - PR #7** (`feature/sam-gh-icon-redesign` @ `e837f0c7`, open, base `sow/2026-Q3`, not merged). Analysed 2026-10-06: the branch carries only its own 4 icon-only commits (`0cc4162`, `733e12c`, `3bec63a`, `e837f0c`) on top of Q3 commit `5addfa77`. Those commits are not reachable from `sow/2026-Q4` (Q4 is built on the promoted `master` line), so a plain retarget would list 6 commits. Replaying exactly those commits onto `sow/2026-Q4` @ `fda6c5e2` is conflict-free (verified commit-by-commit with `git merge-tree`; identical to the net-diff merge). Planned action: rebase-onto Q4 as a new branch + PR, then close this one; owner-approved controlled task, not yet executed. **Update:** pilot-migrated; replacement Q4 PR SAM_Psychrometrics#9 (see the pilot migration section); this old PR stays open for now.

## Repository-specific next steps

- Await Q4 planning. Open PRs for Q4 work against `sow/2026-Q4`.
- Follow the continuity convention in `AGENTS.md` for every PR and closeout.

## Decisions / assumptions

- Q4 base is `master` `e8b20498`; the internal files were recovered from `sow/2026-Q3` into this branch only, never onto `master`.
- Q4 history intentionally does not contain the Q3 branch history (the maintained `master` is the promoted Q3 line, which is not a descendant of `sow/2026-Q3`); the frozen `sow/2026-Q3` branch is the permanent record.
- Historical Q2/Q3 content below is kept as evidence; its branch names, SHAs and next steps describe Q3 and are not current instructions.

## Validation

- Bootstrap verified 2026-10-06: `sow/2026-Q4` was created at exactly `e8b20498` and the push was a normal (non-forced) branch creation.

## Issues / blockers

- None at bootstrap.

## Next step

- Owner to set Q4 priorities; then start the first Q4 task from this branch.

## Q4 operational cleanup (2026-10-06)

- Reviewed every active Q2/Q3 reference in this repository on `sow/2026-Q4` (workflow branch filters, dependency-branch resolution, `.gitmodules`/validation, docs). Historical Q2/Q3 mentions (feature documentation records, the frozen Q3 section below) are intentionally unchanged.
- Changed (`fda6c5e`): removed the dead `$candidates += 'sow/2026-Q2'` fallback from the dependency-branch resolution in `.github/workflows/build.yml`. No dependency repository has a `sow/2026-Q2` branch, so the entry never matched and resolution already fell through to the default branch; behaviour is unchanged (PR head ref, current sow ref, then the dependency's default branch) and no per-quarter edit is needed.
- Checked, no action: the `github.repository_owner == 'SAM-BIM'` build guard (intentional; its comment names HoareLea only to explain why the guard exists), CODEOWNERS (SAM-BIM owners), and workflow secrets (no HoareLea-named secret). The local `upstream` (HoareLea) remote is preserved.
- Carry-over: **SAM Grasshopper icon redesign - PR #7** (`feature/sam-gh-icon-redesign` @ `e837f0c7`, open, base `sow/2026-Q3`, not merged). Analysed 2026-10-06: the branch carries only its own 4 icon-only commits (`0cc4162`, `733e12c`, `3bec63a`, `e837f0c`) on top of Q3 commit `5addfa77`. Those commits are not reachable from `sow/2026-Q4` (Q4 is built on the promoted `master` line), so a plain retarget would list 6 commits. Replaying exactly those commits onto `sow/2026-Q4` @ `fda6c5e2` is conflict-free (verified commit-by-commit with `git merge-tree`; identical to the net-diff merge). Planned action: rebase-onto Q4 as a new branch + PR, then close this one; owner-approved controlled task, not yet executed.
- Full cross-repository record, migration table and owner decisions: `SAM_Deploy:sow/2026-Q4` `PROJECT_PROGRESS.md`.

## Q4 icon-redesign pilot migration (2026-10-06)

- Old PR: SAM-BIM/SAM_Psychrometrics#7 (`feature/sam-gh-icon-redesign` @ `e837f0c7`, base `sow/2026-Q3`) - **preserved, open, untouched**.
- New branch `feature/sam-gh-icon-redesign-q4` cut from `sow/2026-Q4` @ `9c771758`; new PR **SAM-BIM/SAM_Psychrometrics#9** (base `sow/2026-Q4`), feature head `4811bca7`. **Not merged.**
- Replayed (old -> new, `cherry-pick -x`): `0cc4162`->`6cacf73`, `733e12c`->`fcc5c6d`, `3bec63a`->`befc494`, `e837f0c`->`287a52b`; plus one new docs commit `4811bca` pointing the PR record at the new PR. No Q3 history imported.
- Verified before push: result tree identical to the net-diff merge of the old feature onto Q4; Q4 feature diff has the same `git patch-id`, file set, numstat and blobs (all PNGs) as the old PR's feature diff; no workflow/`.gitmodules`/`AGENTS.md`/`PROJECT_PROGRESS.md` changes. Inherited from the old commits (identical in both diffs, not fixed): `git diff --check` trailing-whitespace notes in generated `Resources.Designer.cs` and `review/REVIEW.md`.
- Validation: `tools/check_source.py origin/sow/2026-Q4` OK, `tools/check_assemblies.py` OK, local `dotnet build SAM_Psychrometrics.sln -c Debug` 0 errors; PR CI `build` and `spdx` green.
- Next: owner decides whether/when to close the old PR; merge remains the maintainer's call.

---

# Historical record - 2026-Q3 (frozen)

Source: last revision of the file on `sow/2026-Q3`, commit `5addfa7` (the file was removed from the Q3 tip by `6b5bdb9`; `sow/2026-Q3` tip is `6b5bdb9d`). The Q3 file was an unpopulated template ("Not updated yet"); no Q3 work was recorded in this repository's progress file.
