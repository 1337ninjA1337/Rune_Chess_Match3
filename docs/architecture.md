# Rune Chess Match-3 Architecture

This document fixes the Windows-first game development stack for the MVP. The target platform remains iOS, but day-to-day development must work on Windows without macOS or Xcode.

## Product Constraints

- Target platform: iOS first, with Windows editor development.
- Primary orientation: iPhone portrait.
- Session length: 8-15 minutes.
- MVP mode: 10-round PvE roguelite run.
- Core experience: auto battler preparation plus active match-3 combat.
- Runtime target: smooth 60 FPS on supported iPhones.

## Technical Stack

- Game engine: Unity 6.3 LTS.
- Language: C#.
- Editor platform: Windows via Unity Hub.
- Rendering/UI: Unity 2D + uGUI for the MVP; upgrade to UIToolkit only if the UI starts needing stronger tooling.
- Core game rules: pure C# local Unity package at `Packages/com.runechess.core`.
- Windows verification: .NET smoke checks in `tools/CoreSmoke` for pure core logic.
- Unity verification: Unity Test Framework for edit-mode and play-mode tests once Unity is installed.
- iOS build path: Unity Build Automation, Codemagic, or another macOS cloud builder for signed iOS builds. Apple still requires the iOS toolchain for final device/App Store builds; the Mac can be cloud-hosted instead of owned locally.

## Why This Stack

Native Swift/SpriteKit is technically excellent for iOS, but it assumes macOS and Xcode. This project needs to be developed on Windows, so Unity is the practical optimum:

- full editor support on Windows;
- strong 2D/mobile workflow;
- C# domain logic that can be tested outside the Unity editor;
- iOS export/build support through cloud macOS builders;
- enough rendering and animation headroom for a match-3 + auto battler game.

Godot is viable for Windows development, but its official iOS export path still requires macOS/Xcode. Expo/EAS can build iOS from Windows, but it is an app stack first and a weaker fit for a game with board rendering, animation, timing, and future effects.

## Runtime Boundaries

The codebase should keep these boundaries:

```text
Assets/
  Editor/                         Unity editor utilities.
  Scenes/                         Generated Unity scenes.
  Scripts/
    Presentation/                 MonoBehaviours, uGUI, input, and visual composition.
Packages/
  com.runechess.core/
    Runtime/                      Pure C# domain logic with no UnityEngine dependency.
      Match3Board.cs
      RunState.cs
      HeroDefinition.cs
ProjectSettings/
  ProjectVersion.txt
tools/
  CoreSmoke/                      Windows-runnable .NET smoke verifier.
```

Unity MonoBehaviours may render and dispatch player input, but they should not own combat formulas, shop odds, merge rules, or match-3 resolution. Those systems belong in `Packages/com.runechess.core`.

## Core State

The central run state should include:

- current round number and phase;
- run health;
- gold, XP, and player level;
- selected commander;
- squad on tactical board;
- bench;
- shop;
- artifacts;
- active synergies;
- next enemy preview;
- combat timer and battle result;
- match-3 board state during combat.

Use explicit commands such as `NewRun(commanderId)`, `BuyHero`, `PlaceHero`, `StartCombat`, `SwapRunes`, `ResolveCombatTick`, `ClaimReward`, and `AdvanceRound`. Prefer value-style state transitions for deterministic systems.

## Domain Modules

### Match-3

`Packages/com.runechess.core/Runtime` owns:

- 7x7 board generation;
- six rune types: red, blue, green, yellow, purple, white;
- neighbor swap validation;
- horizontal and vertical match detection;
- rune removal and gravity refill;
- chain reaction resolution;
- `matchPower = matchedRunesCount + comboDepth`;
- cooldown and hint timing data exposed to Unity presentation.

### Combat

The combat module owns:

- target selection;
- attack timers;
- melee and ranged attack rules;
- mana gain from attacks, damage, and blue runes;
- automatic ability casts;
- health, shield, armor, magic resist, healing, crit, and timer outcomes.

### Run Progression

The run module owns:

- 10 MVP rounds;
- preparation, combat, reward, event, win, and loss phases;
- run health damage;
- reward routing;
- minimum saved progress.

### Economy

The economy module owns:

- starting gold, level, health, and bench size;
- shop size by level;
- buying, selling, reroll, and XP purchase rules;
- income, streak, interest, and event modifiers;
- shop rarity odds.

## Content Model

Heroes must be data-driven and include the required GDD fields:

- `id`
- `name`
- `rarity`
- `cost`
- `faction`
- `class`
- `runeAffinity`
- `role`
- `attackType`
- `targeting`
- `stars`
- `ability`
- `passive`

Commanders, artifacts, enemies, rounds, rune effects, and synergies should follow the same data-first pattern using C# records first, then ScriptableObjects or JSON when content grows.

## UI Architecture

The first implemented screen should be a useful game surface, not a marketing page. For the MVP, portrait screens are:

- main screen with start run and commander access;
- commander selection;
- preparation screen with tactical field, bench, shop, economy, synergies, and enemy preview;
- combat screen with tactical field, health/mana indicators, 7x7 rune board, timer, and pause;
- reward screen;
- event screen;
- hero details;
- run summary.

The combat/prep surface is vertically stacked:

1. Tactical field.
2. Match-3 board.
3. Compact action/status panel.

## Verification Strategy

Each implementation task should include the cheapest relevant verification:

- `dotnet run --project tools/CoreSmoke/CoreSmoke.csproj` for pure C# domain smoke checks;
- Unity edit-mode tests for domain modules once Unity is installed;
- Unity play-mode smoke test for generated scene;
- manual portrait layout check in the Unity Game view;
- cloud iOS build only when we need device/App Store validation.

## Resolved Product Decision: class count

The GDD MVP scope summary says "6 classes", while the detailed class list enumerates 7 (Защитник, Убийца, Маг, Стрелок, Целитель, Призыватель, Берсерк). Per the codex rule that the GDD is the source of truth, the explicit detailed list wins over the summary count, so the MVP ships **7 classes**. This is encoded in `ClassCatalog` (7 entries). Only Защитник, Маг and Убийца carry GDD-defined synergy breakpoints today; the other four are real classes with no class synergy yet. The "6 classes" line in the GDD summary should be corrected to "7 classes" on the next GDD edit pass.

## Synergy data layer

Faction and class synergies live as pure data in `FactionCatalog`, `ClassCatalog` and the shared `SynergyDefinition`/`SynergyTier` records. `SynergyCalculator` counts distinct heroes per faction/class (a hero counts once regardless of star level, like standard auto-battler traits) and reports active tiers plus the next breakpoint. Applying the individual synergy effects to combat is tracked separately under P0 "Фракции и классы".
