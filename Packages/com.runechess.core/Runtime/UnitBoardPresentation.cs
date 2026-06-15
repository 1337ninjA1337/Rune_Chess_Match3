using System;
using System.Collections.Generic;

namespace RuneChess.Core
{
    /// <summary>Which way a unit's sprite faces so allies and enemies look at each other.</summary>
    public enum UnitFacing
    {
        /// <summary>Faces toward the top of the board (player units, whose enemies sit above).</summary>
        Up,

        /// <summary>Faces toward the bottom of the board (enemy units, whose foes sit below).</summary>
        Down
    }

    /// <summary>
    /// Status badge shown over a unit on the board (GDD «индикаторы статуса»: щит, бафф,
    /// дебафф, оглушение, анти-хил, призыв). The Unity layer draws an icon per active kind.
    /// </summary>
    public enum UnitStatusKind
    {
        Shield,
        Buff,
        Debuff,
        Stun,
        AntiHeal,
        Summon
    }

    /// <summary>
    /// A transient visual cue a unit plays during combat. These are presentation timings the
    /// Unity layer animates; the model only owns the trigger contract and ordered durations.
    /// </summary>
    public enum UnitAnimationCue
    {
        /// <summary>Brief lunge/strike when the unit lands an auto attack.</summary>
        Attack,

        /// <summary>Quick flash when the unit takes damage.</summary>
        DamageFlash,

        /// <summary>Burst/glyph when the unit casts its ability.</summary>
        AbilityBurst,

        /// <summary>Fade-out when the unit dies.</summary>
        Death
    }

    /// <summary>Catalog helper for the status badge set (mirrors <see cref="RuneTypes"/>/<see cref="HeroRarities"/>).</summary>
    public static class UnitStatuses
    {
        public static IReadOnlyList<UnitStatusKind> All { get; } = Array.AsReadOnly(new[]
        {
            UnitStatusKind.Shield,
            UnitStatusKind.Buff,
            UnitStatusKind.Debuff,
            UnitStatusKind.Stun,
            UnitStatusKind.AntiHeal,
            UnitStatusKind.Summon
        });

        public static string GetId(UnitStatusKind kind)
        {
            return kind switch
            {
                UnitStatusKind.Shield => "shield",
                UnitStatusKind.Buff => "buff",
                UnitStatusKind.Debuff => "debuff",
                UnitStatusKind.Stun => "stun",
                UnitStatusKind.AntiHeal => "antiheal",
                UnitStatusKind.Summon => "summon",
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown unit status kind.")
            };
        }
    }

    /// <summary>
    /// Resolved per-unit board view: the bars, facing and active status badges the Unity layer
    /// renders under/over a unit. Pure data so the renderer only draws what this computes.
    /// </summary>
    public sealed record UnitBoardSnapshot(
        UnitFacing Facing,
        double HealthFraction,
        double ManaFraction,
        bool IsAlive,
        IReadOnlyList<UnitStatusKind> Statuses);

    /// <summary>
    /// Engine-agnostic single source of truth for the on-board unit visuals overhaul: sprite
    /// facing, star-tier pip colours, the HP/mana bars, the rarity frame and the status badge /
    /// animation-cue contracts. Colours are packed <c>0xRRGGBB</c> so the core package stays free
    /// of any engine type (the Unity layer maps them via <c>GameColors</c>). All values are
    /// original art direction (codex.md IP rule) and follow the "Unit on board" anatomy in
    /// <c>docs/visual-style.md</c>.
    ///
    /// Rendering and the animations themselves are Unity-only (documented verification gap — no
    /// Unity/.NET SDK here); the contract (ordered/positive timings, clamped bar fills, facing,
    /// status derivation, tier colours) is verified headless by <c>tools/CoreSmoke</c>.
    /// </summary>
    public static class UnitBoardPresentation
    {
        // --- Star pips. A unit shows 1..3 pips tinted by tier so upgrades read at a glance. ---

        public const int MinStars = 1;
        public const int MaxStars = 3;

        /// <summary>Tier pip colours: bronze (1★) → silver (2★) → gold (3★). Original tokens.</summary>
        public const uint StarTier1Color = 0xB07A4Bu;
        public const uint StarTier2Color = 0xC9D2DCu;
        public const uint StarTier3Color = 0xE8C45Au;

        /// <summary>Pip colour for a star count (clamped to the supported 1..3 tiers).</summary>
        public static uint StarTierColor(int stars)
        {
            if (stars < MinStars || stars > MaxStars)
            {
                throw new ArgumentOutOfRangeException(nameof(stars), stars, "Unit stars must be 1..3.");
            }

            return stars switch
            {
                1 => StarTier1Color,
                2 => StarTier2Color,
                3 => StarTier3Color,
                _ => throw new ArgumentOutOfRangeException(nameof(stars), stars, "Unit stars must be 1..3.")
            };
        }

