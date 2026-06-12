# Rune Chess Match-3

[![Core Smoke](https://github.com/1337ninjA1337/Rune_Chess_Match3/actions/workflows/core-smoke.yml/badge.svg)](https://github.com/1337ninjA1337/Rune_Chess_Match3/actions/workflows/core-smoke.yml)

Windows-developed Unity tactical roguelite targeting iOS portrait play sessions. The MVP combines auto battler squad building with an active match-3 rune board during combat.

## Project Sources

- Product design: [`GDD_Rune_Chess_Match3.md`](GDD_Rune_Chess_Match3.md)
- Development checklist: [`tasks/.tasks.md`](tasks/.tasks.md)
- Architecture and stack: [`docs/architecture.md`](docs/architecture.md)
- Pure C# core package: [`Packages/com.runechess.core`](Packages/com.runechess.core)

## Fixed Stack

The MVP stack is now Windows-first Unity:

- Unity 6.3 LTS for Windows editor development.
- C# for gameplay, presentation, and tests.
- Unity 2D + uGUI for the first portrait game surface.
- Pure C# core logic in a local Unity package with no `UnityEngine` dependency.
- .NET smoke checks for domain logic that can run on Windows without Unity.

Final iOS signing/building still needs Apple's iOS toolchain somewhere. We will use a cloud macOS builder such as Unity Build Automation or Codemagic when the project is ready for iPhone/App Store validation.

## Local Setup On Windows

Install Unity Hub, then install the latest Unity 6.3 LTS editor available in Hub. Open this repository as a Unity project.

Inside Unity:

1. Open the menu `Rune Chess > Create Main Scene`.
2. Press Play.
3. Use the Game view in portrait resolution, for example `390x844`.

For pure core smoke checks without Unity (requires the .NET 8 SDK):

```sh
./scripts/run-core-smoke.sh
```

This is the same check CI runs on every push and pull request via the
[`Core Smoke`](.github/workflows/core-smoke.yml) workflow.

## Working Rules

- Keep the MVP focused on iOS portrait.
- Keep combat, economy, run progression, and match-3 rules outside MonoBehaviours.
- Store balance numbers in explicit C# configs or data assets.
- Prefer small, testable modules that map directly to the GDD systems.
