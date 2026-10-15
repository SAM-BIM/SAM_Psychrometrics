# SAM-BIM Grasshopper component metadata standard

Pilot implementation in `SAM_Psychrometrics` (2026-Q4). This document records the standard, what the
pilot changed, and - most importantly - why the parameter **names** were deliberately left alone.

Scope: the three active Grasshopper components of this repository. Nothing outside `SAM_Psychrometrics`
was touched. Shared SAM serialization/inspection infrastructure was not modified.

## 1. The standard

**Inputs**

| Pattern   | Meaning                                   |
|-----------|-------------------------------------------|
| `_name`   | mandatory input                           |
| `_name_`  | optional input with a documented default  |
| `name_`   | optional input without a defined default  |

**Outputs**

- `PascalCase`, e.g. `Temperature`, `RelativeHumidity`, `Diagnostics`.
- No leading or trailing underscores.
- Units and engineering detail belong in the description, not the name.

**Descriptions**

- Begin with a capital letter, be grammatically and technically correct.
- Document units, defaults and significant limitations.
- No placeholders or vague text.

**Nicknames** - `NickName = Name` for new interfaces. An intentional existing nickname is never
overwritten blindly.

## 2. Why the names in this repository are frozen

Grasshopper stores a component's parameters inside the definition itself. `GH_DocumentIO` writes a
`ParameterData` chunk that holds, per parameter, its own `InstanceGuid`, `Name`, `NickName`,
`Description`, `Optional`, `Access` and persistent data; connections reference the param
`InstanceGuid`, not its index.

When such a definition is reopened, **the parameter names in the file win over the names the component
class declares.** This was measured in a real Rhino 8 / Grasshopper run (test
`SavedDefinitionCompatibilityTests.AChangedDeclaredNameNoLongerResolvesInAnExistingDefinition`).
Consequences:

- Reopening a definition saved before a rename leaves the parameter carrying the **old** name.
- Every `Params.IndexOfInputParam("<new name>")` / `IndexOfOutputParam(...)` in `SolveInstance` then
  returns `-1`, so the component reports *Invalid data* and produces nothing - with no compile error
  and no failed build to warn anyone.
- The SAM updater (`Modify.DuplicateComponent`) recovers a parameter set by name as well, so a rename
  also drops wires and typed-in values for anyone who runs *Update component*.

Renaming a published parameter is therefore a **breaking change**. The pilot preserves every existing
`Name` and `NickName`, and the naming-standard deviations are held in an explicit, reviewed exception
list (`design/grasshopper-icons/param_contract.json`, key `namingExceptions`) instead of being applied.

### Measured evidence

A definition saved by the pre-pilot build was reopened by the post-pilot build, and a parameter in the
saved definition was renamed to stand in for the rename scenario:

```
reopened parameter count      3             (unchanged - no duplicate parameters)
reopened parameter type       Param_Number  (unchanged)
reopened parameter name       the NAME FROM THE FILE
IndexOfInputParam(declared)   -1            <-- the declared name no longer resolves
IndexOfInputParam(file)        0
wire to the component         preserved     (resolves by param InstanceGuid)
```

## 3. What the pilot changed

Only metadata: descriptions (typos, grammar, units, documented defaults) and the per-component patch
version. No `Name`, `NickName`, parameter order, access, optionality, persistent default, GUID,
category or subcategory was changed, and no numerical algorithm was touched.

| Component | GUID | Version before | Version after |
|---|---|---|---|
| `SAMPsychrometrics.Psychrometrics` | `064dc9f2-a368-459a-9e97-b60588a269f1` | 1.0.5 | 1.0.6 |
| `SAMPsychrometrics.DryBulbTemperature` | `7a795583-cabe-452e-864f-bce79135fd5c` | 1.0.1 | 1.0.2 |
| `SAMPsychrometrics.DewPointTemperature` | `e9bf471d-4ca5-4847-b5b9-bb3fbf712e41` | 1.0.1 | 1.0.2 |

Representative description fixes (the full before/after is in the PR):

