using UnityEngine;
using WooAsset;

namespace AOT
{
    public class AOTAssetsSetting : AssetsSetting
    {
        public override string GetUrlByBundleName(string buildTarget, string bundleName)
        {
            return base.GetUrlByBundleName(buildTarget, bundleName) + ".bytes";
        }
        public override string GetUrlByBundleName(string buildTarget, string version, string bundleName)
        {
            return GetUrlByBundleName(buildTarget, bundleName);
        }
        protected override string GetBaseUrl()
        {
            return Application.streamingAssetsPath;
            return null;
            //return AOTDefine.G.CDN;
            return "https://webpkgs.oss-cn-shanghai.aliyuncs.com/DreamElevator/Server";
            return "http://127.0.0.1:8080";
            return "http://192.168.1.4:8080/webgl/Server";
        }
        public override bool GetBundleAlwaysFromWebRequest()
        {
            return true;
        }

        public override IAssetLife GetAssetLife()
        {
            return null;
        }
        public override bool GetCachesDownloadedBundles()
        {
            return true;
        }
        //public override bool CheckVersionByVersionCollection() => false;
    }


}
