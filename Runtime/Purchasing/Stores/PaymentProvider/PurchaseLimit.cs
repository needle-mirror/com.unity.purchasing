namespace UnityEngine.Purchasing
{
    /// <summary>
    /// The UTC calendar period a <see cref="PurchaseLimit"/> applies to.
    /// </summary>
    public enum PurchaseLimitPeriod
    {
        /// <summary>
        /// Resets at the top of each hour, UTC.
        /// </summary>
        Hour,

        /// <summary>
        /// Resets at 00:00 UTC each day.
        /// </summary>
        Day,

        /// <summary>
        /// Resets on the 1st of each month at 00:00 UTC.
        /// </summary>
        Month
    }

    /// <summary>
    /// How many times one player can buy a catalog listing per period.
    /// </summary>
    public class PurchaseLimit
    {
        /// <summary>
        /// The maximum number of purchases one player can make per period.
        /// </summary>
        public int count { get; }

        /// <summary>
        /// The period the count applies to.
        /// </summary>
        public PurchaseLimitPeriod period { get; }

        internal PurchaseLimit(int count, PurchaseLimitPeriod period)
        {
            this.count = count;
            this.period = period;
        }
    }
}
