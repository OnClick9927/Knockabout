using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace GamePlay
{

    class TagAsset : ScriptableObject
    {
        private static TagAsset _ins;
        private const string path = "Assets/Editor/TagAsset.asset";

        public static TagAsset Get()
        {
            if (_ins == null)
            {
                if (!System.IO.File.Exists(path))
                {
                    _ins = ScriptableObject.CreateInstance<TagAsset>();
                    AssetDatabase.CreateAsset(_ins, path);
                    AssetDatabase.SaveAssetIfDirty(_ins);
                    AssetDatabase.Refresh();
                }
                if (_ins == null)
                    _ins = AssetDatabase.LoadAssetAtPath<TagAsset>(path);
            }
            return _ins;
        }

        public List<string> Tags = new List<string>();
        public List<string> fixeds = new List<string>();

        public void Valid()
        {
            fixeds = typeof(Tags).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                  .Where(x => x.FieldType == typeof(string))
                  .Select(x => x.GetValue(null) as string).ToList();

            foreach (var item in fixeds)
            {
                if (!Tags.Contains(item))
                    Tags.Add(item);
            }
        }
    }
}
