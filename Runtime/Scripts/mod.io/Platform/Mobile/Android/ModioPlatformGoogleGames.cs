﻿using System;

#if UNITY_ANDROID && !MODIO_OCULUS && !IGNORE_MODIO
using GooglePlayGames;
using UnityEngine;
#endif

namespace ModIO.Implementation.Platform
{
    public class ModioPlatformGoogleGames : ModioPlatform, IModioSsoPlatform
    {
        public static void SetAsPlatform()
        {
            ActivePlatform = new ModioPlatformGoogleGames();
        }

        public void PerformSso(TermsHash? displayedTerms, Action<Result> onComplete, string optionalThirdPartyEmailAddressUsedForAuthentication = null)
        {
#if UNITY_ANDROID && !MODIO_OCULUS && !IGNORE_MODIO
            GetIdToken(token =>
            {
                ModIOUnity.AuthenticateUserViaGoogle(token,
                    optionalThirdPartyEmailAddressUsedForAuthentication,
                    displayedTerms,
                    onComplete);
            });
#endif
        }

#if UNITY_ANDROID && !MODIO_OCULUS && !IGNORE_MODIO
        void GetIdToken(Action<string> onReceivedAuthCode)
        {
            if (Application.isEditor)
                return;

            try
            {
                // Request server-side auth code from Play Games Platform
                var idToken = PlayGamesPlatform.Instance.GetIdToken();
                Logger.Log(LogLevel.Verbose, $"Id Token: {idToken}");
                onReceivedAuthCode?.Invoke(idToken);
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error, e.Message);
            }
        }
#endif
    }
}
