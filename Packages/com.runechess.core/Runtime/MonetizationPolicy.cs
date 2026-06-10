using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RuneChess.Core
{
    /// <summary>
    /// The enforceable non-pay-to-win policy (GDD "Метапрогрессия"/"Монетизация": "Баланс
    /// должен избегать pay-to-win. Монетизация не должна давать прямое преимущество в PvP").
    /// Where <see cref="MonetizationCatalog"/> holds the design as data, this type turns the
    /// four prohibitions into guards that smoke checks can assert, and gives the cosmetic-only
    /// invariant teeth: a monetization format must expose no numeric (stat/power) field, so it
    /// can never sell combat advantage. See docs/monetization.md.
    /// </summary>
    public static class MonetizationPolicy
    {
        /// <summary>The acceptable monetization kinds (GDD "Подходящие форматы").</summary>
        public static IReadOnlyList<MonetizationFormatKind> AcceptableKinds { get; } =
            Array.AsReadOnly(MonetizationCatalog.AcceptableFormats.Select(format => format.Kind).ToArray());

        /// <summary>True when a kind is one the catalog actually offers.</summary>
        public static bool IsAcceptable(MonetizationFormatKind kind) => AcceptableKinds.Contains(kind);

        /// <summary>
        /// The game forbids every prohibited format unconditionally (GDD "Нежелательные
        /// форматы"). There is no acceptable form of these, so the guard always reports true.
        /// </summary>
        public static bool IsProhibited(MonetizationProhibitionKind prohibition) =>
            MonetizationCatalog.Prohibitions.Contains(prohibition);

        /// <summary>
        /// True when a format is cosmetic-only: its data type exposes no numeric field that
        /// could encode hero power, gold, or any combat stat. This is the structural guarantee
        /// that an acceptable format cannot be turned into a pay-to-win offer.
        /// </summary>
        public static bool IsCosmeticOnly(MonetizationFormat format)
        {
            if (format is null)
            {
                throw new ArgumentNullException(nameof(format));
            }

            return format.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .All(property => !IsNumeric(property.PropertyType));
        }

        /// <summary>Throw if a format is not cosmetic-only — a hard guard against power creep.</summary>
        public static void EnsureCosmeticOnly(MonetizationFormat format)
        {
            if (!IsCosmeticOnly(format))
            {
                throw new InvalidOperationException(
                    $"Monetization format '{format.Id}' exposes a numeric field and would be pay-to-win.");
            }
        }

        /// <summary>
        /// True when the whole catalog honours the policy: every acceptable format is
        /// cosmetic-only and all four prohibitions are recognised as forbidden. The single
        /// invariant a presentation/store layer can assert before showing any offer.
        /// </summary>
        public static bool CatalogHonoursPolicy() =>
            MonetizationCatalog.AcceptableFormats.All(IsCosmeticOnly)
            && Enum.GetValues(typeof(MonetizationProhibitionKind))
                .Cast<MonetizationProhibitionKind>()
                .All(IsProhibited);

        private static bool IsNumeric(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type) ?? type;
            return underlying == typeof(int)
                || underlying == typeof(long)
                || underlying == typeof(short)
                || underlying == typeof(byte)
                || underlying == typeof(float)
                || underlying == typeof(double)
                || underlying == typeof(decimal);
        }
    }
}
