using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// Resolves a deterministic autobattle for the level-complete summary so the
    /// presentation layer never owns combat rules. The enemy is the data-driven PvE
    /// roster authored on the round (<see cref="PveRoundDefinition.Roster"/>), built
    /// from the GDD "Первые 10 раундов" compositions, rather than a mirror of the
    /// player's own team. This keeps each round's threat readable and authored as
    /// data while staying deterministic.
    /// </summary>
    public static class LevelCombatSimulator
    {
        public const double DefaultTickSeconds = 0.1;

        /// <summary>
        /// Build the enemy battle units for a round from its data-driven roster. Each
        /// roster entry resolves a hero definition at its authored star level and is
        /// placed on the enemy half of the MVP field.
        /// </summary>
        public static IReadOnlyList<BattleUnit> BuildRoundEnemies(PveRoundDefinition round)
        {
            if (round is null)
            {
                throw new ArgumentNullException(nameof(round));
            }

            var roster = round.Roster;
            var enemies = new List<BattleUnit>(roster.Count);
            var index = 0;
            foreach (var enemy in roster)
            {
                if (!TacticalField.Mvp.IsEnemySide(enemy.Position))
                {
                    throw new ArgumentException(
                        $"PvE enemy '{enemy.HeroId}' for round {round.Round} must be placed on the enemy half of the field.",
                        nameof(round));
                }

                var definition = HeroCatalog.Get(enemy.HeroId);
                enemies.Add(BattleUnit.FromHero(
                    definition,
                    enemy.Stars,
                    $"enemy_r{round.Round}_{index}_{enemy.HeroId}",
                    TacticalSide.Enemy,
                    enemy.Position));
                index += 1;
            }

            return enemies;
        }

        /// <summary>
        /// Run the player's team against the round's data-driven enemy roster to a
        /// resolved outcome (or the timer) and return the final <see cref="BattleState"/>
        /// with its accumulated player-centric combat totals. Returns <c>null</c> when
        /// there is nothing to fight (empty team, or a round with no authored enemy
        /// roster such as an event/shop round), so callers can show a neutral summary.
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

            if (team.Count == 0)
            {
                return null;
            }

            var enemies = BuildRoundEnemies(round);
            if (enemies.Count == 0)
            {
                return null;
            }

            var allies = team
                .Select(boardHero => BattleUnit.FromBoardHero(boardHero, TacticalSide.Player))
                .ToList();
            var units = allies.Concat(enemies).ToList();

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
