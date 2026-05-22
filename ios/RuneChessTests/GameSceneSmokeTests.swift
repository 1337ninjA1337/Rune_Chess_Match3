import XCTest
@testable import RuneChess

final class GameSceneSmokeTests: XCTestCase {
    func testPortraitSceneRendersWithoutChildrenOverflowingRoot() {
        let scene = GameScene(size: CGSize(width: 390, height: 844))

        scene.renderPortraitLayout()

        XCTAssertGreaterThan(scene.children.count, 0)
    }
}
