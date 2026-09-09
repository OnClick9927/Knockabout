using UnityEngine;
using WooAsset;
using System;
using System.IO;

namespace AOT
{
    public class AOTAssetsSetting : AssetsSetting
    {
        public override string GetUrlByBundleName(string buildTarget, string bundleName)
        {
            return new Uri(Path.Combine(Application.streamingAssetsPath, buildTarget, bundleName + ".bytes")).AbsoluteUri;
        }
        public override string GetUrlByBundleName(string buildTarget, string version, string bundleName)
        {
            return GetUrlByBundleName(buildTarget, bundleName);
        }
        protected override string GetBaseUrl()
        {
            return Application.streamingAssetsPath;
        }
        public override bool GetBundleAlwaysFromWebRequest()
        {
            return false;
        }

        public override IAssetLife GetAssetLife()
        {
            return null;
        }
        public override bool GetCachesDownloadedBundles()
        {
            return false;
        }
        public override bool NeedCopyStreamBundles() => false;
        public override bool GetSaveBytesWhenPlaying() => false;
        public override string GetBundleLocalPath(string bundlePath)
            => Path.Combine(AssetsHelper.StreamBundlePath, Path.GetFileName(bundlePath) + ".bytes");
    }


}
