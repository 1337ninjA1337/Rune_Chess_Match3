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

        // Tactical-board cell palette comes from the engine-agnostic TacticalBoardStyle
        // token set (single source of truth) so the board, its restyle and the smoke
        // checks never drift from the Unity layer.
        public static readonly Color EnemyCell = ColorFromPacked(TacticalBoardStyle.EnemyHalfColor);
        public static readonly Color PlayerCell = ColorFromPacked(TacticalBoardStyle.PlayerHalfColor);
        public static readonly Color CellAvailable = ColorFromPacked(TacticalBoardStyle.PlacementAvailableColor);
        public static readonly Color CellUnavailable = ColorFromPacked(TacticalBoardStyle.UnavailableColor);
        public static readonly Color AllyCellOccupied = ColorFromPacked(TacticalBoardStyle.AllyOccupiedColor);
        public static readonly Color EnemyCellOccupied = ColorFromPacked(TacticalBoardStyle.EnemyOccupiedColor);

        // Tactical-board restyle tokens: mid-line divider and the hover/selection overlay.
        public static readonly Color BoardMidLine = ColorFromPacked(TacticalBoardStyle.MidLineColor);
        public static readonly Color BoardCellBorder = ColorFromPacked(TacticalBoardStyle.CellBorderColor);
        public static readonly Color BoardPlacementBorder = ColorFromPacked(TacticalBoardStyle.PlacementBorderColor);

        public static readonly Color Gold = ColorFromPacked(UiTheme.GoldColor);
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
            return ColorFromPacked(TacticalBoardStyle.CellFillColor(state));
        }

        /// <summary>Outline colour for a tactical-board cell (placement targets glow brighter).</summary>
        public static Color TacticalCellBorderColor(TacticalCellState state)
        {
            return ColorFromPacked(TacticalBoardStyle.CellBorderColorFor(state));
        }

        /// <summary>
        /// Translucent hover/selection veil for a board cell, ready to lay over its fill. Idle
        /// cells return a fully transparent overlay so the caller can blit unconditionally.
        /// </summary>
        public static Color TacticalCellOverlay(TacticalCellInteraction interaction)
        {
            return WithAlpha(
                ColorFromPacked(TacticalBoardStyle.InteractionOverlayColor),
                TacticalBoardStyle.InteractionOverlayOpacity(interaction));
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

        /// <summary>Star pip colour for a unit's star tier (bronze/silver/gold).</summary>
        public static Color StarTierColor(int stars)
        {
            return ColorFromPacked(UnitBoardPresentation.StarTierColor(stars));
        }

        /// <summary>Runtime tint for a unit status badge (shield/buff/debuff/stun/anti-heal/summon).</summary>
        public static Color UnitStatusColor(UnitStatusKind kind)
        {
            switch (kind)
            {
                case UnitStatusKind.Shield:
                    return Shield;
                case UnitStatusKind.Buff:
                    return Heal;
                case UnitStatusKind.Debuff:
                    return Commander;
                case UnitStatusKind.Stun:
                    return ButtonPrimary;
                case UnitStatusKind.AntiHeal:
                    return Health;
                case UnitStatusKind.Summon:
                    return Mana;
                default:
                    return Muted;
            }
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        /// <summary>
        /// Tint a placeholder asset swatch from its <see cref="UiTheme"/> token colour (single source
        /// of truth). Assets whose colour is decided at runtime (no token) fall back to the muted tone.
        /// </summary>
        public static Color PlaceholderTint(PlaceholderAssetSpec spec)
        {
            if (spec == null)
            {
                return Muted;
            }

            return spec.TokenColor.HasValue ? ColorFromPacked(spec.TokenColor.Value) : Muted;
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
