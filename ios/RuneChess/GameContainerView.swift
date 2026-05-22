import SpriteKit
import SwiftUI

struct GameContainerView: View {
    @State private var scene = GameScene(size: CGSize(width: 390, height: 844))

    var body: some View {
        GeometryReader { proxy in
            SpriteView(scene: configuredScene(for: proxy.size))
                .ignoresSafeArea()
                .background(Color(GameTheme.background))
        }
    }

    private func configuredScene(for size: CGSize) -> SKScene {
        scene.scaleMode = .resizeFill
        scene.size = size
        scene.renderPortraitLayout()
        return scene
    }
}
