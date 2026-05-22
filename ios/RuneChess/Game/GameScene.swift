import SpriteKit

final class GameScene: SKScene {
    private let contentNode = SKNode()
    private let tacticalColumns = 6
    private let tacticalRows = 4
    private let runeColumns = 7
    private let runeRows = 7
    private var lastRenderedSize = CGSize.zero

    private let runePattern: [RuneType] = [
        .red, .blue, .green, .yellow, .purple, .white, .red,
        .green, .yellow, .blue, .purple, .red, .white, .green,
        .blue, .purple, .white, .green, .yellow, .blue, .red,
        .yellow, .green, .red, .white, .blue, .purple, .yellow,
        .purple, .white, .blue, .red, .green, .yellow, .white,
        .red, .blue, .purple, .yellow, .white, .green, .red,
        .white, .yellow, .green, .purple, .blue, .red, .green
    ]

    override init(size: CGSize) {
        super.init(size: size)
        backgroundColor = GameTheme.background
        addChild(contentNode)
    }

    required init?(coder: NSCoder) {
        super.init(coder: coder)
        backgroundColor = GameTheme.background
        addChild(contentNode)
    }

    override func didMove(to view: SKView) {
        view.ignoresSiblingOrder = true
        renderPortraitLayout()
    }

    override func didChangeSize(_ oldSize: CGSize) {
        renderPortraitLayout()
    }

    func renderPortraitLayout() {
        guard size.width > 0, size.height > 0, size != lastRenderedSize else {
            return
        }

        lastRenderedSize = size
        contentNode.removeAllChildren()

        let contentWidth = min(size.width - 32, 430)
        let left = (size.width - contentWidth) / 2
        var cursorY = size.height - 54

        addHeader(left: left, top: cursorY, width: contentWidth)
        cursorY -= 66
        addSectionTitle("TACTICAL FIELD", meta: "6x4", left: left, top: cursorY, width: contentWidth)
        cursorY -= 26
        cursorY = addTacticalGrid(left: left, top: cursorY, width: contentWidth) - 18

        addSectionTitle("RUNE BOARD", meta: "7x7", left: left, top: cursorY, width: contentWidth)
        cursorY -= 26
        cursorY = addRuneBoard(left: left, top: cursorY, width: contentWidth) - 14

        addActionPanel(left: left, top: cursorY, width: contentWidth)
    }

    private func addHeader(left: CGFloat, top: CGFloat, width: CGFloat) {
        addLabel("ROUND 2-1", x: left, y: top, size: 11, color: GameTheme.gold, alignment: .left)
        addLabel("Rune Chess", x: left, y: top - 22, size: 21, color: GameTheme.text, alignment: .left)

        let stats = [("HP", "92", GameTheme.health), ("G", "12", GameTheme.gold), ("LV", "4", GameTheme.mana)]
        for (index, stat) in stats.enumerated() {
            let boxWidth: CGFloat = 42
            let x = left + width - CGFloat(stats.count - index) * (boxWidth + 6) + 6
            let rect = CGRect(x: x, y: top - 38, width: boxWidth, height: 40)
            addRoundedRect(rect, fill: GameTheme.panel, stroke: GameTheme.border, radius: 8)
            addLabel(stat.1, x: rect.midX, y: rect.midY + 4, size: 16, color: stat.2, alignment: .center)
            addLabel(stat.0, x: rect.midX, y: rect.midY - 13, size: 9, color: GameTheme.muted, alignment: .center)
        }
    }

    private func addSectionTitle(_ title: String, meta: String, left: CGFloat, top: CGFloat, width: CGFloat) {
        addLabel(title, x: left, y: top, size: 13, color: GameTheme.text, alignment: .left)
        addLabel(meta, x: left + width, y: top, size: 11, color: GameTheme.muted, alignment: .right)
    }

    private func addTacticalGrid(left: CGFloat, top: CGFloat, width: CGFloat) -> CGFloat {
        let gap: CGFloat = 4
        let cell = (width - gap * CGFloat(tacticalColumns - 1)) / CGFloat(tacticalColumns)
        let units: [String: (label: String, ally: Bool, hp: CGFloat, mana: CGFloat?)] = [
            "0:1": ("V", false, 0.72, nil),
            "0:4": ("M", false, 0.64, nil),
            "1:2": ("G", false, 0.88, nil),
            "2:1": ("A", true, 0.81, 0.46),
            "2:4": ("S", true, 1.00, 0.28),
            "3:2": ("H", true, 0.69, 0.62)
        ]

        for row in 0..<tacticalRows {
            for column in 0..<tacticalColumns {
                let x = left + CGFloat(column) * (cell + gap)
                let y = top - CGFloat(row + 1) * cell - CGFloat(row) * gap
                let cellRect = CGRect(x: x, y: y, width: cell, height: cell)
                let isEnemySide = row < tacticalRows / 2
                addRoundedRect(
                    cellRect,
                    fill: isEnemySide ? GameTheme.enemyCell : GameTheme.playerCell,
                    stroke: GameTheme.border,
                    radius: 4
                )

                if let unit = units["\(row):\(column)"] {
                    addUnit(unit, in: cellRect)
                }
            }
        }

        return top - CGFloat(tacticalRows) * cell - CGFloat(tacticalRows - 1) * gap
    }

