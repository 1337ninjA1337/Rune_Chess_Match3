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

        // Rune and tier colours come from the engine-agnostic UiTheme token set
        // so the match-3 board, rarity borders, and synergy panel share one
        // source of truth with the core package (and its smoke checks).
        public static Color RuneColor(RuneType rune)
        {
            return ColorFromPacked(UiTheme.RuneColor(rune));
        }

        /// <summary>Border/gem colour for a hero rarity (auto-battler card tiers).</summary>
        public static Color RarityColor(HeroRarity rarity)
        {
            return ColorFromPacked(UiTheme.RarityColor(rarity));
        }

        /// <summary>Colour for a synergy strength tier in the alliance panel.</summary>
        public static Color SynergyTierColor(SynergyStrength strength)
        {
            return ColorFromPacked(UiTheme.SynergyTierColor(strength));
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

        private static Color ColorFromPacked(uint packed)
        {
            return new Color(
                UiTheme.RedChannel(packed) / 255f,
                UiTheme.GreenChannel(packed) / 255f,
                UiTheme.BlueChannel(packed) / 255f,
                1f);
        }
    }
}
