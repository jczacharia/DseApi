# Offline SonarQube quality profile

This mirrors the server-side SonarQube quality profile **"Sonar way PNC"** into the
local build and Rider, with **no network access to the Sonar server**. The C# rules
SonarQube runs are Roslyn analyzers shipped in the free [`SonarAnalyzer.CSharp`](https://www.nuget.org/packages/SonarAnalyzer.CSharp)
NuGet, so the same `S####` issues surface offline as build diagnostics.

## Files

| File                                 | Role                                           | Maintained          |
| ------------------------------------ | ---------------------------------------------- | ------------------- |
| `Sonar-way-PNC.xml`                  | Profile backup exported from the server        | by hand (re-export) |
| `convert.mjs`                        | Generates the two artifacts below from the XML | by hand             |
| `sonar-quality-profile.globalconfig` | Per-rule severities (global analyzer config)   | **generated**       |
| `SonarLint.xml`                      | Parameter values for parameterized rules       | **generated**       |

Wired in the root `Directory.Build.props` (applies to every C# project):
`SonarAnalyzer.CSharp` package + `GlobalAnalyzerConfigFiles` + `AdditionalFiles`.

## Severity policy (graded, build-breaking)

| Sonar priority             | Roslyn severity | Effect (with `TreatWarningsAsErrors`) |
| -------------------------- | --------------- | ------------------------------------- |
| BLOCKER / CRITICAL / MAJOR | `warning`       | **build error**                       |
| MINOR / INFO               | `suggestion`    | IDE hint only                         |

## Regenerating after a server-side profile change

1. On a machine with VPN access: SonarQube → **Quality Profiles** → _Sonar way PNC_ →
   ⋯ → **Back up**. (Or `GET /api/qualityprofiles/backup?language=cs&qualityProfile=Sonar%20way%20PNC`.)
2. Replace `Sonar-way-PNC.xml` with the download.
3. `node eng/sonar/convert.mjs`
4. Commit the regenerated files.

## TypeScript / Angular (`Sonar-way-PNC-ts.xml`)

The C# rules map 1:1 to a NuGet; the frontend doesn't. Sonar's TS rules are reproduced offline
via [`eslint-plugin-sonarjs`](https://www.npmjs.com/package/eslint-plugin-sonarjs), already in
the `ui/` ESLint flat config.

- `convert-ts.mjs` derives the `S#### -> sonarjs/<rule-name>` map from the **installed plugin's
  own metadata** (`meta.docs.url` carries the RSPEC key), reads `Sonar-way-PNC-ts.xml`, and emits
  `sonarjs-pnc-rules.mjs` — a flat-config `rules` object imported by `eslint.config.ts`.
- Severity (graded, mirrors C#): BLOCKER/CRITICAL/MAJOR -> `error`; MINOR/INFO -> `warn`.
- Parameters: every parameterized PNC rule matches the plugin default **except `S101`**
  (PNC forbids a leading `$` in class names) — that one is overridden in `PARAM_OVERRIDES`.
- Refresh: re-export the `ts` profile over `Sonar-way-PNC-ts.xml`, then `node eng/sonar/convert-ts.mjs`.

What this pins (last run): **187 rules enforced**, **205 profile rules not in the plugin**
(mostly the newest `S77xx` Angular rules + rules SonarQube sources from other engines — they
postdate plugin v4.1), **21 server-only** (`tssecurity` taint + `tsarchitecture`). The skipped
list is printed by the converter. This replaced the generic `sonarjs.configs.recommended` preset,
so rules outside the PNC profile (e.g. `function-return-type`) are no longer enforced — exact parity.

CSS and HTML are intentionally not pinned: there is no hand-written CSS/SCSS yet (all Tailwind in
templates), and Angular HTML is covered by `angular-eslint` template + a11y rules. Sonar's CSS
engine is Stylelint-based when that day comes.

## Known gaps (server-only)

- **SonarSecurity taint analysis** (`roslyn.sonaranalyzer.security.cs`: SQL/command/path
  injection, XSS, SSRF — 27 rules in this profile) is Developer Edition+, runs server-side
  only, and is **not** in the NuGet. It cannot be reproduced offline. The rules are listed
  for the record at the bottom of `sonar-quality-profile.globalconfig`.
- **Quality _gate_** (coverage %, duplication, new-code conditions) is a server-side
  aggregate and is intentionally not represented here — this mirrors _issues_, not the gate.
- The NuGet's default-on rule set ≈ "Sonar way"; a few rules outside this profile may still
  fire. To seal exact parity, disable the complement (not done — documented trade-off).
