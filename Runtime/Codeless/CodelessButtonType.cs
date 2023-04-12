namespace UnityEngine.Purchasing
{
    /// <summary>
    /// The type of a <c>CodelessIAPButton</c>, can be either a purchase or a restore button.
    /// </summary>
    public enum CodelessButtonType
    {
        /// <summary>
        /// This button will display localized product title and price. Clicking will trigger a purchase.
        /// </summary>
        Purchase,
        /// <summary>
        /// This button will display a static string for restoring previously purchased non-consumable
        /// and subscriptions. Clicking will trigger this restoration process, on supported app stores.
        /// </summary>
        Restore
    }
}
