using UnityEngine;
using Unity.Services.LevelPlay;
using System.Collections;
using JigsawFun.Ads;

public class LevelPlayIntegrationTester : MonoBehaviour
{
    public void ValidateIntegration()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        LevelPlay.ValidateIntegration();
#else
        Debug.Log("LevelPlay integration validation is enabled only in Editor or Development builds.");
#endif
    }

    public void ShowInterstitialByHint()
    {
        TryShowRewarded("hint_reward");
    }

    public void ShowInterstitialOnLevelComplete()
    {
        TryShowInterstitial("Level_Complete");
    }

    private void TryShowInterstitial(string placement)
    {
        var ad = AdManager.Instance != null ? AdManager.Instance : FindObjectOfType<AdManager>(true);
        if (ad == null) return;
        ad.EnsureInitialized();
        if (ad.InterstitialHandler != null)
        {
            if (ad.InterstitialHandler.CanShowAd())
            {
                ad.InterstitialHandler.ShowAd(placement);
            }
            else
            {
                ad.InterstitialHandler.LoadAd();
            }
        }
    }

    private void TryShowRewarded(string placement)
    {
        var ad = AdManager.Instance != null ? AdManager.Instance : FindObjectOfType<AdManager>(true);
        if (ad == null) return;
        ad.EnsureInitialized();
        if (ad.RewardedHandler != null)
        {
            if (ad.RewardedHandler.CanShowAd())
            {
                ad.RewardedHandler.ShowAd(placement);
            }
            else
            {
                ad.RewardedHandler.LoadAd();
            }
        }
    }
}
