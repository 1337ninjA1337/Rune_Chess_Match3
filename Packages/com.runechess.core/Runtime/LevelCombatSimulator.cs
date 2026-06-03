using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// Resolves a deterministic autobattle for the level-complete summary so the
    /// presentation layer never owns combat rules. The MVP enemy is a mirror of the
    /// player's own placed team (same heroes/stars, reflected onto the enemy side),
    /// which keeps the fight readable and deterministic without inventing PvE balance
    /// data. Replace the mirror with the GDD PvE roster once enemy compositions are
    /// data-driven (see tasks/.tasks.md).
    /// </summary>
    public static class LevelCombatSimulator
    {
        public const double DefaultTickSeconds = 0.1;

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

            var battle = BattleState.Create(units, durationSeconds);

            // Cap iterations defensively so a degenerate stalemate can never spin forever.
            var maxTicks = (int)Math.Ceiling(durationSeconds / tickSeconds) + 2;
            for (var tick = 0; tick < maxTicks && !battle.IsResolved; tick += 1)
            {
                battle = battle.Tick(tickSeconds);
            }

            return battle;
        }
    }
}
