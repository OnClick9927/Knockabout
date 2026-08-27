using IFramework;
using System;
using System.Collections.Generic;
using UnityEngine;
using WooAsset;
namespace AOT
{
    class AOTGame : Game, IFramework.ILogger
    {
        void IFramework.ILogger.Error(string messages, params object[] paras) => UnityEngine.Debug.LogErrorFormat(messages, paras);
        void IFramework.ILogger.Exception(Exception ex) => UnityEngine.Debug.LogException(ex);
        void IFramework.ILogger.Log(string messages, params object[] paras) => UnityEngine.Debug.LogFormat(messages, paras);
        void IFramework.ILogger.Warn(string messages, params object[] paras) => UnityEngine.Debug.LogWarningFormat(messages, paras);
        void IFramework.ILogger.Assert(bool condition, string messages, params object[] paras) => UnityEngine.Debug.AssertFormat(condition, messages, paras);
        [SerializeField] private AOTDefine set;
        [SerializeField] private AssetReference<Game> GGame;
        protected override void Startup()
        {

            AOTDefine.G = set;
            Application.targetFrameRate = 60;
#if !UNITY_EDITOR
            Log.logger = this;
#endif
            Log.enable = true;
            AssetsHelper.log_Enable = Log.enable_L = AOTDefine.G.logType.HasFlag(AOTDefine.LogType.Log);
            AssetsHelper.warn_Enable = Log.enable_W = AOTDefine.G.logType.HasFlag(AOTDefine.LogType.Warn);
            AssetsHelper.err_Enable = Log.enable_E = AOTDefine.G.logType.HasFlag(AOTDefine.LogType.Err);
            Assets.SetAssetsSetting(new AOTAssetsSetting());


            Run();
        }
        private void LoadMetadataForAOTAssemblies(AssetsGroupOperation operation)
        {
            IReadOnlyList<string> aotDllList = AOTGenericReferences.PatchedAOTAssemblyList;
            foreach (var aotDllName in aotDllList)
            {
#if !UNITY_EDITOR
                string path = $"{AOTDefine.ASBDir}/{aotDllName}.bytes";
                var asset = operation.FindAsset(path) as RawAsset;
                var raw = asset.GetAsset();
                byte[] dllBytes = raw.bytes;
                HybridCLR.LoadImageErrorCode err = HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, HybridCLR.HomologousImageMode.SuperSet);
                UnityEngine.Debug.Log($"LoadMetadataForAOTAssembly:{aotDllName}. ret:{err}");
#endif
            }
            // Editor环境下，HotUpdate.dll.bytes已经被自动加载，不需要加载，重复加载反而会出问题。
#if !UNITY_EDITOR
            {
                string path = $"{AOTDefine.ASBDir}/Assembly-CSharp.dll.bytes";
                var asset = operation.FindAsset(path) as RawAsset;
                var raw = asset.GetAsset();
                byte[] dllBytes = raw.bytes;
                System.Reflection.Assembly.Load(dllBytes);
            }
#endif
        }

        private async void Run()
        {
            await Assets.InitAsync();
            var prepareOperation = await Assets.PrepareAssetsByTag(AOTDefine.HotAssemblyTag);
            LoadMetadataForAOTAssemblies(prepareOperation);
            prepareOperation.Release();
            await GGame.Instantiate(null);
        }
        protected override void OnQuit()
        {
            Log.L("退出AOT");
        }
    }


}
