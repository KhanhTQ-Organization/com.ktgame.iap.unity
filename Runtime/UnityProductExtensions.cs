using com.ktgame.iap.core;
using UnityEngine.Purchasing;
using SubscriptionInfo = com.ktgame.iap.core.SubscriptionInfo;

namespace com.ktgame.iap.unity
{
    public static class UnityProductExtensions
    {
        public static ProductType ToUnityProductType(this PurchaseType type)
        {
            switch (type)
            {
                case PurchaseType.Consumable:
                    return ProductType.Consumable;
                case PurchaseType.NonConsumable:
                    return ProductType.NonConsumable;
                case PurchaseType.Subscription:
                    return ProductType.Subscription;
                default:
                    return ProductType.Consumable;
            }
        }

        public static PurchaseType ToLocalProductType(this ProductType type)
        {
            switch (type)
            {
                case ProductType.Consumable:
                    return PurchaseType.Consumable;
                case ProductType.NonConsumable:
                    return PurchaseType.NonConsumable;
                case ProductType.Subscription:
                    return PurchaseType.Subscription;
                default:
                    return PurchaseType.Consumable;
            }
        }

        public static SubscriptionInfo ToLocalSubscriptionInfo(this UnityEngine.Purchasing.SubscriptionInfo info)
        {
            return new SubscriptionInfo(
                info.isSubscribed() == Result.True, info.isExpired() == Result.True, info.isCancelled() == Result.True,
                info.isFreeTrial() == Result.True, info.isAutoRenewing() == Result.True, info.isIntroductoryPricePeriod() == Result.True,
                info.getPurchaseDate(), info.getExpireDate(), info.getCancelDate(), info.getRemainingTime(), info.getIntroductoryPrice(),
                info.getIntroductoryPricePeriod(), info.getIntroductoryPricePeriodCycles(), info.getFreeTrialPeriod(), info.getSubscriptionPeriod()
            );
        }
    }
}