    private func addRuneBoard(left: CGFloat, top: CGFloat, width: CGFloat) -> CGFloat {
        let gap: CGFloat = 4
        let cell = (width - gap * CGFloat(runeColumns - 1)) / CGFloat(runeColumns)

        for row in 0..<runeRows {
            for column in 0..<runeColumns {
                let index = row * runeColumns + column
                let rune = runePattern[index]
                let x = left + CGFloat(column) * (cell + gap)
                let y = top - CGFloat(row + 1) * cell - CGFloat(row) * gap
                let rect = CGRect(x: x, y: y, width: cell, height: cell)
                addRoundedRect(rect, fill: rune.color, stroke: SKColor.white.withAlphaComponent(0.20), radius: 8)

                let core = SKShapeNode(circleOfRadius: cell * 0.18)
                core.fillColor = SKColor.white.withAlphaComponent(0.35)
                core.strokeColor = .clear
                core.position = CGPoint(x: rect.midX, y: rect.midY)
                contentNode.addChild(core)
            }
        }

        return top - CGFloat(runeRows) * cell - CGFloat(runeRows - 1) * gap
    }

    private func addActionPanel(left: CGFloat, top: CGFloat, width: CGFloat) {
        let panelHeight: CGFloat = 84
        let panelRect = CGRect(x: left, y: top - panelHeight, width: width, height: panelHeight)
        addRoundedRect(panelRect, fill: GameTheme.panel, stroke: GameTheme.border, radius: 8)

        let benchNames = ["Iron Guard", "Oath Archer", "Field Medic"]
        let gap: CGFloat = 6
        let benchWidth = (width - 24 - gap * CGFloat(benchNames.count - 1)) / CGFloat(benchNames.count)
        for (index, name) in benchNames.enumerated() {
            let rect = CGRect(
                x: left + 12 + CGFloat(index) * (benchWidth + gap),
                y: top - 34,
                width: benchWidth,
                height: 24
            )
            addRoundedRect(rect, fill: GameTheme.panelRaised, stroke: GameTheme.border, radius: 4)
            addLabel(name, x: rect.midX, y: rect.midY - 4, size: 10, color: GameTheme.text, alignment: .center)
        }

        let actions = ["Shop", "Reroll", "XP", "Fight"]
        let actionWidth = (width - 24 - gap * CGFloat(actions.count - 1)) / CGFloat(actions.count)
        for (index, action) in actions.enumerated() {
            let rect = CGRect(
                x: left + 12 + CGFloat(index) * (actionWidth + gap),
                y: top - 72,
                width: actionWidth,
                height: 30
            )
            let isPrimary = action == "Fight"
            addRoundedRect(
                rect,
                fill: isPrimary ? GameTheme.gold : GameTheme.panelRaised,
                stroke: isPrimary ? .clear : GameTheme.border,
                radius: 4
            )
            addLabel(
                action,
                x: rect.midX,
                y: rect.midY - 5,
                size: 11,
                color: isPrimary ? GameTheme.background : GameTheme.text,
                alignment: .center
            )
        }
    }

    private func addUnit(
        _ unit: (label: String, ally: Bool, hp: CGFloat, mana: CGFloat?),
        in rect: CGRect
    ) {
        let unitRect = rect.insetBy(dx: rect.width * 0.12, dy: rect.height * 0.12)
        let fill = unit.ally
            ? SKColor(red: 0.18, green: 0.44, blue: 0.36, alpha: 1.0)
            : SKColor(red: 0.50, green: 0.23, blue: 0.29, alpha: 1.0)

        addRoundedRect(unitRect, fill: fill, stroke: .clear, radius: 4)
        addLabel(unit.label, x: unitRect.midX, y: unitRect.midY - 1, size: 15, color: GameTheme.text, alignment: .center)
        addBar(
            CGRect(x: unitRect.minX + 4, y: unitRect.minY + 6, width: unitRect.width - 8, height: 3),
            progress: unit.hp,
            fill: GameTheme.health
        )

        if let mana = unit.mana {
            addBar(
                CGRect(x: unitRect.minX + 4, y: unitRect.minY + 2, width: unitRect.width - 8, height: 3),
                progress: mana,
                fill: GameTheme.mana
            )
        }
    }

    private func addBar(_ rect: CGRect, progress: CGFloat, fill: SKColor) {
        addRoundedRect(rect, fill: SKColor.black.withAlphaComponent(0.35), stroke: .clear, radius: 1)
        let clampedProgress = min(max(progress, 0), 1)
        let fillRect = CGRect(x: rect.minX, y: rect.minY, width: rect.width * clampedProgress, height: rect.height)
        addRoundedRect(fillRect, fill: fill, stroke: .clear, radius: 1)
    }

    private func addRoundedRect(_ rect: CGRect, fill: SKColor, stroke: SKColor, radius: CGFloat) {
        let node = SKShapeNode(rect: rect, cornerRadius: radius)
        node.fillColor = fill
        node.strokeColor = stroke
        node.lineWidth = stroke == .clear ? 0 : 1
        contentNode.addChild(node)
    }

    private func addLabel(
        _ text: String,
        x: CGFloat,
        y: CGFloat,
        size: CGFloat,
        color: SKColor,
        alignment: SKLabelHorizontalAlignmentMode
    ) {
        let label = SKLabelNode(fontNamed: "AvenirNext-DemiBold")
        label.text = text
        label.fontSize = size
        label.fontColor = color
        label.horizontalAlignmentMode = alignment
        label.verticalAlignmentMode = .baseline
        label.position = CGPoint(x: x, y: y)
        contentNode.addChild(label)
    }
}