| Parameter | Before | After |
|---|---|---|
| Psychrometrics `humidityRatio` output | `Humidty Ratio [g/kg]` | `Humidity ratio [g/kg]` |
| Psychrometrics `density` output | `Density [kg/m3]` | `Density [kg/m³]` |
| Psychrometrics `degreeSaturation` output | `Degree of saturation [unitless]` | `Degree of saturation [-]` |
| Psychrometrics `_dryBulbTemperature` input | `Dry bulb temperature [°C]` | `Dry-bulb temperature [°C]` |
| Psychrometrics `_pressure_` input | `Atmospheric pressure [Pa]` | `Atmospheric pressure [Pa]. Default: 101325` |
| Psychrometrics humidity inputs | *lists only three humidity indications* | lists all four (`relativeHumidity`, `humidityRatio`, `wetBulbTemperature`, `dewPointTemperature`), matching the exclusivity enforced in `SolveInstance` |
| DryBulb `_humidityRatio_` input | `Humidty Ratio [kg/kg]` | `Humidity ratio [kg/kg]` + the exclusivity note |
| DryBulb `dryBulbTemperature` output | `Dry Bulb Temperature [°C]` | `Dry-bulb temperature [°C]` |
| DewPoint `_pressure_` input | `optional Atmospheric Pressure [Pa]` | `Atmospheric pressure [Pa]. Default: 101325` |
| DewPoint component description | `Calculates DewPointTemperature by Relative Humidity(0 - 100)[%] ... \|101325 Pa` | a correct sentence naming inputs, units and default |

Descriptions are stored per definition, so improving them cannot change how an existing definition
computes. A placed component picks up the new text when it is placed anew or refreshed through
*Update component* (the patch version bump makes that offer visible).

## 4. The metadata contract and how it is enforced

Two independent, lightweight mechanisms, each reusing infrastructure that already exists in the
repository:

1. **Source contract** - `design/grasshopper-icons/tools/param_contract.py`, built on the same scanner
   as the vendored icon `inventory.py`. It extracts every component's name, version, GUID, and each
   parameter's name, nickname, description, optional / access / visibility and persistent default into
   `design/grasshopper-icons/param_contract.json`.

   ```
   python design/grasshopper-icons/tools/param_contract.py           # rewrite the manifest from source
   python design/grasshopper-icons/tools/param_contract.py --check   # verify source against the manifest
   ```

   `--check` fails on any change to GUID, name, nickname, order, access, optionality, visibility or
   persistent default, on a new/removed component, and on description-quality problems (empty,
   not starting with a capital, known typos, `NickName != Name`, unexpected access). It also keeps the
   naming-exception list honest: a **new** standard deviation fails, and a listed exception that is no
   longer a deviation also fails. Descriptions are deliberately *not* frozen, so improving one does not
   require regenerating the manifest.

2. **Runtime contract** - the NUnit project `Grasshopper/SAM.Core.Grasshopper.Psychrometrics.Tests`,
   hosted in a real headless Rhino 8 + Grasshopper (`Rhino.Testing`, the same setup as
   `SAM.Core.Grasshopper.Tests` and the repository's `GhIconTest`). It asserts the GUIDs, versions,
   names, order, access and optionality as they actually exist at runtime, the insertability of the
   voluntary parameters, and reopens the committed pre-pilot definition
   `Fixtures/Psychrometrics-Legacy.ghx`, checking that its wires and typed-in values survive and that it
   recomputes the same numbers.

   ```
   dotnet test Grasshopper/SAM.Core.Grasshopper.Psychrometrics.Tests
   ```

Both run without the shared SAM repository being modified.

## 5. Documented legacy exceptions (deferred migration)

The 23 identifiers in `param_contract.json → namingExceptions` do not follow section 1 and are expected
to stay as they are:

- the **16 outputs** are `camelCase` where the standard asks for `PascalCase`, and
- the **7 optional inputs with no persistent default** (`_relativeHumidity_`, `_humidityRatio_`,
  `_wetBulbTemperature_`, `_dewPointTemperature_` in Psychrometrics; `_humidityRatio_`,
  `_relativeHumidity_` in DryBulb; `_pressure_` in DewPoint) use `_name_` where `name_` would be the
  standard spelling.

Applying any of them requires the breaking migration below, and is out of scope for this pilot.

### Deferred migration (not implemented)

A safe rename needs the old and new names to coexist, because a reopened component keeps the file's
names:

1. Accept both names in `SolveInstance` (look up the new name, fall back to the old), and
2. keep both declared in `Inputs`/`Outputs` so `CreateParameter`/`RestoreConnections` can still resolve
   the old name, with the retired one hidden, plus
3. a version bump and an owner-approved migration note.

That changes shared behaviour and must be planned with the SAM serialization/reconciliation helpers in
view - the boundary this pilot was told not to cross.

## 6. Proof this can be re-run

```
python design/grasshopper-icons/tools/param_contract.py --check
dotnet test Grasshopper/SAM.Core.Grasshopper.Psychrometrics.Tests
dotnet build SAM_Psychrometrics.sln -c Release
```
