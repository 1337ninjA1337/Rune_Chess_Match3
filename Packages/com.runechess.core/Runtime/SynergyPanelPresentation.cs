using System;
using System.Linq;

namespace RuneChess.Core
{
    /// <summary>
    /// Engine-agnostic presentation glue for the synergy/alliance panel overhaul. It turns a
    /// <see cref="SynergyPanelEntry"/> from <see cref="SynergyPanelModel"/> into the bits the Unity
    /// panel renders — the faction/class icon key, the strength-tier colour, the
    /// «current/threshold» count label, the expand/tooltip text and the beginner spotlight flag —
    /// without the view re-deriving any synergy maths. Colours are the shared <see cref="UiTheme"/>
    /// synergy-tier tokens (original art direction, codex.md IP rule — original alliance tiers, not
    /// Dota terms). Rendering itself is Unity-only (documented gap); the contract is verified
    /// headless by <c>tools/CoreSmoke</c>.
    /// </summary>
    public static class SynergyPanelPresentation
    {
        /// <summary>Stable placeholder icon key for an entry (faction or class).</summary>
        public static string IconKey(SynergyPanelEntry entry)
        {
            if (entry is null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            return entry.Kind switch
            {
                SynergyKind.Faction => $"faction.{entry.Id}",
                SynergyKind.Class => $"class.{entry.Id}",
                _ => throw new ArgumentOutOfRangeException(nameof(entry), entry.Kind, "Unknown synergy kind.")
            };
        }

        /// <summary>Placeholder icon spec for an entry, resolved from the asset catalog.</summary>
        public static PlaceholderAssetSpec Icon(SynergyPanelEntry entry)
        {
            var key = IconKey(entry);
            if (!PlaceholderAssetCatalog.TryGet(key, out var spec))
            {
                throw new InvalidOperationException($"No placeholder icon registered for synergy entry key '{key}'.");
            }

            return spec;
        }

        /// <summary>Packed strength-tier colour for an entry (building / active / maxed).</summary>
        public static uint TierColor(SynergyPanelEntry entry)
        {
            if (entry is null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            return UiTheme.SynergyTierColor(entry.Strength);
        }

        /// <summary>
        /// «current/threshold» count label. While a breakpoint is still ahead it reads
        /// «held/required» (e.g. «2/4»); once every tier is reached it reads «held · макс».
        /// </summary>
        public static string ThresholdLabel(SynergyPanelEntry entry)
        {
            if (entry is null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            return entry.HasNextTier
                ? $"{entry.UnitCount}/{entry.NextTier!.RequiredCount}"
                : $"{entry.UnitCount} · макс";
        }

        /// <summary>
        /// True when this entry is the one the panel should spotlight for a new player
        /// (<see cref="SynergyPanelModel.BeginnerHighlight"/>). Compared by kind + id so it matches
        /// the same logical synergy even across rebuilt models.
        /// </summary>
        public static bool IsBeginnerSpotlight(SynergyPanelModel model, SynergyPanelEntry entry)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (entry is null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            return model.BeginnerHighlight is { } highlight
                && highlight.Kind == entry.Kind
                && string.Equals(highlight.Id, entry.Id, StringComparison.Ordinal);
        }

        /// <summary>
        /// Expand/tooltip text describing the synergy effect: its focus, the effects of its active
        /// tiers, the next breakpoint still to reach and the heroes that would close it. Built here
        /// so the Unity tooltip never re-derives the synergy maths.
        /// </summary>
        public static string TooltipText(SynergyPanelEntry entry)
        {
            if (entry is null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            var lines = new System.Collections.Generic.List<string>(4)
            {
                $"{entry.Name} — {entry.Focus}"
            };

            if (entry.ActiveTiers.Count > 0)
            {
                var active = string.Join("; ", entry.ActiveTiers.Select(tier => tier.Effect));
                lines.Add($"Активно: {active}");
            }

            if (entry.HasNextTier)
            {
                lines.Add($"Дальше ({entry.NextTier!.RequiredCount}): {entry.NextTier.Effect}");
                if (entry.HeroesToNextTier > 0)
                {
                    lines.Add($"Нужно ещё: {entry.HeroesToNextTier}");
                }
            }
            else
            {
                lines.Add("Синергия на максимуме.");
            }

            return string.Join("\n", lines);
        }
    }
}
