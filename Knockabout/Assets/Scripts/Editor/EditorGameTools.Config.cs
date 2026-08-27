using IFramework;
using Luban;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using WooLocalization;
partial class EditorGameTools
{
    public class Config
    {
        [UnityEditor.InitializeOnLoadMethod]
        static void Init()
        {
            static ByteBuf LoadBin(string file)
            {
                string path = Configs.GetConfigFile(file);
                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (asset)
                    return ByteBuf.Wrap(asset.bytes);
                return new ByteBuf();
            }
            Configs.Init(new Tables(LoadBin));

        }





        const string exceldir = "Assets/../../excels/";
        public static string ToolsPath => "Assets/../../Tools/Client";
        const string target = "Assets/../../RGBS/_Excel";
        [MenuItem("Tools/SC/同步Luban给Server")]
        public static void SyncToServer()
        {
            var path = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(nameof(Luban));
            CopyDir(Path.GetDirectoryName(path), target, false);
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/SC/打开配置表格文件夹")]
        public static void OpenFolder()
        {
            IFramework.EditorTools.OpenFolder(exceldir);
        }
        [MenuItem("Tools/SC/打表")]

        public static void Sync()
        {
            var LocalizationPath_AOT = "";
            var LocalizationPath = "";

            var type = typeof(LocalizationBehavior);
            var method = type.GetField("defaultContext", BindingFlags.NonPublic | BindingFlags.Static);
            var defaultContext = method.GetValue(null) as LocalizationData;
            if (!defaultContext) return;
            LocalizationPath = AssetDatabase.GetAssetPath(defaultContext);
            LocalizationPath_AOT = AssetDatabase.FindAssets($"t:{nameof(LocalizationData)}")
                .Select(x => AssetDatabase.GUIDToAssetPath(x))
                .Where(x => x != LocalizationPath && x.Contains("AOT"))
                .FirstOrDefault();
            if (string.IsNullOrEmpty(LocalizationPath)) return;
            ToData($"{exceldir}_本地化/y-语言_AOT.xlsx", LocalizationPath_AOT, true);
            ToData($"{exceldir}_本地化/y-语言.xlsx", LocalizationPath, true);
            ToData($"{exceldir}_本地化/y-语言_剧情.xlsx", LocalizationPath, false);
            WooLocalization.LocalizationEditorHelper.GenKeys();
            string dir = ToolsPath;
            RunBat(dir.ToAbsPath() + "/excel.bat", new string[]
            {
                $"{target}/Gen".ToAbsPath(),
                $"{target}/Data".ToAbsPath(),
            }, dir.ToAbsPath());
            var path = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(nameof(Luban));

            File.Move(path, "xx");
            var _path = Path.GetDirectoryName(path);
            CopyDir(target, _path);
            CopyDir($"{_path}/Data", Configs.Directory, false);
            Directory.Delete($"{_path}/Data", true);
            File.Move("xx", path);
            AssetDatabase.Refresh();
            //Init();

        }


        public static void CopyDir(string src, string dest, bool deep = true)
        {

            if (deep && Directory.Exists(dest))
                Directory.Delete(dest, true);
            if (!Directory.Exists(dest))
                Directory.CreateDirectory(dest);
            if (deep)
            {

                var dirs = Directory.GetDirectories(src);
                foreach (var dir in dirs)
                {
                    if (dir.Contains("Editor")) continue;
                    var name = Path.GetFileName(dir);
                    var target = Path.Combine(dest, name);
                    CopyDir(dir, target);
                }
            }
            var files = Directory.GetFiles(src);
            foreach (var item in files)
            {
                if (item.Contains(".meta")) continue;
                if (item.Contains(".asmdef")) continue;

                var name = Path.GetFileName(item);
                var target = Path.Combine(dest, name);
                if (File.Exists(target))
                    File.Delete(target);
                File.Copy(item, target);
            }
        }


        private static void ToData(string path, string dest, bool clear)
        {
            var context = AssetDatabase.LoadAssetAtPath<WooLocalization.LocalizationData>(dest);
            if (clear)
                WooLocalization.LocalizationEditorHelper.ClearContext(context);
            WooLocalization.LocalizationEditorHelper.ReadExcel(path, context);
        }

        public static System.Diagnostics.Process CreateShellExProcess(string cmd, string args, string workingDir = "")
        {
            var pStartInfo = new System.Diagnostics.ProcessStartInfo(cmd);
            pStartInfo.Arguments = args;
            pStartInfo.CreateNoWindow = false;
            pStartInfo.UseShellExecute = true;
            pStartInfo.RedirectStandardError = false;
            pStartInfo.RedirectStandardInput = false;
            pStartInfo.RedirectStandardOutput = false;
            if (!string.IsNullOrEmpty(workingDir))
                pStartInfo.WorkingDirectory = workingDir;
            return System.Diagnostics.Process.Start(pStartInfo);
        }
        public static void RunBat(string batfile, string[] args, string workingDir = "")
        {
            RunBat(batfile, string.Join(" ", args), workingDir);
        }

        public static void RunBat(string batfile, string args, string workingDir = "")
        {
            var p = CreateShellExProcess(batfile, args, workingDir);

            p.WaitForExit();
            p.Close();
        }
    }

}


