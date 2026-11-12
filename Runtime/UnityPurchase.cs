using System;
using System.Collections.Generic;
using com.ktgame.iap.core;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using SubscriptionInfo = com.ktgame.iap.core.SubscriptionInfo;
#if UNITY_2020_3_OR_NEWER
using Unity.Services.Core;
using Unity.Services.Core.Environments;
#endif

namespace com.ktgame.iap.unity
{
    public class UnityPurchase : IPurchase, IDetailedStoreListener
    {
        public event Action<PurchaseInitialize> PurchaseInitialized;
        public event Action<PurchaseError> PurchaseFailed;
        public event Action<PurchaseComplete> PurchaseCompleted;
        public event Action<PurchaseProcess> PurchaseProcessing;
        public event Action<PurchaseComplete> ServerPurchaseValid;
        public event Action<PurchaseRestore> RestoreCompleted;

        public bool IsInitialized => _storeController != null && _storeExtensionProvider != null;
        public IEnumerable<IProduct> Products => _products;
        public IPurchaseValidator LocalValidator => _localValidator;
        public IPurchaseValidator ServerValidator => _serverValidator;

        private IStoreController _storeController;
        private IExtensionProvider _storeExtensionProvider;
        private readonly IPurchaseValidator _localValidator;
        private readonly IPurchaseValidator _serverValidator;
        private readonly List<UnityProduct> _products;
        private bool _initializing;

        public UnityPurchase(IPurchaseValidator localValidator)
        {
            _localValidator = localValidator;
            _products = new List<UnityProduct>();
        }

        public UnityPurchase(IPurchaseValidator localValidator, IPurchaseValidator serverValidator) : this(localValidator)
        {
            _serverValidator = serverValidator;
        }

        public void InitializePurchasing(IEnumerable<ProductData> productData)
        {
            if (IsInitialized || _initializing)
            {
                Debug.Log($"[{nameof(UnityPurchase)}] InitializePurchasing ignored: already {(IsInitialized ? "initialized" : "initializing")}.");
                return;
            }

            _initializing = true;
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            foreach (var product in productData)
            {
                builder.AddProduct(product.Id, product.Type.ToUnityProductType());
            }

            UnityPurchasing.Initialize(this, builder);
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _initializing = false;
            _storeController = controller;
            _storeExtensionProvider = extensions;

            _products.Clear();
            foreach (var product in _storeController.products.all)
            {
                _products.Add(new UnityProduct(_localValidator, product));
            }

            PurchaseInitialized?.Invoke(new PurchaseInitialize(InitializationStatus.Ready));
            Debug.Log($"[{nameof(UnityPurchase)}] OnInitialized");
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            _initializing = false;
            PurchaseInitialized?.Invoke(new PurchaseInitialize(InitializationStatus.NotReady));
            Debug.Log($"[{nameof(UnityPurchase)}] OnInitializeFailed FailureReason: {error}");
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            _initializing = false;
            PurchaseInitialized?.Invoke(new PurchaseInitialize(InitializationStatus.NotReady));
            Debug.Log($"[{nameof(UnityPurchase)}] OnInitializeFailed FailureReason: {error}, Message: {message}");
        }

        public void Purchase(string productId)
        {
            if (!IsInitialized)
            {
                Debug.Log($"[{nameof(UnityPurchase)}] Purchase {productId}: not initialized.");
                RaiseFailed(productId, PurchaseStatus.PurchasingUnavailable, "Store not initialized");
                return;
            }

            var product = _storeController.products.WithID(productId);
            if (product == null || !product.availableToPurchase)
            {
                Debug.Log($"[{nameof(UnityPurchase)}] Purchase {productId}: product unavailable.");
                RaiseFailed(productId, PurchaseStatus.ProductUnavailable, product == null ? "Unknown product" : "Product not available to purchase");
                return;
            }

            Debug.Log($"[{nameof(UnityPurchase)}] Purchase {productId}: PROCESSING.");
            PurchaseProcessing?.Invoke(new PurchaseProcess(product.definition.id));
            _storeController.InitiatePurchase(product);
        }

        public void RestorePurchases()
        {
            if (!IsInitialized)
            {
                Debug.Log($"[{nameof(UnityPurchase)}] RestorePurchases: not initialized.");
                RaiseRestoreCompleted(false, "Store not initialized");
                return;
            }

            switch (Application.platform)
            {
                case RuntimePlatform.IPhonePlayer:
                case RuntimePlatform.OSXPlayer:
                    _storeExtensionProvider.GetExtension<IAppleExtensions>().RestoreTransactions(OnRestoreResult);
                    break;
                case RuntimePlatform.Android:
                    _storeExtensionProvider.GetExtension<IGooglePlayStoreExtensions>().RestoreTransactions(OnRestoreResult);
                    break;
                default:
                    Debug.Log($"[{nameof(UnityPurchase)}] RestorePurchases not supported on {Application.platform}.");
                    RaiseRestoreCompleted(false, $"Not supported on {Application.platform}");
                    break;
            }
        }

