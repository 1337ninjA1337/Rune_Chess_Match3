using System;

namespace RuneChess.Core;

/// <summary>
/// Simple, readable MVP combat formulas from the GDD "Формулы боя" section.
/// All magnitudes are doubles so balance values stay easy to tune; the formulas
/// are pure functions with explicit constants and no hidden state.
/// </summary>
public static class CombatFormulas
{
    // Star scaling.
    public const double OneStarMultiplier = 1.0;
    public const double TwoStarMultiplier = 2.0;
    public const double ThreeStarMultiplier = 4.0;

    // Defense softening constant shared by armor and magic resist.
    public const double DefenseConstant = 100.0;

    // Attack speed clamp (attacks per second).
    public const double MinAttacksPerSecond = 0.4;
    public const double MaxAttacksPerSecond = 3.0;

    // Mana gain.
    public const double ManaFromAttack = 10.0;
    public const double MaxManaFromDamageTaken = 20.0;
    public const double DamageTakenManaScale = 50.0;
    public const double ManaPerBlueRune = 8.0;

    // Critical strikes.
    public const double BaseCritChance = 0.05;
    public const double BaseCritMultiplier = 1.5;

    // Shields.
    public const double MaxShieldFraction = 0.6;

    /// <summary>Star multiplier for hero health and stats: 1★ x1.0, 2★ x2.0, 3★ x4.0.</summary>
    public static double GetStarMultiplier(int stars)
    {
        return stars switch
        {
            1 => OneStarMultiplier,
            2 => TwoStarMultiplier,
            3 => ThreeStarMultiplier,
            _ => throw new ArgumentOutOfRangeException(nameof(stars), stars, "Heroes have one to three stars.")
        };
    }

    /// <summary>finalHealth = baseHealth * starMultiplier * synergyMultiplier * artifactMultiplier.</summary>
    public static double CalculateFinalHealth(
        double baseHealth,
        double starMultiplier,
        double synergyMultiplier = 1.0,
        double artifactMultiplier = 1.0)
    {
        RequireNonNegative(baseHealth, nameof(baseHealth));
        RequireNonNegative(starMultiplier, nameof(starMultiplier));
        RequireNonNegative(synergyMultiplier, nameof(synergyMultiplier));
        RequireNonNegative(artifactMultiplier, nameof(artifactMultiplier));

        return baseHealth * starMultiplier * synergyMultiplier * artifactMultiplier;
    }

    /// <summary>Fraction of damage removed by a defensive stat: defense / (defense + 100).</summary>
    public static double DamageReduction(double defense)
    {
        RequireNonNegative(defense, nameof(defense));
        return defense / (defense + DefenseConstant);
    }

    /// <summary>finalPhysicalDamage = rawDamage * (1 - armorReduction).</summary>
    public static double CalculatePhysicalDamage(double rawDamage, double armor)
    {
        RequireNonNegative(rawDamage, nameof(rawDamage));
        return rawDamage * (1.0 - DamageReduction(armor));
    }

    /// <summary>finalMagicDamage = rawMagicDamage * (1 - resistReduction).</summary>
    public static double CalculateMagicDamage(double rawMagicDamage, double magicResist)
    {
        RequireNonNegative(rawMagicDamage, nameof(rawMagicDamage));
        return rawMagicDamage * (1.0 - DamageReduction(magicResist));
    }

    /// <summary>
    /// attacksPerSecond = baseAttackSpeed * speedBonusMultiplier, clamped to the MVP range 0.4-3.0.
    /// </summary>
    public static double CalculateAttacksPerSecond(double baseAttackSpeed, double speedBonusMultiplier = 1.0)
    {
        RequireNonNegative(baseAttackSpeed, nameof(baseAttackSpeed));
        RequireNonNegative(speedBonusMultiplier, nameof(speedBonusMultiplier));

        var attacksPerSecond = baseAttackSpeed * speedBonusMultiplier;
        return Math.Clamp(attacksPerSecond, MinAttacksPerSecond, MaxAttacksPerSecond);
    }

    /// <summary>attackInterval = 1 / attacksPerSecond.</summary>
    public static double CalculateAttackInterval(double attacksPerSecond)
    {
        if (attacksPerSecond <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(attacksPerSecond), "Attacks per second must be positive.");
        }

