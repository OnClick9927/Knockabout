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
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using WooAsset;
using static WooAsset.AssetsEditorTool;

partial class EditorGameTools
{
    static class AppBuild
    {
        private static string ProjectAsbDir => AOTDefine.ASBDir;
        const string old_copy = "_old_copy";
        [MenuItem("Tools/打包/设置HyBirdCLR")]
        private static void Set()
        {

            HybridCLRSettings.Instance.hotUpdateAssemblies = new string[] { "Assembly-CSharp" };
            HybridCLRSettings.Instance.outputLinkFile = "AOT/link.xml";
            HybridCLRSettings.Instance.outputAOTGenericReferenceFile = "AOT/AOTGenericReferences.cs";

            EditorUtility.SetDirty(HybridCLRSettings.Instance);

            AssetDatabase.SaveAssets();

        }
        [MenuItem("Tools/打包/制作程序集")]
        public static async void Build()
        {
            PrebuildCommand.GenerateAll();
            CheckAccessMissingMetadata();
            CopyOldAOTAssembly();
            while (EditorApplication.isCompiling)
            {
                await Task.Delay(100);
            }
            CopyAssemblyToProject();
        }

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
                    System.IO.File.Copy(file, dest);//复制文件
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
            catch (Exception)
            {
                return 0;
            }

        }
        static void CopyAssemblyToProject()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            string srcDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
            string dest = ProjectAsbDir;
            if (Directory.Exists(dest))
                Directory.Delete(dest, true);
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



