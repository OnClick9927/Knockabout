using ActionBuffer;
using ActionEditor;
using Lockstep;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace GamePlay
{
    static class BackupEditorTools
    {
        static bool IsAssignableToGenericType(this Type typeToCheck, Type genericType)
        {
            // 校验入参：目标类型必须是泛型且是开放泛型（未指定类型参数）
            if (genericType == null || !genericType.IsGenericTypeDefinition)
            {
                throw new ArgumentException("genericType 必须是开放泛型类型（如 typeof(List<>)）", nameof(genericType));
            }

            // 处理 null 或值类型（值类型无继承链，直接判断是否匹配）
            if (typeToCheck == null) return false;
            Type currentType = typeToCheck.IsGenericType ? typeToCheck.GetGenericTypeDefinition() : typeToCheck;

            // 1. 先判断当前类型是否直接等于目标泛型（封闭泛型转开放泛型后对比）
            if (currentType == genericType)
            {
                return true;
            }

            // 2. 遍历基类链，检查是否有基类匹配目标泛型
            while (currentType != null && currentType != typeof(object))
            {
                // 如果当前基类是泛型，转开放泛型后对比
                if (currentType.IsGenericType)
                {
                    var genericDefinition = currentType.GetGenericTypeDefinition();
                    if (genericDefinition == genericType)
                    {
                        return true;
                    }
                }

                // 继续向上遍历基类
                currentType = currentType.BaseType;
            }

            // 3. 额外处理接口泛型（如果需要判断是否实现泛型接口）
            // 如需支持接口，取消下面注释：
            /*
            foreach (var interfaceType in typeToCheck.GetInterfaces())
            {
                if (interfaceType.IsGenericType)
                {
                    var interfaceGenericDef = interfaceType.GetGenericTypeDefinition();
                    if (interfaceGenericDef == genericType)
                    {
                        return true;
                    }
                }
            }
            */

            return false;
        }

        [MenuItem("Tools/Lockstep/BuildBackup(Empty)", priority = 2500)]
        static void BuildBackupEmpty()
        {
            _BuildBackup(true);
        }


        [MenuItem("Tools/Lockstep/BuildBackup", priority = 2500)]
        static void BuildBackup()
        {
            _BuildBackup(false);
        }

        static void _BuildBackup(bool empty)
        {
            var name = "Backups";
            var file = $"Assets/Scripts/GamePlay/Logic/{name}.cs";
            string result = $"using {nameof(ActionBuffer)};\n" +
                "using System;\n" +
                "using Lockstep;\n" +
                "using System.Text;\n" +
                "namespace GamePlay {\n";
            var types = typeof(BackupAttribute).Assembly.GetTypes()
                .Where(x => x.GetCustomAttribute<BackupAttribute>(false) != null)
                .Where(x => !x.IsNested);
            foreach (var type in types)
            {

                result += WriteClass(type, empty);


            }
            result += "}\n";



            File.WriteAllText(file, result);
            AssetDatabase.Refresh();
        }
        static List<MemberInfo> GetMembers(Type type)
        {
            return type.GetMembers(BindingFlags.Public |
             BindingFlags.NonPublic |
             BindingFlags.Instance |
              BindingFlags.DeclaredOnly).Where(x => x.IsDefined(typeof(BackupAttribute)) &&
              (x is PropertyInfo || x is FieldInfo)

              ).ToList();
        }
        private static string WriteClass(Type type, bool empty)
        {

            string result = "\n";
            if (type.IsGenericType)
            {
                var Args = type.GetGenericArguments();
                result += $"partial class {type.Name.Split("`")[0]}<{string.Join(",", Args.Select(x => x.Name))}>:IBackup {"{"} \n";

            }
            else
            {
                result += $"partial class {type.Name}:IBackup {"{"} \n";
            }
            var _nests = type.GetNestedTypes()
                     .Where(x => x.GetCustomAttribute<BackupAttribute>(false) != null);
            foreach (var item in _nests)
            {
                result += WriteClass(item, empty);
            }

            var members = empty ? new() : GetMembers(type);

            bool has_base = false;
            while (true)
            {
                type = type.BaseType;
                if (type == typeof(System.Object)) break;
                if (type.IsDefined(typeof(BackupAttribute)))
                {
                    has_base = true;
                    break;
                }

            }


            if (has_base)
                result += "public override void ReadBackup(BufferReader reader){\n" +
                         "base.ReadBackup(reader);\n";
            else
                result += "public virtual void ReadBackup(BufferReader reader){\n";
            for (int i = 0; i < members.Count; i++)
                result += $"{ReadMember(members[i])};\n";
            result += "}\n";


            if (has_base)
                result += "public override void WriteBackup(BufferWriter writer){\n" +
                         "base.WriteBackup(writer);\n";
            else
                result += "public virtual void WriteBackup(BufferWriter writer){\n";
            for (int i = 0; i < members.Count; i++)
                result += $"{WriteMember(members[i])};\n";
            result += "}\n";

            if (has_base)
                result += "public override void DumpString(StringBuilder builder,string perfix){\n" +
                         "base.DumpString(builder,perfix);\n";
            else
                result += "public virtual void DumpString(StringBuilder builder,string perfix){\n";
            for (int i = 0; i < members.Count; i++)
                result += $"{WriteDump(members[i])};\n";
            result += "}\n";


            if (has_base)
                result += "public override int GetHash(ref int idx){\n" +
                         "int hash = base.GetHash(ref idx);\n";
            else
                result += "public virtual int GetHash(ref int idx){\n int hash = 1;\n";
            for (int i = 0; i < members.Count; i++)
                result += $"{GetHash(members[i])};\n";
            result += "return hash;\n";
            result += "}\n";



            result += "}\n";
            return result;
        }
        private static string WriteMember(MemberInfo member)
        {
            Type type = null;
            string name = string.Empty;
            if (member.MemberType == MemberTypes.Field)
            {
                FieldInfo fi = member as FieldInfo;
                type = fi.FieldType;
                name = fi.Name;
            }
            else if (member.MemberType == MemberTypes.Property)
            {
                PropertyInfo pi = member as PropertyInfo;
                type = pi.PropertyType;
                name = pi.Name;
            }
            return WriteMember(member.GetCustomAttribute<BackupAttribute>(), type, name);
        }
        private static string WriteDump(MemberInfo member)
        {
            Type type = null;
            string name = string.Empty;
            if (member.MemberType == MemberTypes.Field)
            {
                FieldInfo fi = member as FieldInfo;
                type = fi.FieldType;
                name = fi.Name;
            }
            else if (member.MemberType == MemberTypes.Property)
            {
                PropertyInfo pi = member as PropertyInfo;
                type = pi.PropertyType;
                name = pi.Name;
            }
            return WriteDump(type, name);
        }
        private static string WriteDump(Type type, string name)
        {

            string result = string.Empty;

            if (type.IsBackUp())
                return $"builder.AppendLine($\"{{perfix}}{{nameof({name})}}:\");\n" +
                    $"{name}.{nameof(IBackup.DumpString)}(builder,\"\\t\"+perfix)";
            else if (type.IsGenericType)
            {

                var args = type.GetGenericArguments();
                if (args.Length == 1 && type.GetInterface(typeof(IEnumerable).FullName) != null)
                {

                    var arg0 = args[0];

                    if (arg0.IsBackUp())
                    {


                        return $"builder.AppendLine($\"{{perfix}}{{nameof({name})}}: [\");\n" +
                                  $"foreach (var item in {name}){{\n" +
                                  "builder.AppendLine($\"{perfix}{{\");\n" +

                                  $"item.{nameof(IBackup.DumpString)}(builder,\"\\t\"+perfix);\n" +
                                  $"builder.AppendLine($\"{{perfix}}}}}}\");\n" +

                                  "}\n" +
                                  $"builder.AppendLine($\"{{perfix}}]\")\n";

                    }
                    else
                    {
                        return $"builder.AppendLine($\"{{perfix}}{{nameof({name})}}: [\");\n" +
                                $"foreach (var item in {name}){{\n" +
                                $"builder.AppendLine($\"\t{{perfix}}{{item.ToString()}}\");\n" +
                                "}\n" +
                                $"builder.AppendLine($\"{{perfix}}]\")\n";
                    }
                }



            }





            return $"builder.AppendLine($\"{{perfix}}{{nameof({name})}}:{{{name}.ToString()}}\")";
        }
        // builder.AppendLine($"{perfix}{nameof(lastFrame)}:{lastFrame.ToString()}");
        private static string GetHash(MemberInfo member)
        {
            Type type = null;
            string name = string.Empty;
            if (member.MemberType == MemberTypes.Field)
            {
                FieldInfo fi = member as FieldInfo;
                type = fi.FieldType;
                name = fi.Name;
            }
            else if (member.MemberType == MemberTypes.Property)
            {
                PropertyInfo pi = member as PropertyInfo;
                type = pi.PropertyType;
                name = pi.Name;
            }
            return GetHash(type, name);
        }

        private static string GetHash(Type type, string name)
        {
            string result = string.Empty;
            if (type.IsGenericType)
            {

                var args = type.GetGenericArguments();
                if (args.Length == 1 && type.GetInterface(typeof(IEnumerable).FullName) != null)
                {

                    var arg0 = args[0];

                    return
                                $"foreach (var item in {name}){{\n" +
                                $"{GetHash(arg0, "item")};\n" +
                                "}\n";
                }



            }
            return $"hash += {name}.{nameof(IBackup.GetHash)}(ref idx) * PrimerLUT.GetPrimer(idx++);";


        }
        private static bool IsBackUp(this Type type)
        {
            if (type.IsDefined(typeof(BackupAttribute)))
                return true;
            if (type.GetInterface(typeof(IBackup).FullName) != null)
                return true;
            return false;
        }
        private static string WriteMember(BackupAttribute attribute, Type type, string name)
        {

            string result = string.Empty;

            if (type.IsEnum)
                result = $"{nameof(BufferWriter.WriteEnum)}";
            if (type.IsBackUp())
                return $"{name}.{nameof(IBackup.WriteBackup)}(writer)";
            else if (type == typeof(long))
                result = $"{nameof(BufferWriter.WriteInt64)}";
            else if (type == typeof(float))
                result = $"{nameof(BufferWriter.WriteFloat)}";
            else if (type == typeof(int))
                result = $"{nameof(BufferWriter.WriteInt32)}";
            else if (type == typeof(short))
                result = $"{nameof(BufferWriter.WriteInt16)}";
            else if (type == typeof(bool))
                result = $"{nameof(BufferWriter.WriteBool)}";
            else if (type == typeof(string))
                result = $"{nameof(BufferWriter.WriteUTF8)}";

            else if (type == typeof(Lockstep.Random))
                return $"writer.{nameof(BufferWriter.WriteUInt64)}({name}.randSeed)";

            else if (type == typeof(LFloat))
                return $"writer.{nameof(BufferWriter.WriteInt64)}({name}._val)";
            else if (type == typeof(LVector2))
            {
                return $"writer.{nameof(BufferWriter.WriteInt64)}({name}.x._val);\n" +
                     $"writer.{nameof(BufferWriter.WriteInt64)}({name}.y._val);";

            }
            else if (type == typeof(LVector3))
            {
                return $"writer.{nameof(BufferWriter.WriteInt64)}({name}.x._val);\n" +
                     $"writer.{nameof(BufferWriter.WriteInt64)}({name}.y._val);\n" +
                     $"writer.{nameof(BufferWriter.WriteInt64)}({name}.z._val);\n";

            }
            else if (type.IsGenericType)
            {
                var args = type.GetGenericArguments();
                if (type.IsAssignableToGenericType(typeof(List<>)))
                {
                    var arg0 = args[0];
                    if (arg0.IsBackUp())
                    {
                        if (!attribute.CustomCreateElement)
                        {
                            return $"{"{"} writer.WriteUInt16(Convert.ToUInt16({name}.Count));\n" +

                                $"for (int i = 0; i < {name}.Count; i++){"{"}\n" +
                          $"var back= {name}[i];\n" +
                          $"back.{nameof(IBackup.WriteBackup)}(writer);\n" +
                          "}}";
                        }
                        else
                            return $"for (int i = 0; i < {name}.Count; i++){"{"}\n" +
                                 $"var back= {name}[i];\n" +
                                 $"back.{nameof(IBackup.WriteBackup)}(writer);\n" +
                                 "}";

                    }
                    else
                    {
                        return
                   $"{"{"} writer.WriteUInt16(Convert.ToUInt16({name}.Count));\n" +
                   $"for (int i = 0; i < {name}.Count; i++){"{"}\n" +
                   $"{WriteMember(null, arg0, $"{name}[i]")};\n" +
                   "}}\n";
                    }


                }
                if (type.IsAssignableToGenericType(typeof(HashSet<>)))
                {
                    var arg0 = args[0];
                    if (arg0.IsBackUp())
                    {

                    }
                    else
                        return $"using (var pool = StaticPool.CreateDisposableArray<{arg0.FullName.Replace("+", ".")}>({name}.Count)){"{"}\n" +
                           $"{name}.CopyTo(pool.value);\n" +
                           $" writer.WriteUInt16(Convert.ToUInt16(pool.value.Length));\n" +
                           $"for (int i = 0; i < pool.value.Length; i++){"{"}\n" +
                           $"{WriteMember(null, arg0, "pool.value[i]")};\n" +
                           "}}\n";
                }
            }


            return $"writer.{result}({name})";
        }

        static string ReadMember(MemberInfo member)
        {

            Type type = null;
            string name = string.Empty;


            if (member.MemberType == MemberTypes.Field)
            {
                FieldInfo fi = member as FieldInfo;
                type = fi.FieldType;
                name = fi.Name;
            }
            else if (member.MemberType == MemberTypes.Property)
            {
                PropertyInfo pi = member as PropertyInfo;
                type = pi.PropertyType;
                name = pi.Name;
            }
            return ReadMember(member.GetCustomAttribute<BackupAttribute>(), type, name);
        }
        private static string ReadMember(BackupAttribute attribute, Type type, string name)
        {

            string result = string.Empty;

            if (type.IsEnum)
                result = $"({type.FullName.Replace("+", ".")})reader.{nameof(BufferReader.ReadEnum)}(typeof({type.FullName.Replace("+", ".")}))";
            if (type.IsBackUp())
                return $"{name}.{nameof(IBackup.ReadBackup)}(reader)";
            //else if (type == typeof(Property))
            //    return $"{name}.{nameof(Property.Set)}(reader.{nameof(BufferReader.ReadInt64)}())";

            if (type == typeof(long))
                result = $"reader.{nameof(BufferReader.ReadInt64)}()";
            if (type == typeof(float))
                result = $"reader.{nameof(BufferReader.ReadFloat)}()";
            else if (type == typeof(int))
                result = $"reader.{nameof(BufferReader.ReadInt32)}()";
            else if (type == typeof(short))
                result = $"reader.{nameof(BufferReader.ReadInt16)}()";
            else if (type == typeof(bool))
                result = $"reader.{nameof(BufferReader.ReadBool)}()";
            else if (type == typeof(string))
                result = $"reader.{nameof(BufferReader.ReadUTF8)}()";


            else if (type == typeof(Lockstep.Random))
            {
                //new Lockstep.Random(read);
                result = $"new Lockstep.Random((uint)reader.{nameof(BufferReader.ReadUInt64)}())";
            }


            else if (type == typeof(LFloat))
                result = $"LFloat.FromRaw(reader.{nameof(BufferReader.ReadInt64)}())";
            else if (type == typeof(LVector2))
            {
                result = $"new LVector2(LFloat.FromRaw(reader.{nameof(BufferReader.ReadInt64)}()), LFloat.FromRaw(reader.{nameof(BufferReader.ReadInt64)}()))";
            }
            else if (type == typeof(LVector3))
            {
                result = $"new LVector3(LFloat.FromRaw(reader.{nameof(BufferReader.ReadInt64)}())," +
                    $"LFloat.FromRaw(reader.{nameof(BufferReader.ReadInt64)}())," +
                    $"LFloat.FromRaw(reader.{nameof(BufferReader.ReadInt64)}()))";
            }
            else if (type.IsGenericType)
            {
                var args = type.GetGenericArguments();
                if (type.IsAssignableToGenericType(typeof(List<>)))
                {
                    var arg0 = args[0];
                    if (!attribute.CustomCreateElement)
                    {
                        string add = string.Empty;
                        if (!arg0.IsValueType && arg0.IsBackUp())
                        {

                            add = $"  GameHelper.SetListToPool({name});\n";
                        }
                        else
                        {
                            add = $"{name}?.Clear();\n";
                        }

                        if (!arg0.IsBackUp())
                        {
                            return add +
                                             $"{"{"}var len = reader.ReadUInt16();\n" +
                                                    "for (int i = 0; i < len; i++){\n" +

                                              (!arg0.IsValueType ? 
                                                                    $"var {ReadMember(null, arg0, "back")};\n" :
                                                                    $"var {ReadMember(null, arg0, "back")};\n") +
                                                    $"{name}.Add(back);" +

                                                    "}}\n";
                        }
                        return add +
                            $"{"{"}var len = reader.ReadUInt16();\n" +
                                   "for (int i = 0; i < len; i++){\n" +

                             (!arg0.IsValueType ? $"var back= StaticPool.Get<{arg0.FullName.Replace("+", ".")}>();\n" +
                                                        $"{ReadMember(null, arg0, "back")};\n" :
                                                   $"var {ReadMember(null, arg0, "back")};\n") +
                                   $"{name}.Add(back);" +

                                   "}}\n";
                    }
                    else
                        return
                            $"for (int i = 0; i < {name}.Count; i++){"{"}\n" +
                             $"var back= {name}[i];\n" +
                             $"back.{nameof(IBackup.ReadBackup)}(reader);\n" +
                             "}";




                }
                if (type.IsAssignableToGenericType(typeof(HashSet<>)))
                {

                    var arg0 = args[0];

                    if (arg0.IsBackUp())
                    {

                    }
                    else
                        return $"{name}?.Clear();\n" +
                            $"{"{"}var len = reader.ReadUInt16();\n" +
                           "for (int i = 0; i < len; i++){\n" +
                           $"var {ReadMember(null, arg0, "back")};\n" +
                           $"{name}.Add(back);" +

                           "}}\n";


                }
            }

            return $"{name}= {result}";
        }
    }


}

