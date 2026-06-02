using System;
using System.Collections.Generic;
using System.Linq;

namespace RuneChess.Core
{

/// <summary>
/// Combat effect category produced by collecting a colored rune match.
/// Mirrors the GDD "Эффекты по цветам" table.
/// </summary>
public enum RuneEffectKind
{
    PhysicalDamage,
    Mana,
    Healing,
    Shield,
    MagicDamage,
    CommanderEnergy
}

/// <summary>Match size tier: 3 = base effect, 4 = enhanced effect, 5+ = great rune.</summary>
public enum RuneMatchTier
{
    Match3,
    Match4,
    Match5
}

/// <summary>
/// A single discrete rune match: a connected, same-color group of matched cells.
/// Knows its size, effect tier and whether it forms a straight line or a bent T/L shape.
/// </summary>
public sealed record RuneMatchGroup(
    RuneType Rune,
    IReadOnlyCollection<BoardPoint> Cells,
    bool IsTOrLShaped,
    bool ContainsGreatRune
)
{
    public int Size => Cells.Count;
    public RuneMatchTier Tier => RuneEffects.GetTier(Size);

    /// <summary>T/L combos count as mass effects per the GDD.</summary>
    public bool IsMassEffect => IsTOrLShaped;

    /// <summary>A stored great rune activates when its cell is included in a later match.</summary>
    public bool ActivatesGreatRune => ContainsGreatRune;
}

/// <summary>
/// A resolved combat effect for a single rune match, with the final magnitude
/// after applying tier, T/L, chain and great-rune modifiers.
/// </summary>
public sealed record RuneEffect(
    RuneType Rune,
    RuneEffectKind Kind,
    RuneMatchTier Tier,
    int MatchedRunesCount,
    int ChainNumber,
    bool IsMassEffect,
    bool ChargesHero,
    bool CreatesGreatRune,
    bool IsGreatRuneActivation,
    int CommanderEnergy,
    double Power
)
{
    public bool IsDamage => Kind is RuneEffectKind.PhysicalDamage or RuneEffectKind.MagicDamage;
}

/// <summary>Shared rune-effect constants and color/tier mappings from the GDD.</summary>
public static class RuneEffects
{
    /// <summary>Great rune activation multiplier from the GDD ("усиливает эффект цвета в 2.5 раза").</summary>
    public const double GreatRuneMultiplier = 2.5;

    /// <summary>matchPower bonus granted by a T/L combo.</summary>
    public const int TShapeMatchPowerBonus = 2;

    /// <summary>Commander energy granted by a T/L combo.</summary>
    public const int TShapeCommanderEnergy = 10;

    public static RuneEffectKind GetEffectKind(RuneType rune)
    {
        return rune switch
        {
            RuneType.Red => RuneEffectKind.PhysicalDamage,
            RuneType.Blue => RuneEffectKind.Mana,
            RuneType.Green => RuneEffectKind.Healing,
            RuneType.Yellow => RuneEffectKind.Shield,
            RuneType.Purple => RuneEffectKind.MagicDamage,
            RuneType.White => RuneEffectKind.CommanderEnergy,
            _ => throw new ArgumentOutOfRangeException(nameof(rune), rune, "Unknown rune type.")
        };
    }

    public static RuneMatchTier GetTier(int matchedRunesCount)
    {
        if (matchedRunesCount < 3)
        {
            throw new ArgumentOutOfRangeException(
                nameof(matchedRunesCount),
                "A rune match needs at least three runes."
            );
        }

        return matchedRunesCount switch
        {
            3 => RuneMatchTier.Match3,
            4 => RuneMatchTier.Match4,
            _ => RuneMatchTier.Match5
        };
    }

    /// <summary>
    /// Chain reaction strength multiplier from the GDD:
    /// chain 1 x1.0, chain 2 x1.25, chain 3 x1.5, chain 4+ x2.0.
    /// </summary>
    public static double GetChainMultiplier(int chainNumber)
    {
        if (chainNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(chainNumber), "Chain number starts at one.");
        }

        return chainNumber switch
        {
            1 => 1.0,
            2 => 1.25,
            3 => 1.5,
            _ => 2.0
        };
    }

    public static double GetCommanderEnergyGain(RuneEffect effect)
    {
        if (effect is null)
        {
            throw new ArgumentNullException(nameof(effect));
        }

        return effect.CommanderEnergy
            + (effect.Kind == RuneEffectKind.CommanderEnergy ? effect.Power : 0.0);
    }
}

/// <summary>Turns rune match groups into resolved combat effects.</summary>
public static class RuneEffectResolver
{
    /// <summary>
    /// Resolves a single rune match group into a combat effect.
    /// <paramref name="chainNumber"/> is 1 for the swap match, 2 for the first chain reaction, and so on.
    /// </summary>
    public static RuneEffect Resolve(RuneMatchGroup group, int chainNumber, bool greatRuneActivated = false)
    {
        if (group is null)
        {
            throw new ArgumentNullException(nameof(group));
        }

        if (chainNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(chainNumber), "Chain number starts at one.");
        }

        var tier = group.Tier;
        var isMass = group.IsMassEffect;
        var comboDepth = chainNumber - 1;
        var baseMatchPower = Match3Scoring.CalculateMatchPower(group.Size, comboDepth)
            + (isMass ? RuneEffects.TShapeMatchPowerBonus : 0);
        var activatesGreatRune = greatRuneActivated || group.ActivatesGreatRune;
        var multiplier = RuneEffects.GetChainMultiplier(chainNumber)
            * (activatesGreatRune ? RuneEffects.GreatRuneMultiplier : 1.0);

        return new RuneEffect(
            Rune: group.Rune,
            Kind: RuneEffects.GetEffectKind(group.Rune),
            Tier: tier,
            MatchedRunesCount: group.Size,
            ChainNumber: chainNumber,
            IsMassEffect: isMass,
            ChargesHero: tier != RuneMatchTier.Match3,
            CreatesGreatRune: tier == RuneMatchTier.Match5,
            IsGreatRuneActivation: activatesGreatRune,
            CommanderEnergy: isMass ? RuneEffects.TShapeCommanderEnergy : 0,
            Power: baseMatchPower * multiplier
        );
    }

    /// <summary>Resolves every rune match currently present on a board for the given chain number.</summary>
    public static IReadOnlyList<RuneEffect> ResolveStep(Match3Board board, int chainNumber, bool greatRuneActivated = false)
    {
        if (board is null)
        {
            throw new ArgumentNullException(nameof(board));
        }

        return board
            .FindMatchGroups()
            .Select(group => Resolve(group, chainNumber, greatRuneActivated))
            .ToList();
    }
}
}
