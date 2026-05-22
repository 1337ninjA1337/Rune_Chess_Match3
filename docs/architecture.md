# Rune Chess Match-3 Architecture

This document fixes the native iOS technical stack and architecture for the MVP. It replaces the earlier Expo/React Native direction because the project is an iOS-first game, not a general mobile app prototype.

## Product Constraints

- Target platform: iOS.
- Primary orientation: iPhone portrait.
- Session length: 8-15 minutes.
- MVP mode: 10-round PvE roguelite run.
- Core experience: auto battler preparation plus active match-3 combat.
- Runtime target: smooth 60 FPS on supported iPhones.

## Technical Stack

- App platform: native iOS.
- Language: Swift 6.
- IDE/build system: Xcode, with `ios/project.yml` as the generated project source when XcodeGen is available.
- Game rendering: SpriteKit for 2D scene graph, animation, particles, touch input, and portrait game composition.
- Game systems: GameplayKit for state machines, rule systems, deterministic randomness, pathfinding, and AI helpers where useful.
- Native shell UI: SwiftUI for app hosting, settings, summary screens, and future non-combat overlays.
- Apple integration: GameKit for achievements/leaderboards later, StoreKit only for non-pay-to-win cosmetics if monetization is added.
- Persistence: Codable save snapshots stored locally first; upgrade to CloudKit only after MVP.
- Tests: XCTest for pure game rules, state reducers, match-3 resolution, combat formulas, and smoke tests for scene layout.

## Why This Stack

The game is a 2D iOS portrait experience with a dense tactical board, match-3 input, native touch feel, and many lightweight UI panels. SpriteKit gives a direct Apple-supported game loop and rendering model without the runtime weight and licensing tradeoffs of a general-purpose cross-platform engine. Swift keeps gameplay, UI shell, persistence, and Apple platform APIs in one language.

Unity remains the better choice if the product pivots to cross-platform release, heavy 3D, large animator-driven content, or a team pipeline centered on Unity editor tooling. For the current iOS-first 2D MVP, native Swift + SpriteKit is the default.

## Runtime Boundaries

The codebase should keep these boundaries:

```text
ios/
  project.yml              XcodeGen project definition.
  RuneChess/
    RuneChessApp.swift     SwiftUI app entry point.
    GameContainerView.swift
    Game/
      GameScene.swift      SpriteKit rendering and touch surface.
      GameTheme.swift
      RuneType.swift
    Core/
      Match3/              Pure match-3 rules and board resolution.
      Combat/              Auto battler target, attack, mana, damage, and timer rules.
      Run/                 10-round run flow and win/loss routing.
      Economy/             Shop, XP, income, reroll, and buy/sell rules.
    Content/
      Heroes/
      Commanders/
      Enemies/
      Artifacts/
      Synergies/
    Persistence/
      SaveSnapshot.swift
    UI/
      Screens/
      Components/
  RuneChessTests/
    Core/
    GameSceneSmokeTests.swift
```

SpriteKit scenes may display and dispatch player input, but they should not own combat formulas, shop odds, merge rules, or match-3 resolution. Those systems belong in pure Swift core modules.

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

Use explicit commands such as `buyHero`, `placeHero`, `startCombat`, `swapRunes`, `resolveCombatTick`, `claimReward`, and `advanceRound`. Prefer value types for state and deterministic functions for rule resolution.

## Domain Modules

### Match-3

`Core/Match3` owns:

- 7x7 board generation;
- six rune types: red, blue, green, yellow, purple, white;
- neighbor swap validation;
- horizontal and vertical match detection;
- rune removal and gravity refill;
- chain reaction resolution;
- `matchPower = matchedRunesCount + comboDepth`;
- cooldown and hint timing data exposed to the scene.

### Combat

`Core/Combat` owns:

- target selection;
- attack timers;
- melee and ranged attack rules;
- mana gain from attacks, damage, and blue runes;
- automatic ability casts;
- health, shield, armor, magic resist, healing, crit, and timer outcomes.

### Run Progression

`Core/Run` owns:

- 10 MVP rounds;
- preparation, combat, reward, event, win, and loss phases;
- run health damage;
- reward routing;
- minimum saved progress.

### Economy

`Core/Economy` owns:

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

Commanders, artifacts, enemies, rounds, rune effects, and synergies should follow the same data-first pattern using Codable Swift structs or static data tables that can later move to JSON.

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

The combat scene layout is vertically stacked:

1. Tactical field.
2. Match-3 board.
3. Compact action/status panel.

## Verification Strategy

Each implementation task should include the cheapest relevant verification:

- XCTest for shared models and pure rules;
- deterministic match-3 tests for swaps, matches, gravity, and chains;
- combat formula tests before UI wiring;
- Xcode build or simulator smoke test once a macOS/Xcode environment is available;
- manual portrait layout check on small and standard iPhone viewports.

## Known Product Issue

The GDD MVP scope says "6 classes", while the detailed class list contains 7 classes. The task list already tracks this discrepancy under P0 "Фракции и классы"; do not resolve it implicitly while building unrelated systems.
