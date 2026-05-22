# Rune Chess iOS Project

This folder contains the native iOS game scaffold.

## Stack

- Swift 6 for game code.
- SpriteKit for the 2D game scene, animation, particles, and touch input.
- GameplayKit for state machines, deterministic rules, and AI utilities as the game core grows.
- SwiftUI for native app hosting and non-game shell screens.
- XCTest for core rule tests and scene smoke checks.

## Open In Xcode

The repository stores `project.yml` so the Xcode project can be regenerated instead of hand-editing `.pbxproj` files.

```sh
cd ios
xcodegen generate
open RuneChess.xcodeproj
```

If XcodeGen is not installed, create an iOS App project in Xcode named `RuneChess`, then add the `RuneChess` and `RuneChessTests` folders to the project.

## Portrait Contract

The app is locked to iPhone portrait through `Info.plist`. The first scene renders the GDD combat structure directly: tactical field, 7x7 rune board, and compact action/status panel.
