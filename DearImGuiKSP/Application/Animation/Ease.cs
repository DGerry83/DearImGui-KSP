namespace DearImGuiKSP
{
    /// <summary>
    /// Easing curves for <c>Tween.To</c> (spec §4.3). The eased value is evaluated
    /// over t in [0,1] as the tween progresses; every curve is exact at both
    /// endpoints (t = 0 yields <c>from</c>, t = 1 yields <c>to</c>).
    /// </summary>
    public enum Ease
    {
        /// <summary>Constant rate; the eased value is t itself.</summary>
        Linear,

        /// <summary>Quadratic acceleration; t squared.</summary>
        QuadIn,

        /// <summary>Quadratic deceleration; the mirror of <see cref="QuadIn"/>.</summary>
        QuadOut,

        /// <summary>Quadratic acceleration into deceleration; half-scale in plus half-scale out.</summary>
        QuadInOut,

        /// <summary>Cubic acceleration; t cubed.</summary>
        CubicIn,

        /// <summary>Cubic deceleration; the mirror of <see cref="CubicIn"/>.</summary>
        CubicOut,

        /// <summary>Cubic acceleration into deceleration; half-scale in plus half-scale out.</summary>
        CubicInOut,
    }

    /// <summary>
    /// Pure easing functions behind <see cref="Ease"/> (C14). Stateless, allocation-free;
    /// each curve is exact at t = 0 and t = 1.
    /// </summary>
    internal static class EaseFunctions
    {
        internal static float Evaluate(Ease ease, float t)
        {
            switch (ease)
            {
                case Ease.QuadIn:
                    return t * t;
                case Ease.QuadOut:
                    return t * (2f - t);
                case Ease.QuadInOut:
                    return t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
                case Ease.CubicIn:
                    return t * t * t;
                case Ease.CubicOut:
                {
                    float u = t - 1f;
                    return u * u * u + 1f;
                }
                case Ease.CubicInOut:
                {
                    if (t < 0.5f)
                    {
                        return 4f * t * t * t;
                    }
                    float u = 2f * t - 2f;
                    return u * u * u * 0.5f + 1f;
                }
                default:
                    return t;
            }
        }
    }
}
