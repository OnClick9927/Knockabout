/*********************************************************************************
 *Author:         OnClick
 *Version:        0.1
 *UnityVersion:   2020.3.3f1c1
 *Date:           2023-04-22
*********************************************************************************/
using AOT;
using HybridCLR.Editor;
using HybridCLR.Editor.AOT;
using HybridCLR.Editor.Commands;
using HybridCLR.Editor.HotUpdate;
using HybridCLR.Editor.Settings;
using IFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.Rendering;
using WooAsset;
using static WooAsset.AssetsEditorTool;

partial class EditorGameTools
{
    // Unity's command-line method lookup does not resolve nested editor types.
    public static void BuildWindows() => AppBuild.BuildWindows();
    public static void BuildWindowsHotUpdate() => AppBuild.BuildHotUpdate();

    public static class AppBuild
    {
        private const string StageKey = "Knockabout.AppBuild.Stage";
        private const string KindKey = "Knockabout.AppBuild.Kind";
        private const string OutputKey = "Knockabout.AppBuild.Output";
        private const UnityEditor.BuildTarget WindowsTarget = UnityEditor.BuildTarget.StandaloneWindows64;
        private static string[] localBundleFiles;
        private enum BuildKind { Player, HotUpdate, Assemblies }

        [MenuItem("Tools/打包/Windows完整包")]
        public static void BuildWindows() => StartBuild(BuildKind.Player);

        [MenuItem("Tools/打包/Windows本地热更资源")]
        public static void BuildHotUpdate() => StartBuild(BuildKind.HotUpdate);

