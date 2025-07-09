using com.ktgame.iap.core;
using UnityEngine;
using UnityEngine.Purchasing;

namespace com.ktgame.iap.unity
{
    public class UnityProduct : IProduct
    {
        public string Id => _product.definition.id;
        public PurchaseType Type => _product.definition.type.ToLocalProductType();
        public decimal LocalizedPrice => _product.metadata.localizedPrice;
        public string IsoCurrencyCode => _product.metadata.isoCurrencyCode;
        public string LocalizedPriceString => _product.metadata.localizedPriceString;
        public string LocalizedTitle => _product.metadata.localizedTitle;
        public string LocalizedDescription => _product.metadata.localizedDescription;
        public bool HasReceipt => _product.hasReceipt;
        public string Receipt => _product.receipt;
        public bool IsOwnedNonConsumable
        {
            get
            {
                if (_product == null || _product.definition == null)
                {
                    return false;
                }

                if (_product.definition.type == ProductType.NonConsumable)
                {
                    return _product.hasReceipt;
                }

                return false;
            }
        }
        public bool IsSubscribed
        {
            get
            {
                if (_product == null || _product.definition == null)
                {
                    return false;
                }

                if (_product.definition.type == ProductType.Subscription)
                {
                    var validState = _validator.Validate(_product.definition.id, _product.definition.type.ToString(), _product.receipt);
                    if (validState == PurchaseState.Purchased)
                    {
                        var meta = _product.metadata.GetGoogleProductMetadata();
                        var subscriptionManager = new SubscriptionManager(_product, meta?.originalJson);
                        var subscriptionInfo = subscriptionManager.getSubscriptionInfo();
                        return subscriptionInfo.isSubscribed() == Result.True;
                    }
                }

                return false;
            }
        }

        private readonly IPurchaseValidator _validator;
        private readonly Product _product;

        public UnityProduct(IPurchaseValidator validator, Product product)
        {
            _validator = validator;
            _product = product;
            Debug.Log(ToString());
        }

        public sealed override string ToString()
        {
            return
                $"--------------------------------\n Id: {Id}\n Type: {Type}\n LocalizedPrice: {LocalizedPrice}\n IsoCurrencyCode: {IsoCurrencyCode}\n LocalizedPriceString: {LocalizedPriceString}\n --------------------------------\n";
        }
    }
}