        // --- Facing. Player units look up the board, enemy units look down, so they face off. ---

        /// <summary>Which way a unit on the given side faces.</summary>
        public static UnitFacing FacingFor(TacticalSide side)
        {
            return side switch
            {
                TacticalSide.Player => UnitFacing.Up,
                TacticalSide.Enemy => UnitFacing.Down,
                _ => throw new ArgumentOutOfRangeException(nameof(side), side, "Unknown tactical side.")
            };
        }

        // --- HP / mana bars under the unit. Heights come from the shared UiTheme token. ---

        /// <summary>Height of the thin HP/mana bar drawn under a unit (shared UiTheme token).</summary>
        public const float BarHeight = UiTheme.UnitBarHeight;

        /// <summary>Clamp a raw 0..1-ish fraction into the renderable [0,1] range.</summary>
        public static double ClampFraction(double fraction)
        {
            if (fraction <= 0.0)
            {
                return 0.0;
            }

            return fraction >= 1.0 ? 1.0 : fraction;
        }

        // --- Rarity frame. Delegates to the shared rarity tokens / placeholder catalog. ---

        /// <summary>Border/highlight colour for a unit's rarity frame.</summary>
        public static uint RarityFrameColor(HeroRarity rarity) => UiTheme.RarityColor(rarity);

        /// <summary>Placeholder frame asset for a unit's rarity.</summary>
        public static PlaceholderAssetSpec RarityFrame(HeroRarity rarity) => PlaceholderAssetCatalog.RarityFrame(rarity);

        // --- Status badges. Icons live in the placeholder catalog; here we derive which are
        // active from a live BattleUnit where the simulation models them. ---

        /// <summary>Placeholder icon asset for a status badge.</summary>
        public static PlaceholderAssetSpec StatusIcon(UnitStatusKind kind) => PlaceholderAssetCatalog.StatusIcon(kind);

        /// <summary>
        /// The status badges currently active on a unit, in a stable display order. Only the
        /// statuses the autobattle actually models are derived (shield, buff via lifesteal,
        /// debuff via weakness, timed summon); stun/anti-heal icons exist for future mechanics.
        /// </summary>
        public static IReadOnlyList<UnitStatusKind> ActiveStatuses(BattleUnit unit)
        {
            if (unit is null)
            {
                throw new ArgumentNullException(nameof(unit));
            }

            var statuses = new List<UnitStatusKind>(4);
            if (unit.Shield > 0.0)
            {
                statuses.Add(UnitStatusKind.Shield);
            }

            if (unit.HasActiveLifesteal)
            {
                statuses.Add(UnitStatusKind.Buff);
            }

            if (unit.HasActiveWeakness)
            {
                statuses.Add(UnitStatusKind.Debuff);
            }

            if (unit.HasTimedSummon)
            {
                statuses.Add(UnitStatusKind.Summon);
            }

            return statuses;
        }

        // --- Animation cues. Durations (seconds) are positive and ordered so a quick damage
        // flash never outlasts the death fade; the Unity layer plays the actual tweens. ---

        public const double DamageFlashSeconds = 0.12;
        public const double AttackSeconds = 0.25;
        public const double AbilityBurstSeconds = 0.40;
        public const double DeathFadeSeconds = 0.50;

        /// <summary>Duration (seconds) for an animation cue.</summary>
        public static double CueDurationSeconds(UnitAnimationCue cue)
        {
            return cue switch
            {
                UnitAnimationCue.DamageFlash => DamageFlashSeconds,
                UnitAnimationCue.Attack => AttackSeconds,
                UnitAnimationCue.AbilityBurst => AbilityBurstSeconds,
                UnitAnimationCue.Death => DeathFadeSeconds,
                _ => throw new ArgumentOutOfRangeException(nameof(cue), cue, "Unknown unit animation cue.")
            };
        }

        // --- Full per-unit resolution for the renderer. ---

        /// <summary>
        /// Resolve a live combat unit into its board view (facing, clamped HP/mana fractions,
        /// alive flag and active status badges).
        /// </summary>
        public static UnitBoardSnapshot Resolve(BattleUnit unit)
        {
            if (unit is null)
            {
                throw new ArgumentNullException(nameof(unit));
            }

            var manaFraction = unit.ManaMax <= 0.0 ? 0.0 : ClampFraction(unit.CurrentMana / unit.ManaMax);
            return new UnitBoardSnapshot(
                FacingFor(unit.Side),
                ClampFraction(unit.HealthPercent),
                manaFraction,
                unit.IsAlive,
                ActiveStatuses(unit));
        }
    }
}
