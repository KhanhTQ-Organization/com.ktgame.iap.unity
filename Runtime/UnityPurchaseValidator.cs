using UnityEngine;
using com.ktgame.iap.core;
using UnityEngine.Purchasing.Security;

namespace com.ktgame.iap.unity
{
	public class UnityPurchaseValidator : IPurchaseValidator
	{
		private readonly CrossPlatformValidator _validator;

		public UnityPurchaseValidator(byte[] googlePublicKey, byte[] appleRootCert)
		{
#if UNITY_EDITOR
			_validator = null;
#else
			_validator = new CrossPlatformValidator(googlePublicKey,appleRootCert,Application.identifier);
#endif
		}

		public PurchaseState Validate(string productId, string productType, string receipt)
		{
			if (string.IsNullOrEmpty(receipt))
			{
				Debug.Log($"[{nameof(UnityPurchase)}] Receipt is NULL.");
				return PurchaseState.Canceled;
			}

#if UNITY_EDITOR
			Debug.Log($"[{nameof(UnityPurchase)}] Skip validation in Editor.");
			return PurchaseState.Purchased;
#else
    try
    {
        _validator.Validate(receipt);
        return PurchaseState.Purchased;
    }
    catch (IAPSecurityException)
    {
        Debug.Log($"[{nameof(UnityPurchase)}] Receipt is NOT valid.");
        return PurchaseState.Canceled;
    }
#endif
		}
	}
}