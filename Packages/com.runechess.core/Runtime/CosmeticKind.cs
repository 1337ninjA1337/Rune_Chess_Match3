namespace RuneChess.Core
{
    /// <summary>
    /// The cosmetic family a metaprogression reward belongs to (GDD "Метапрогрессия":
    /// косметику, визуальные эффекты рун). Cosmetics are purely visual: a kind only tells
    /// the presentation layer which surface to reskin, never a combat stat. Keeping it an
    /// explicit enum lets the non-pay-to-win invariant be smoke-tested (no kind carries power).
    /// </summary>
    public enum CosmeticKind
    {
        /// <summary>A reskin of the match-3 board surface (GDD monetization "скины доски").</summary>
        BoardSkin,

        /// <summary>A visual effect for rune matches (GDD "визуальные эффекты рун").</summary>
        RuneEffect,

        /// <summary>A reskin of a hero portrait/banner (GDD monetization "скины героев").</summary>
        HeroSkin
    }
}
