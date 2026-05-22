using UnityEngine;

namespace RuneChess.Presentation;

public static class GameColors
{
    public static readonly Color Background = ColorFromHex(0x171819);
    public static readonly Color Panel = ColorFromHex(0x222326);
    public static readonly Color PanelRaised = ColorFromHex(0x2B2C30);
    public static readonly Color Border = ColorFromHex(0x4A4C52);
    public static readonly Color Text = ColorFromHex(0xF5F0E6);
    public static readonly Color Muted = ColorFromHex(0xB8B0A3);
    public static readonly Color EnemyCell = ColorFromHex(0x342638);
    public static readonly Color PlayerCell = ColorFromHex(0x22352E);
    public static readonly Color Gold = ColorFromHex(0xD9A441);
    public static readonly Color Health = ColorFromHex(0xD25A50);
    public static readonly Color Mana = ColorFromHex(0x5D8BD6);

    public static Color RuneColor(RuneChess.Core.RuneType rune)
    {
        return rune switch
        {
            RuneChess.Core.RuneType.Red => ColorFromHex(0xC94B4B),
            RuneChess.Core.RuneType.Blue => ColorFromHex(0x4A7ED1),
            RuneChess.Core.RuneType.Green => ColorFromHex(0x54A06A),
            RuneChess.Core.RuneType.Yellow => ColorFromHex(0xDFBF4F),
            RuneChess.Core.RuneType.Purple => ColorFromHex(0x8662BD),
            RuneChess.Core.RuneType.White => ColorFromHex(0xE8E2D2),
            _ => Text
        };
    }

    private static Color ColorFromHex(int hex)
    {
        var red = ((hex >> 16) & 0xFF) / 255f;
        var green = ((hex >> 8) & 0xFF) / 255f;
        var blue = (hex & 0xFF) / 255f;
        return new Color(red, green, blue, 1f);
    }
}
