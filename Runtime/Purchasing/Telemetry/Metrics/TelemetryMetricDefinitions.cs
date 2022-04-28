namespace UnityEngine.Purchasing.Telemetry
{
    static class TelemetryMetricDefinitions
    {
        internal static readonly TelemetryMetricDefinition
            confirmSubscriptionPriceChangeName = "confirm_subscription_price_change",
            continuePromotionalPurchasesName = "continue_promotional_purchases",
            dequeueQueryProductsTimeName = "dequeue_query_products_time",
            dequeueQueryPurchasesTimeName = "dequeue_query_purchases_time",
            fetchStorePromotionOrderName = "fetch_store_promotion_order",
            fetchStorePromotionVisibilityName = "fetch_store_promotion_visibility",
            initPurchaseName = "init_purchase",
            packageInitTimeName = "package_init_time",
            presentCodeRedemptionSheetName = "present_code_redemption_sheet",
            refreshAppReceiptName = "refresh_app_receipt",
            restoreTransactionName = "restore_transaction",
            retrieveProductsName = "retrieve_products",
            setStorePromotionOrderName = "set_store_promotion_order",
            setStorePromotionVisibilityName = "set_store_promotion_visibility",
            upgradeDowngradeSubscriptionName = "upgrade_downgrade_subscription";
    }
}