        private void OnRestoreResult(bool success, string message)
        {
            Debug.Log($"[{nameof(UnityPurchase)}] RestorePurchases {(success ? "SUCCESS" : "FAILED")}. {message}");
            RaiseRestoreCompleted(success, message);
        }

        public SubscriptionInfo GetSubscriptionInfo(string productId)
        {
            if (!IsInitialized)
            {
                Debug.Log($"[{nameof(UnityPurchase)}] GetSubscriptionInfo: not initialized.");
                return null;
            }

            var product = _storeController.products.WithID(productId);
            if (product != null && product.definition.type == ProductType.Subscription)
            {
                var validState = _localValidator.Validate(product.definition.id, product.definition.type.ToString(), product.receipt);
                if (validState == PurchaseState.Purchased)
                {
                    var meta = product.metadata.GetGoogleProductMetadata();
                    var subscriptionManager = new SubscriptionManager(product, meta?.originalJson);
                    var subscriptionInfo = subscriptionManager.getSubscriptionInfo();
                    return subscriptionInfo.ToLocalSubscriptionInfo();
                }
            }

            return null;
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseArgs)
        {
            var product = purchaseArgs.purchasedProduct;
            var productId = product.definition.id;

            Debug.Log($"[{nameof(UnityPurchase)}] Purchase {productId} Process with receipt {product.receipt}");

            var purchaseState = PurchaseState.Purchased;
            if (_localValidator != null)
            {
                purchaseState = _localValidator.Validate(productId, product.definition.type.ToString(), product.receipt);
            }

            if (purchaseState == PurchaseState.Purchased && _serverValidator != null)
            {
                _serverValidator.Validate(productId, product.definition.type.ToString(), product.receipt);
            }

            Debug.Log($"[{nameof(UnityPurchase)}] Purchase {productId} Process with state {purchaseState}");

            if (purchaseState == PurchaseState.Purchased)
            {
                var purchase = new PurchaseComplete(productId, product.receipt, product.transactionID, product.appleProductIsRestored);
                PurchaseCompleted?.Invoke(purchase);
            }
            else
            {
                RaiseFailed(productId, PurchaseStatus.SignatureInvalid, $"Local validation returned {purchaseState}");
            }

            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            Debug.Log($"[{nameof(UnityPurchase)}] OnPurchaseFailed {product?.definition?.id}: {failureDescription.reason}. {failureDescription.message}");
            RaiseFailed(product?.definition?.id, ToPurchaseStatus(failureDescription.reason), failureDescription.message);
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            Debug.Log($"[{nameof(UnityPurchase)}] OnPurchaseFailed {product?.definition?.id}: {failureReason}");
            RaiseFailed(product?.definition?.id, ToPurchaseStatus(failureReason), null);
        }

        private static PurchaseStatus ToPurchaseStatus(PurchaseFailureReason failureReason)
        {
            switch (failureReason)
            {
                case PurchaseFailureReason.PurchasingUnavailable:
                    return PurchaseStatus.PurchasingUnavailable;
                case PurchaseFailureReason.ExistingPurchasePending:
                    return PurchaseStatus.ExistingPurchasePending;
                case PurchaseFailureReason.ProductUnavailable:
                    return PurchaseStatus.ProductUnavailable;
                case PurchaseFailureReason.SignatureInvalid:
                    return PurchaseStatus.SignatureInvalid;
                case PurchaseFailureReason.UserCancelled:
                    return PurchaseStatus.UserCancelled;
                case PurchaseFailureReason.PaymentDeclined:
                    return PurchaseStatus.PaymentDeclined;
                case PurchaseFailureReason.DuplicateTransaction:
                    return PurchaseStatus.DuplicateTransaction;
            }

            // Các giá trị chỉ có ở Unity IAP 5 so sánh theo tên để vẫn compile được với Unity IAP 4.
            switch (failureReason.ToString())
            {
                case "ValidationFailure":
                    return PurchaseStatus.ReceiptValidationFailed;
                case "StoreNotConnected":
                    return PurchaseStatus.PurchasingUnavailable;
                case "UserNotAuthenticated":
                    return PurchaseStatus.Unauthorized;
                case "NotSupported":
                    return PurchaseStatus.UnsupportedStore;
                case "OrderCancelled":
                    return PurchaseStatus.UserCancelled;
                default:
                    return PurchaseStatus.Unknown;
            }
        }

        private void RaiseFailed(string productId, PurchaseStatus status, string message)
        {
            PurchaseFailed?.Invoke(new PurchaseError(productId, status, message));
        }

        private void RaiseRestoreCompleted(bool success, string message)
        {
            RestoreCompleted?.Invoke(new PurchaseRestore(success, message));
        }
    }
}
