using System;
using System.Collections.Generic;

namespace RuneChess.Core
{
    /// <summary>
    /// Engine-agnostic UI design tokens: the single source of truth for the
    /// portrait auto-battler presentation overhaul. Colors are packed as
    /// <c>0xRRGGBB</c> so the core package stays free of any engine type; the
    /// Unity presentation layer maps them onto its own colour struct via
    /// <c>GameColors</c>. All values are original art direction for this project
    /// and are not derived from any third-party game.
    /// </summary>
    public static class UiTheme
    {
        // --- Rarity tier colours (card borders / cost gem). ---
        public const uint CommonColor = 0x9AA4B0u;
        public const uint RareColor = 0x4A7ED1u;
        public const uint EpicColor = 0x8662BDu;
        public const uint LegendaryColor = 0xD9A441u;

        /// <summary>Tier colour for a hero rarity, used for card borders and gems.</summary>
        public static uint RarityColor(HeroRarity rarity)
        {
            return rarity switch
            {
                HeroRarity.Common => CommonColor,
                HeroRarity.Rare => RareColor,
                HeroRarity.Epic => EpicColor,
                HeroRarity.Legendary => LegendaryColor,
                _ => throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "Unknown hero rarity.")
            };
        }

        // --- Rune palette (match-3 board). Single source of truth for the six colours. ---
        public static uint RuneColor(RuneType rune)
        {
            return rune switch
            {
                RuneType.Red => 0xC94B4Bu,
                RuneType.Blue => 0x4A7ED1u,
                RuneType.Green => 0x54A06Au,
                RuneType.Yellow => 0xDFBF4Fu,
                RuneType.Purple => 0x8662BDu,
                RuneType.White => 0xE8E2D2u,
                _ => throw new ArgumentOutOfRangeException(nameof(rune), rune, "Unknown rune type.")
            };
        }

        // --- Synergy strength tier colours (alliance panel). ---
        public static uint SynergyTierColor(SynergyStrength strength)
        {
            return strength switch
            {
                SynergyStrength.Building => 0x6B7280u,
                SynergyStrength.Active => 0x5BC0A6u,
                SynergyStrength.Maxed => 0xE2B84Bu,
                _ => throw new ArgumentOutOfRangeException(nameof(strength), strength, "Unknown synergy strength.")
            };
        }

        // --- Spacing scale (density-independent units). Strictly ascending. ---
        public const float SpacingXs = 4f;
        public const float SpacingSm = 8f;
        public const float SpacingMd = 12f;
        public const float SpacingLg = 16f;
        public const float SpacingXl = 24f;
        public const float SpacingXxl = 32f;

        public static IReadOnlyList<float> SpacingScale { get; } = Array.AsReadOnly(new[]
        {
            SpacingXs,
            SpacingSm,
            SpacingMd,
            SpacingLg,
            SpacingXl,
            SpacingXxl
        });

        // --- Type scale (points). Strictly ascending. ---
        public const float TypeCaption = 12f;
        public const float TypeBody = 16f;
        public const float TypeSubtitle = 20f;
        public const float TypeTitle = 28f;
        public const float TypeDisplay = 40f;

        public static IReadOnlyList<float> TypeScale { get; } = Array.AsReadOnly(new[]
        {
            TypeCaption,
            TypeBody,
            TypeSubtitle,
            TypeTitle,
            TypeDisplay
        });

        // --- Shape tokens. ---
        public const float RadiusSmall = 6f;
        public const float RadiusMedium = 10f;
        public const float RadiusLarge = 16f;

        public const float BorderThin = 1f;
        public const float BorderRegular = 2f;
        public const float BorderThick = 4f;

        /// <summary>Height of the thin HP/MP bar drawn under a unit on the board.</summary>
        public const float UnitBarHeight = 6f;

        /// <summary>Height of a HUD resource bar (run HP, XP).</summary>
        public const float HudBarHeight = 14f;

        // --- Channel helpers (unpack a packed 0xRRGGBB value). ---
        public static int RedChannel(uint packed) => (int)((packed >> 16) & 0xFFu);

        public static int GreenChannel(uint packed) => (int)((packed >> 8) & 0xFFu);

        public static int BlueChannel(uint packed) => (int)(packed & 0xFFu);
    }
}
