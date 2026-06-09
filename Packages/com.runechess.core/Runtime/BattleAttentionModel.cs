using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// The two play zones that compete for the player's attention on the combat screen at the
    /// same time (GDD/codex: "Сделать бой визуально читаемым при одновременном автобое и match-3").
    /// The auto battler runs on the tactical field while the player matches runes on the 7x7 board,
    /// so cues from both can arrive together and must be coordinated into one readable beat.
    /// </summary>
    public enum BattleZone
    {
        /// <summary>The tactical field: hero attacks, ability casts, deaths — the auto battler.</summary>
        AutoBattle,

        /// <summary>The 7x7 rune board: the player's match-3 moves and their resolved effects.</summary>
        Match3
    }

    /// <summary>
    /// One emphasis-worthy beat on the combat screen, tagged with the zone it comes from and how
    /// strongly it should be surfaced. A cue is the unit of attention the readability layer arbitrates:
    /// match-3 cues are built from a resolved <see cref="RuneEffect"/>; auto-battle cues are built by
    /// the tactical layer for events it owns (a clutch ability, a unit dying). Pure data so the whole
    /// arbitration stays smoke-testable without the Unity render layer.
    /// </summary>
    public sealed record BattleCue(
        BattleZone Zone,
        BattleEventSalience Salience,
        int ChainDepth,
        double Power,
        string Label)
    {
        /// <summary>True when the cue comes from the match-3 board.</summary>
        public bool IsMatch3 => Zone == BattleZone.Match3;

        /// <summary>True when the cue comes from the auto battler on the tactical field.</summary>
        public bool IsAutoBattle => Zone == BattleZone.AutoBattle;

        /// <summary>
        /// Build a match-3 cue from a resolved rune effect, reusing the shared salience scoring so a
        /// rune beat reads with the same weight on the board as it does in the effect list.
        /// </summary>
        public static BattleCue FromRuneEffect(RuneEffect effect, string? label = null)
        {
            if (effect is null)
            {
                throw new ArgumentNullException(nameof(effect));
            }

            return new BattleCue(
                Zone: BattleZone.Match3,
                Salience: BattleReadabilityModel.SalienceOf(effect),
                ChainDepth: Math.Max(0, effect.ChainNumber - 1),
                Power: Math.Max(0.0, effect.Power),
                Label: label ?? CombatRuneEffectChip.DescribeKind(effect.Kind));
        }

        /// <summary>
        /// Build an auto-battle cue for a tactical-field event the combat layer wants to emphasise
        /// (an ability cast, a unit death, a heavy hit). The tactical layer scores its own salience.
        /// </summary>
        public static BattleCue AutoBattle(BattleEventSalience salience, double power, string label)
        {
            if (power < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(power), "Cue power cannot be negative.");
            }

            return new BattleCue(
                Zone: BattleZone.AutoBattle,
                Salience: salience,
                ChainDepth: 0,
                Power: power,
                Label: label);
        }
    }

    /// <summary>
    /// Coordinates attention across the two simultaneous combat zones so the screen stays readable
    /// while the auto battler and match-3 resolve together (codex UI rules: "Бой должен быть
    /// читаемым: не перегружай экран одновременными эффектами"; "приоритет у читаемости, компактности
    /// и быстрого принятия решений"). Where <see cref="BattleReadabilityModel"/> ranks and caps
    /// match-3 rune effects on their own, this model decides — when both zones fire at once — which
    /// zone owns the player's focus, dims the other when a fight-swinging beat needs the screen, and
    /// shares the single on-screen emphasis budget across both zones rather than letting each spend it
    /// independently. Pure data; the Unity render layer is a thin consumer and live attention/overload
    /// playtests remain the documented verification gap (no Unity runtime in this environment).
    /// </summary>
    public static class BattleAttentionModel
    {
        /// <summary>
        /// The whole-screen emphasis budget across both zones combined: the auto battler and the
        /// match-3 board may surface at most this many cues together, so simultaneous activity reads
        /// as one focused beat. Shares <see cref="BattleReadabilityModel.MaxSimultaneousEffects"/> so
        /// the cross-zone cap never exceeds the per-zone match-3 cap.
        /// </summary>
        public const int MaxSimultaneousCues = BattleReadabilityModel.MaxSimultaneousEffects;

        /// <summary>
        /// Decide which zone the player should look at right now. The single most salient cue wins;
        /// among equally salient cues the zone carrying the deepest chain wins, and a final tie breaks
        /// toward the auto battler because composition combat is the primary loop (design pillar
        /// "состав остаётся главным решением"). Auto-battle cues carry no chain depth, so only an
        /// escalating match-3 combo — the one the clock literally slows for — steals focus from an
        /// equally salient auto-battle beat. Returns <c>null</c> when there are no cues to focus.
        /// </summary>
        public static BattleZone? PrimaryFocus(IReadOnlyList<BattleCue> cues)
        {
            if (cues is null)
            {
                throw new ArgumentNullException(nameof(cues));
            }

            if (cues.Count == 0)
            {
                return null;
            }

            var topSalience = cues.Max(cue => (int)cue.Salience);
            var leaders = cues.Where(cue => (int)cue.Salience == topSalience).ToList();
            var deepestChain = leaders.Max(cue => cue.ChainDepth);
            var deepest = leaders.Where(cue => cue.ChainDepth == deepestChain).ToList();

            // Final tie (equal salience and chain depth) hands focus to the auto battler: the
            // tactical field is the primary loop, so the player's default glance belongs there.
            return deepest.Any(cue => cue.IsAutoBattle) ? BattleZone.AutoBattle : BattleZone.Match3;
        }

        /// <summary>
        /// Choose which cues to surface across the whole screen when both zones fire together, capped
        /// at <paramref name="cap"/> (defaults to <see cref="MaxSimultaneousCues"/>). Ordering: most
        /// salient first, then cues in the focus zone (so the player's eye is led to one place), then
        /// chain depth, then power, then a stable zone tiebreak. Deterministic for layout.
        /// </summary>
        public static IReadOnlyList<BattleCue> SelectVisibleCues(IReadOnlyList<BattleCue> cues, int? cap = null)
        {
            if (cues is null)
            {
                throw new ArgumentNullException(nameof(cues));
            }

            var limit = cap ?? MaxSimultaneousCues;
            if (limit < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cap), "Cue cap cannot be negative.");
            }

            var focus = PrimaryFocus(cues);
            return cues
                .OrderByDescending(cue => (int)cue.Salience)
                .ThenByDescending(cue => focus.HasValue && cue.Zone == focus.Value)
                .ThenByDescending(cue => cue.ChainDepth)
                .ThenByDescending(cue => cue.Power)
                .ThenBy(cue => (int)cue.Zone)
                .Take(limit)
                .ToList();
        }

        /// <summary>
        /// True when the off-focus zone should be dimmed to keep a fight-swinging beat readable: when
        /// the focus zone carries a <see cref="BattleEventSalience.Critical"/> cue, the other zone
        /// should recede so two critical flashes never compete for the same glance.
        /// </summary>
        public static bool ShouldDimOffFocusZone(IReadOnlyList<BattleCue> cues)
        {
            if (cues is null)
            {
                throw new ArgumentNullException(nameof(cues));
            }

            var focus = PrimaryFocus(cues);
            if (!focus.HasValue)
            {
                return false;
            }

            return cues.Any(cue => cue.Zone == focus.Value && cue.Salience == BattleEventSalience.Critical);
        }

        /// <summary>
        /// Invariant the render layer must respect: the whole screen never emphasises more than the
        /// shared budget across both zones at once (codex: "Проверить, что UI не перегружает внимание
        /// игрока"). <see cref="SelectVisibleCues"/> always satisfies it.
        /// </summary>
        public static bool RespectsAttentionBudget(IReadOnlyList<BattleCue> shownCues, int? cap = null)
        {
            if (shownCues is null)
            {
                throw new ArgumentNullException(nameof(shownCues));
            }

            return shownCues.Count <= (cap ?? MaxSimultaneousCues);
        }
    }
}
