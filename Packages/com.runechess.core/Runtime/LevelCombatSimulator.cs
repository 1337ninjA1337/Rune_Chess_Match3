using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// Resolves a deterministic autobattle for the level-complete summary so the
    /// presentation layer never owns combat rules. The primary path fights the player's
    /// placed team against the data-driven PvE roster declared on the round
    /// (<see cref="PveRoundDefinition.EnemyUnits"/>), which keeps encounters aligned with
    /// the GDD "Первые 10 раундов" table. The legacy mirror helpers are retained for
    /// fallback rounds that carry no authored roster (and for deterministic test plumbing).
    /// </summary>
    public static class LevelCombatSimulator
    {
        public const double DefaultTickSeconds = 0.1;

        /// <summary>
        /// Build the round's data-driven enemy units from its authored composition. Each
        /// entry instantiates a hero definition at its star level on the enemy half.
        /// </summary>
        public static IReadOnlyList<BattleUnit> BuildRoundEnemies(PveRoundDefinition round)
        {
            if (round is null)
            {
                throw new ArgumentNullException(nameof(round));
            }

            var enemies = new List<BattleUnit>(round.EnemyUnits.Count);
            var index = 0;
            foreach (var enemy in round.EnemyUnits)
            {
                var definition = HeroCatalog.Get(enemy.HeroId);
                enemies.Add(BattleUnit.FromHero(
                    definition,
                    enemy.Stars,
                    $"enemy_{round.Round}_{index}_{enemy.HeroId}",
                    TacticalSide.Enemy,
                    enemy.Position));
                index += 1;
            }

            return enemies;
        }

        /// <summary>
        /// Run the player's team against the round's authored PvE roster to a resolved
        /// outcome (or the timer) and return the final <see cref="BattleState"/>. Returns
        /// <c>null</c> when there is nothing to fight: an empty team, a non-combat round,
        /// or a round with no authored enemy composition.
        /// </summary>
        public static BattleState? ResolveRoundMatch(
            IReadOnlyList<BoardHero> team,
            PveRoundDefinition round,
            double durationSeconds = BattleState.DefaultDurationSeconds,
            double tickSeconds = DefaultTickSeconds)
        {
            if (team is null)
            {
                throw new ArgumentNullException(nameof(team));
            }

            if (round is null)
            {
                throw new ArgumentNullException(nameof(round));
            }

            if (tickSeconds <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(tickSeconds), "Tick delta must be positive.");
            }

            if (team.Count == 0 || !round.HasEnemyComposition)
            {
                return null;
            }

            var allies = team
                .Select(boardHero => BattleUnit.FromBoardHero(boardHero, TacticalSide.Player))
                .ToList();
            var units = allies.Concat(BuildRoundEnemies(round)).ToList();

            return RunToResolution(units, durationSeconds, tickSeconds);
        }

        /// <summary>
        /// Build the enemy mirror of the player's team: each placed ally is copied to the
        /// reflected row on the enemy half of the MVP field, keeping its column.
        /// </summary>
        public static IReadOnlyList<BattleUnit> BuildMirrorEnemies(IReadOnlyList<BoardHero> team)
        {
            if (team is null)
            {
                throw new ArgumentNullException(nameof(team));
            }

            var enemies = new List<BattleUnit>(team.Count);
            var index = 0;
            foreach (var boardHero in team)
            {
                var mirroredRow = TacticalField.MvpRows - 1 - boardHero.Position.Row;
                var mirroredPosition = new TacticalPosition(mirroredRow, boardHero.Position.Column);
                var definition = HeroCatalog.Get(boardHero.Hero.HeroId);
                enemies.Add(BattleUnit.FromHero(
                    definition,
                    boardHero.Hero.Stars,
                    $"enemy_mirror_{index}_{boardHero.Hero.InstanceId}",
                    TacticalSide.Enemy,
                    mirroredPosition));
                index += 1;
            }

            return enemies;
        }

        /// <summary>
        /// Run the player's team against the mirror enemy to a resolved outcome (or the
        /// timer) and return the final <see cref="BattleState"/> with its accumulated
        /// player-centric combat totals. Returns <c>null</c> when there is nothing to
        /// fight (empty team), so callers can show a neutral summary.
        /// </summary>
        public static BattleState? ResolveMirrorMatch(
            IReadOnlyList<BoardHero> team,
            double durationSeconds = BattleState.DefaultDurationSeconds,
            double tickSeconds = DefaultTickSeconds)
        {
            if (team is null)
            {
                throw new ArgumentNullException(nameof(team));
            }

            if (tickSeconds <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(tickSeconds), "Tick delta must be positive.");
            }

            if (team.Count == 0)
            {
                return null;
            }

            var allies = team
                .Select(boardHero => BattleUnit.FromBoardHero(boardHero, TacticalSide.Player))
                .ToList();
            var units = allies.Concat(BuildMirrorEnemies(team)).ToList();

            return RunToResolution(units, durationSeconds, tickSeconds);
        }

        /// <summary>
        /// Tick a freshly created battle to its resolution or timer, capping iterations so a
        /// degenerate stalemate can never spin forever.
        /// </summary>
        private static BattleState RunToResolution(
            IReadOnlyList<BattleUnit> units,
            double durationSeconds,
            double tickSeconds)
        {
            var battle = BattleState.Create(units, durationSeconds);
            var maxTicks = (int)Math.Ceiling(durationSeconds / tickSeconds) + 2;
            for (var tick = 0; tick < maxTicks && !battle.IsResolved; tick += 1)
            {
                battle = battle.Tick(tickSeconds);
            }

            return battle;
        }
    }
}
