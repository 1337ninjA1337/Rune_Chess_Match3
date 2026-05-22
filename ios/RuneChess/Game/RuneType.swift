import SpriteKit

enum RuneType: CaseIterable {
    case red
    case blue
    case green
    case yellow
    case purple
    case white

    var color: SKColor {
        switch self {
        case .red:
            return SKColor(red: 0.79, green: 0.29, blue: 0.29, alpha: 1.0)
        case .blue:
            return SKColor(red: 0.29, green: 0.49, blue: 0.82, alpha: 1.0)
        case .green:
            return SKColor(red: 0.33, green: 0.63, blue: 0.42, alpha: 1.0)
        case .yellow:
            return SKColor(red: 0.87, green: 0.75, blue: 0.31, alpha: 1.0)
        case .purple:
            return SKColor(red: 0.53, green: 0.38, blue: 0.74, alpha: 1.0)
        case .white:
            return SKColor(red: 0.91, green: 0.89, blue: 0.82, alpha: 1.0)
        }
    }
}
