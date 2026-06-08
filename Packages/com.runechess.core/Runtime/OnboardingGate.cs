using System;
using System.Collections.Generic;

namespace RuneChess.Core
{
    /// <summary>
    /// The concrete player action that completes one onboarding step (GDD "Обучение должно быть
    /// интерактивным, а не длинным текстовым объяснением"). Each tutorial round stays on screen
    /// until the player actually performs the gated action, so the tutorial advances by doing the
    /// mechanic rather than by tapping through text. One gate maps to exactly one
    /// <see cref="OnboardingMechanic"/>, so the reveal schedule and the interactive completion
    /// condition never drift apart.
    /// </summary>
    public enum OnboardingGate
    {
        /// <summary>Round 1: buy a hero and place it on the player side of the tactical field.</summary>
        PlaceHeroOnField,

        /// <summary>Round 2: clear both a red and a blue rune match in battle.</summary>
        MatchRedAndBlueRunes,

        /// <summary>Round 3: place a tank (defender role) on the front line.</summary>
        PlaceTankInFrontline,

        /// <summary>Round 4: resolve the risk/reward event by accepting or declining it.</summary>
        ResolveRiskEvent,

        /// <summary>Round 5: trigger a shield (yellow) or healing (green) rune effect.</summary>
        TriggerShieldOrHealRune,

        /// <summary>Round 6: keep a backline hero alive through an enemy that dives the back row.</summary>
        ProtectBacklineHero,

        /// <summary>Round 7: win a battle against an enemy that deals magic damage.</summary>
        CounterMagicDamage
    }

    /// <summary>
    /// Stable string ids for <see cref="OnboardingGate"/> so onboarding progress can be persisted
    /// and reloaded between sessions without depending on enum ordinal values.
    /// </summary>
    public static class OnboardingGates
    {
        public static string GetId(OnboardingGate gate) => gate switch
        {
            OnboardingGate.PlaceHeroOnField => "place_hero_on_field",
            OnboardingGate.MatchRedAndBlueRunes => "match_red_and_blue_runes",
            OnboardingGate.PlaceTankInFrontline => "place_tank_in_frontline",
            OnboardingGate.ResolveRiskEvent => "resolve_risk_event",
            OnboardingGate.TriggerShieldOrHealRune => "trigger_shield_or_heal_rune",
            OnboardingGate.ProtectBacklineHero => "protect_backline_hero",
            OnboardingGate.CounterMagicDamage => "counter_magic_damage",
            _ => throw new ArgumentOutOfRangeException(nameof(gate), gate, "Unknown onboarding gate.")
        };

        public static bool TryParseId(string? id, out OnboardingGate gate)
        {
            switch (id?.Trim().ToLowerInvariant())
            {
                case "place_hero_on_field":
                    gate = OnboardingGate.PlaceHeroOnField;
                    return true;
                case "match_red_and_blue_runes":
                    gate = OnboardingGate.MatchRedAndBlueRunes;
                    return true;
                case "place_tank_in_frontline":
                    gate = OnboardingGate.PlaceTankInFrontline;
                    return true;
                case "resolve_risk_event":
                    gate = OnboardingGate.ResolveRiskEvent;
                    return true;
                case "trigger_shield_or_heal_rune":
                    gate = OnboardingGate.TriggerShieldOrHealRune;
                    return true;
                case "protect_backline_hero":
                    gate = OnboardingGate.ProtectBacklineHero;
                    return true;
                case "counter_magic_damage":
                    gate = OnboardingGate.CounterMagicDamage;
                    return true;
                default:
                    gate = default;
                    return false;
            }
        }

        public static OnboardingGate ParseId(string id)
        {
            if (TryParseId(id, out var gate))
            {
                return gate;
            }

            throw new ArgumentException($"Unknown onboarding gate id '{id}'.", nameof(id));
        }

        /// <summary>The interactive gate that completes a given onboarding mechanic.</summary>
        public static OnboardingGate ForMechanic(OnboardingMechanic mechanic) => mechanic switch
        {
            OnboardingMechanic.BuyAndPlaceHero => OnboardingGate.PlaceHeroOnField,
            OnboardingMechanic.RedAndBlueRunes => OnboardingGate.MatchRedAndBlueRunes,
            OnboardingMechanic.TankAndPositioning => OnboardingGate.PlaceTankInFrontline,
            OnboardingMechanic.RiskAndReward => OnboardingGate.ResolveRiskEvent,
            OnboardingMechanic.ShieldsAndHealing => OnboardingGate.TriggerShieldOrHealRune,
            OnboardingMechanic.BacklineThreat => OnboardingGate.ProtectBacklineHero,
            OnboardingMechanic.MagicDamage => OnboardingGate.CounterMagicDamage,
            _ => throw new ArgumentOutOfRangeException(nameof(mechanic), mechanic, "Unknown onboarding mechanic.")
        };
    }
}
