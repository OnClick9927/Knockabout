using ActionBuffer;
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
partial class EditorGameTools
{
    public class NetBuff
    {
        [MenuItem("Tools/SC/同步序列化给Server", priority = -10000)]
        public static void CopySer()
        {
            Type type = typeof(IBufferObject);
            System.Reflection.Assembly assembly = type.Assembly;
            string assemblyPath = assembly.Location;
            var target = $"Assets/../../RGBS/_Frame/{Path.GetFileName(assemblyPath)}";
            if (File.Exists(target))
            {
                File.Delete(target);
            }
            File.Copy(assemblyPath, target);
        }
        [MenuItem("Tools/SC/同步协议", priority = -9999)]
        public static void Sync()
        {
            var path = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(nameof(Proto));
            //AssetDatabase.FindAssets($"t:{AssemblyDefinition}",src)
            File.Move(path, "xxx");
            var src = Path.GetDirectoryName(path);
            Config.CopyDir(target, src);
            File.Move("xxx", path);
            AssetDatabase.Refresh();
        }
        const string target = "Assets/../../RGBS/_Proto";




    }

}


