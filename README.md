# Rune Chess Match-3

Native iOS tactical roguelite for portrait play sessions. The MVP combines auto battler squad building with an active match-3 rune board during combat.

## Project Sources

- Product design: [`GDD_Rune_Chess_Match3.md`](GDD_Rune_Chess_Match3.md)
- Development checklist: [`tasks/.tasks.md`](tasks/.tasks.md)
- Architecture and stack: [`docs/architecture.md`](docs/architecture.md)
- iOS project scaffold: [`ios/README.md`](ios/README.md)

## Fixed Stack

The MVP stack is native iOS:

- Swift 6 for gameplay, UI shell, persistence, and tests.
- SpriteKit for 2D board rendering, animation, particles, and touch input.
- GameplayKit for state machines, deterministic systems, and future AI helpers.
- SwiftUI for app hosting and non-game screens.
- XCTest for deterministic game-rule coverage.

This replaces the earlier Expo/React Native prototype direction. For an iOS-first 2D game, native Swift + SpriteKit is the stronger baseline: lower runtime overhead, direct Apple framework integration, better touch/animation control, and fewer cross-platform compromises.

## Local Setup

On macOS with Xcode installed:

```sh
cd ios
xcodegen generate
open RuneChess.xcodeproj
```

If XcodeGen is unavailable, create an iOS App project in Xcode named `RuneChess`, then add the `ios/RuneChess` and `ios/RuneChessTests` folders to the project.

## Working Rules

- Keep the MVP focused on iOS portrait.
- Keep combat, economy, run progression, and match-3 rules outside SpriteKit rendering code.
- Store balance numbers in explicit Swift configs or Codable data.
- Prefer small, testable modules that map directly to the GDD systems.