        private static void StartBuild(BuildKind kind)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetInt(StageKey, 0) != 0)
                throw new BuildFailedException("Stop Play Mode and wait for any previous build to finish.");
            string output = Path.GetFullPath("../Builds/Windows/Knockabout.exe");
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-desktopBuildOutput");
            if (index >= 0 && index + 1 < args.Length) output = Path.GetFullPath(args[index + 1]);
            SessionState.SetString(OutputKey, output);
            SessionState.SetInt(KindKey, (int)kind);
            SessionState.SetInt(StageKey, 1);
            ResumeAfterReload();
        }

        [InitializeOnLoadMethod]
        private static void ResumeAfterReload()
        {
            if (SessionState.GetInt(StageKey, 0) == 0) return;
            EditorApplication.update -= ContinueBuild;
            EditorApplication.update += ContinueBuild;
        }

        private static async void ContinueBuild()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            EditorApplication.update -= ContinueBuild;
            try
            {
                var kind = (BuildKind)SessionState.GetInt(KindKey, 0);
                switch (SessionState.GetInt(StageKey, 0))
                {
                    case 1:
                        Set();
                        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
                        PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Standalone, ApiCompatibilityLevel.NET_Unity_4_8);
                        PlayerSettings.SetUseDefaultGraphicsAPIs(WindowsTarget, false);
                        PlayerSettings.SetGraphicsAPIs(WindowsTarget, new[] { GraphicsDeviceType.Direct3D11 });
                        // Unity's flip-model swap chain cannot composite a transparent DWM window.
                        PlayerSettings.useFlipModelSwapchain = false;
                        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
                        PlayerSettings.defaultScreenWidth = 1920;
                        PlayerSettings.defaultScreenHeight = 1080;
                        PlayerSettings.resizableWindow = false;
                        PlayerSettings.allowFullscreenSwitch = false;
                        PlayerSettings.runInBackground = true;
                        PlayerSettings.productName = "Knockabout";
                        EditorUserBuildSettings.development = false;
                        EditorUserBuildSettings.buildScriptsOnly = false;
                        UnityEditor.WindowsStandalone.UserBuildSettings.createSolution = false;
                        SessionState.SetInt(StageKey, 2);
                        if (EditorUserBuildSettings.activeBuildTarget != WindowsTarget &&
                            !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, WindowsTarget))
                            throw new BuildFailedException("Could not select Windows x64.");
                        CompilationPipeline.RequestScriptCompilation();
                        return;
                    case 2:
                        if (kind == BuildKind.HotUpdate)
                        {
                            if (!Directory.Exists(SettingsUtil.GetAssembliesPostIl2CppStripDir(WindowsTarget) + old_copy))
                                throw new BuildFailedException("Build a complete Windows player before creating a hot update.");
                            CompileDllCommand.CompileDll(WindowsTarget, false);
                            if (CheckAccessMissingMetadata())
                                throw new BuildFailedException("The hot update requires a new AOT player. Build a complete Windows package.");
                        }
                        else
                        {
                            var installer = new HybridCLR.Editor.Installer.InstallerController();
                            if (!installer.HasInstalledHybridCLR()) installer.InstallDefaultHybridCLR();
                            PrebuildCommand.GenerateAll();
                        }
                        // Resume with the newly compiled AOTGenericReferences, never its previous list.
                        SessionState.SetInt(StageKey, 3);
                        CompilationPipeline.RequestScriptCompilation();
                        return;
                    case 3:
                        CopyAssemblyToProject(kind == BuildKind.HotUpdate);
                        if (kind != BuildKind.Assemblies)
                        {
                            var option = AssetsEditorTool.option;
                            option.buildIn.copyToStream = true;
                            option.buildIn.assets.Clear();
                            option.SetBuildInBundleSelectorType(typeof(LocalBuildInBundleSelector));
                            option.SetAssetBuildType(typeof(ABAssetBuild));
                            EditorUtility.SetDirty(option);
                            AssetDatabase.SaveAssets();
                            localBundleFiles = null;
                            var task = await AssetTaskRunner.Build();
                            if (task.isErr) throw new BuildFailedException(task.error.ToString());
                            ValidateStreamingAssets();
                            string output = SessionState.GetString(OutputKey, "");
                            if (kind == BuildKind.Player)
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(output));
                                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                                {
                                    scenes = new[] { "Assets/AOT/update.unity" },
                                    locationPathName = output,
                                    target = WindowsTarget,
                                    targetGroup = BuildTargetGroup.Standalone,
                                    options = BuildOptions.None,
                                });
                                if (report.summary.result != BuildResult.Succeeded)
                                    throw new BuildFailedException("Windows player build failed: " + report.summary.result);
                                CopyOldAOTAssembly();
                                Debug.Log("Windows player: " + output);
                            }
                            else
                            {
                                string target = Path.Combine(Path.GetDirectoryName(output), "HotUpdate", "StreamingAssets", AssetsEditorTool.BuildTargetName);
                                CopyFolder(AssetsHelper.StreamBundlePath, target);
                                Debug.Log("Local hot update: " + target);
                            }
                        }
                        FinishBuild(true);
                        return;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                FinishBuild(false);
            }
        }

        private static void FinishBuild(bool success)
        {
            SessionState.SetInt(StageKey, 0);
            Debug.Log(success ? "Desktop build completed." : "Desktop build failed.");
            if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1);
        }

        private static void ValidateStreamingAssets()
        {
            if (localBundleFiles == null || localBundleFiles.Length == 0)
                throw new BuildFailedException("WooAsset did not select any local bundles.");
            string directory = AssetsHelper.StreamBundlePath;
            foreach (string file in localBundleFiles)
            {
                string local = Path.Combine(directory, Path.GetFileName(file) + StreamBundlesData.fileExt);
                if (!File.Exists(local) || !File.ReadAllBytes(file).SequenceEqual(File.ReadAllBytes(local)))
                    throw new BuildFailedException("Missing or incomplete local bundle: " + local);
            }
        }

        public class LocalBuildInBundleSelector : IBuildInBundleSelector
        {
            public string[] Select(string[] files, List<string> buildInAssets, List<string> buildInConfig,
                ManifestData manifest, List<PackageExportData> exports)
            {
                foreach (string file in AOTGenericReferences.PatchedAOTAssemblyList.Concat(new[] { "Assembly-CSharp.dll" }))
                    if (manifest.GetAssetData(ProjectAsbDir + "/" + file + ".bytes") == null)
                        throw new BuildFailedException("Hot assembly is missing from the WooAsset manifest: " + file);
                localBundleFiles = files;
                return files;
            }
        }

        private static string ProjectAsbDir => AOTDefine.ASBDir;
        const string old_copy = "_old_copy";
        [MenuItem("Tools/打包/设置HyBirdCLR")]
        private static void Set()
        {

            HybridCLRSettings.Instance.hotUpdateAssemblies = new string[] { "Assembly-CSharp" };
            HybridCLRSettings.Instance.outputLinkFile = "AOT/link.xml";
            HybridCLRSettings.Instance.outputAOTGenericReferenceFile = "AOT/Scripts/AOTGenericReferences.cs";

            EditorUtility.SetDirty(HybridCLRSettings.Instance);

            AssetDatabase.SaveAssets();

        }
        [MenuItem("Tools/打包/制作程序集")]
        public static void Build() => StartBuild(BuildKind.Assemblies);

        static bool CheckAccessMissingMetadata()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            // aotDir指向 构建主包时生成的裁剪aot dll目录，而不是最新的SettingsUtil.GetAssembliesPostIl2CppStripDir(target)目录。
            // 一般来说，发布热更新包时，由于中间可能调用过generate/all，SettingsUtil.GetAssembliesPostIl2CppStripDir(target)目录中包含了最新的aot dll，
            // 肯定无法检查出类型或者函数裁剪的问题。
            // 需要在构建完主包后，将当时的aot dll保存下来，供后面补充元数据或者裁剪检查。
            string aotDir = $"{SettingsUtil.GetAssembliesPostIl2CppStripDir(target)}{old_copy}";
            if (!Directory.Exists(aotDir)) return false;
            // 第2个参数excludeDllNames为要排除的aot dll。一般取空列表即可。对于旗舰版本用户，
            // excludeDllNames需要为dhe程序集列表，因为dhe 程序集会进行热更新，热更新代码中
            // 引用的dhe程序集中的类型或函数肯定存在。
            var checker = new MissingMetadataChecker(aotDir, new List<string>());

            string hotUpdateDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
            foreach (var dll in SettingsUtil.HotUpdateAssemblyFilesExcludePreserved)
            {
                string dllPath = $"{hotUpdateDir}/{dll}";
                bool notAnyMissing = checker.Check(dllPath);
                if (!notAnyMissing)
                {
                    Debug.LogError("AOT 需要重新生成");
                    return true;
                }
            }
            Debug.Log("AOT 不需要重新生成");
            return false;
        }
        static void CopyOldAOTAssembly()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            string srcDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
            string destDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target) + old_copy;
            if (Directory.Exists(destDir))
                Directory.Delete(destDir, true);
            CopyFolder(srcDir, destDir);
            Debug.Log("备份完成");
        }
        /// <summary>
        /// 复制文件夹及文件
        /// </summary>
        /// <param name="sourceFolder">原文件路径</param>
        /// <param name="destFolder">目标文件路径</param>
        /// <returns></returns>
        private static int CopyFolder(string sourceFolder, string destFolder)
        {
            try
            {
                //如果目标路径不存在,则创建目标路径
                if (!Directory.Exists(destFolder))
                    Directory.CreateDirectory(destFolder);

                //得到原文件根目录下的所有文件
                string[] files = Directory.GetFiles(sourceFolder);
                foreach (string file in files)
                {
                    string name = Path.GetFileName(file);
                    string dest = Path.Combine(destFolder, name);
                    System.IO.File.Copy(file, dest, true);//复制文件
                }
                //得到原文件根目录下的所有文件夹
                string[] folders = System.IO.Directory.GetDirectories(sourceFolder);
                foreach (string folder in folders)
                {
                    string name = System.IO.Path.GetFileName(folder);
                    string dest = System.IO.Path.Combine(destFolder, name);
                    CopyFolder(folder, dest);//构建目标路径,递归复制文件
                }
                return 1;
            }
            catch (Exception ex)
            {
                throw new BuildFailedException($"Could not copy {sourceFolder} to {destFolder}: {ex.Message}");
            }

        }
        static void CopyAssemblyToProject(bool usePlayerBaseline = false)
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            string srcDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
            if (usePlayerBaseline) srcDir += old_copy;
            string dest = ProjectAsbDir;
            Directory.CreateDirectory(dest);
            var list = AOTGenericReferences.PatchedAOTAssemblyList;
            foreach (var asb in list)
            {
                string src_file = $"{srcDir}/{asb}";
                string dstFile = $"{dest}/{asb}.bytes";
                AOTAssemblyMetadataStripper.Strip(src_file, dstFile);
            }
            string asb_cs = $"{SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target)}/Assembly-CSharp.dll";
            File.Copy(asb_cs, $"{dest}/Assembly-CSharp.dll.bytes", true);
            AssetDatabase.Refresh();
            Debug.Log("拷贝到工程完成");
        }
    }

    public class ABAssetBuild : WooAsset.IAssetsBuild
    {

        public override List<string> GetAssetTags(string path)
        {
            if (path.StartsWith(AOTDefine.ASBDir)) return new List<string> { AOTDefine.HotAssemblyTag };
            if (path.EndsWith(".json") || path.EndsWith(".bytes")) return new List<string> { AOTDefine.ConfigAssetTag };
            return new List<string> { };
        }
        protected override AssetType CoverAssetType(string path, AssetType assetType, Type type)
        {
            if (path.EndsWith(".asmdef")) return AssetType.Ignore;

            if (path.StartsWith(AOTDefine.ASBDir) && assetType != AssetType.Directory && assetType != AssetType.Ignore)
                return AssetType.Raw;
            //if (type == AssetType.TextAsset)
            //{
            //    var tags = GetAssetTags(path);
            //    if (tags != null && tags.Contains(AOTDefine.ConfigAssetTag)) {
            //        return AssetType.Raw;
            //    }
            //}
            return base.CoverAssetType(path, assetType, type);
        }


        public override string GetVersion(string settingVersion, AssetTaskContext context)
        {
            return DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");

        }
        public override void Create(List<EditorAssetData> assets, List<EditorBundleData> result, EditorPackageData pkg)
        {
            var option = AssetsScriptableObject.Get<SpriteAtlasOption>();
            foreach (var asset in option.atlasPaths)
            {
                var paths = System.IO.Directory.GetDirectories(asset, "*.*", System.IO.SearchOption.AllDirectories);


                foreach (var item in paths)
                {
                    var path = item.ToRegularPath();
                    var find = assets.FindAll(x => x.directory == path);
                    if (find != null && find.Count != 0)
                    {
                        assets.RemoveAll(x => find.Contains(x));
                        EditorBundleTool.N2One(find, result);

                    }

                }

            }
            base.Create(assets, result, pkg);
        }
    }

}



