using RuneChess.Core;
using UnityEngine;

namespace RuneChess.Presentation
{
    public static class GameColors
    {
        public static readonly Color Background = ColorFromHex(0x141619);
        public static readonly Color Frame = ColorFromHex(0x101114);
        public static readonly Color Panel = ColorFromHex(0x22262B);
        public static readonly Color PanelDeep = ColorFromHex(0x1A1D22);
        public static readonly Color PanelRaised = ColorFromHex(0x2E333A);
        public static readonly Color Border = ColorFromHex(0x515761);
        public static readonly Color Text = ColorFromHex(0xF5F0E6);
        public static readonly Color Muted = ColorFromHex(0xB8B0A3);

        public static readonly Color EnemyCell = ColorFromHex(0x342638);
        public static readonly Color PlayerCell = ColorFromHex(0x20352F);
        public static readonly Color CellAvailable = ColorFromHex(0x2F5C46);
        public static readonly Color CellUnavailable = ColorFromHex(0x25282E);
        public static readonly Color AllyCellOccupied = ColorFromHex(0x255B4B);
        public static readonly Color EnemyCellOccupied = ColorFromHex(0x5B2C36);

        public static readonly Color Gold = ColorFromHex(0xD9A441);
        public static readonly Color Health = ColorFromHex(0xD85F57);
        public static readonly Color Mana = ColorFromHex(0x5D8BD6);
        public static readonly Color Commander = ColorFromHex(0xC884D8);
        public static readonly Color Shield = ColorFromHex(0xE2C75A);
        public static readonly Color Heal = ColorFromHex(0x68B977);
        public static readonly Color BarTrack = ColorFromHex(0x111317);
        public static readonly Color Button = ColorFromHex(0x343942);
        public static readonly Color ButtonPrimary = ColorFromHex(0xD6A542);

        public static Color TacticalCellColor(TacticalCellState state)
        {
            switch (state)
            {
                case TacticalCellState.Free:
                    return PlayerCell;
                case TacticalCellState.OccupiedAlly:
                    return AllyCellOccupied;
                case TacticalCellState.OccupiedEnemy:
                    return EnemyCellOccupied;
                case TacticalCellState.AvailableForPlacement:
                    return CellAvailable;
                case TacticalCellState.Unavailable:
                    return CellUnavailable;
                default:
                    return PanelRaised;
            }
        }

        public static Color RuneColor(RuneType rune)
        {
            switch (rune)
            {
                case RuneType.Red:
                    return ColorFromHex(0xC94B4B);
                case RuneType.Blue:
                    return ColorFromHex(0x4A7ED1);
                case RuneType.Green:
                    return ColorFromHex(0x54A06A);
                case RuneType.Yellow:
                    return ColorFromHex(0xDFBF4F);
                case RuneType.Purple:
                    return ColorFromHex(0x8662BD);
                case RuneType.White:
                    return ColorFromHex(0xE8E2D2);
                default:
                    return Text;
            }
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        private static Color ColorFromHex(int hex)
        {
            var red = ((hex >> 16) & 0xFF) / 255f;
            var green = ((hex >> 8) & 0xFF) / 255f;
            var blue = (hex & 0xFF) / 255f;
            return new Color(red, green, blue, 1f);
        }
    }
}