        return 1.0 / attacksPerSecond;
    }

    /// <summary>manaFromDamageTaken = min(20, damageTaken / maxHealth * 50).</summary>
    public static double CalculateManaFromDamageTaken(double damageTaken, double maxHealth)
    {
        RequireNonNegative(damageTaken, nameof(damageTaken));
        if (maxHealth <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxHealth), "Max health must be positive.");
        }

        return Math.Min(MaxManaFromDamageTaken, damageTaken / maxHealth * DamageTakenManaScale);
    }

    /// <summary>manaFromBlueRunes = matchedBlueRunes * 8.</summary>
    public static double CalculateManaFromBlueRunes(int matchedBlueRunes)
    {
        if (matchedBlueRunes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(matchedBlueRunes), "Matched blue rune count cannot be negative.");
        }

        return matchedBlueRunes * ManaPerBlueRune;
    }

    /// <summary>A hero casts its ability once currentMana reaches manaMax.</summary>
    public static bool IsAbilityReady(double currentMana, double manaMax)
    {
        if (manaMax <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(manaMax), "Mana maximum must be positive.");
        }

        RequireNonNegative(currentMana, nameof(currentMana));
        return currentMana >= manaMax;
    }

    /// <summary>Resolves a crit roll against a chance in [0, 1). roll is expected in [0, 1).</summary>
    public static bool WouldCrit(double roll, double critChance = BaseCritChance)
    {
        if (roll < 0.0 || roll >= 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(roll), "Crit roll must be in [0, 1).");
        }

        if (critChance < 0.0 || critChance > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(critChance), "Crit chance must be in [0, 1].");
        }

        return roll < critChance;
    }

    /// <summary>finalDamage = damage * critDamageMultiplier when a hit crits.</summary>
    public static double ApplyCrit(double damage, double critMultiplier = BaseCritMultiplier)
    {
        RequireNonNegative(damage, nameof(damage));
        if (critMultiplier < 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(critMultiplier), "Crit multiplier cannot reduce damage.");
        }

        return damage * critMultiplier;
    }

    /// <summary>effectiveDamage = max(0, incomingDamage - currentShield): shield absorbs damage before health.</summary>
    public static double DamageAfterShield(double incomingDamage, double currentShield)
    {
        RequireNonNegative(incomingDamage, nameof(incomingDamage));
        RequireNonNegative(currentShield, nameof(currentShield));
        return Math.Max(0.0, incomingDamage - currentShield);
    }

    /// <summary>remainingShield = max(0, currentShield - incomingDamage).</summary>
    public static double ShieldAfterDamage(double currentShield, double incomingDamage)
    {
        RequireNonNegative(currentShield, nameof(currentShield));
        RequireNonNegative(incomingDamage, nameof(incomingDamage));
        return Math.Max(0.0, currentShield - incomingDamage);
    }

    /// <summary>Shields stack but are capped at maxShield = maxHealth * 0.6.</summary>
    public static double CapShield(double totalShield, double maxHealth)
    {
        RequireNonNegative(totalShield, nameof(totalShield));
        RequireNonNegative(maxHealth, nameof(maxHealth));
        return Math.Min(totalShield, maxHealth * MaxShieldFraction);
    }

    /// <summary>
    /// finalHealing = rawHealing * healingMultiplier, reduced by anti-healing in [0, 1].
    /// </summary>
    public static double CalculateFinalHealing(double rawHealing, double healingMultiplier = 1.0, double antiHealing = 0.0)
    {
        RequireNonNegative(rawHealing, nameof(rawHealing));
        RequireNonNegative(healingMultiplier, nameof(healingMultiplier));
        if (antiHealing < 0.0 || antiHealing > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(antiHealing), "Anti-healing must be in [0, 1].");
        }

        return rawHealing * healingMultiplier * (1.0 - antiHealing);
    }

    /// <summary>newHealth = min(maxHealth, currentHealth + finalHealing): healing never overfills.</summary>
    public static double ApplyHealing(double currentHealth, double finalHealing, double maxHealth)
    {
        RequireNonNegative(currentHealth, nameof(currentHealth));
        RequireNonNegative(finalHealing, nameof(finalHealing));
        if (maxHealth <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxHealth), "Max health must be positive.");
        }

        return Math.Min(maxHealth, currentHealth + finalHealing);
    }

    private static void RequireNonNegative(double value, string parameterName)
    {
        if (value < 0.0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Combat values cannot be negative.");
        }
    }
}
