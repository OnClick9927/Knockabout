using ActionAttribute;
using ActionEditor;
using IFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using static GamePlay.BuffEffect;

namespace GamePlay
{
    class BuffAssetWindow : EditorWindow
    {
        [MenuItem("Tools/战斗/Buff")]
        public static BuffAssetWindow Open()
        {
            return GetWindow<BuffAssetWindow>();
        }

        private BuffAsset asset;
        private static void ReFresh()
        {
            tree?.Reload();
        }
        static void Save(BuffAsset asset)
        {
            File.WriteAllBytes(BuffAsset.path, asset.ToBytes());
            AssetDatabase.Refresh();
        }
        void Save() => Save(asset);
        void Add()
        {
            if (asset.buffs.Values.Any(x => x.Id == add))
            {
                ShowNotification(new GUIContent("Same Buff Id"));
                return;
            }
            asset.buffs.Add(add, new BuffAsset.Buff()
            {
                Id = add,
            });
            ReFresh();

            tree.SetSelection(new List<int> { add });
            tree.SetFocus();
            ShowBuff(add);
        }
        private TreeViewState state;
        private static Tree tree;
        IFramework.EditorTools.SplitView sp;
        [NonSerialized] private BuffAsset.Buff buff;
        public void ShowBuff(int id)
        {
            _buff_id = id;
            buff = asset.buffs.Values.FirstOrDefault(x => x.Id == id);
            tree?.SetSelection(new List<int> { id });
        }
        private int _buff_id;

        public static BuffAsset EnsureBuff()
        {
            if (BuffAsset.instance != null) return BuffAsset.instance;
            BuffAsset asset;
            if (!File.Exists(BuffAsset.path))
            {
                asset = new BuffAsset();
                Save(asset);
            }
            else
                asset = BuffAsset.FromBytes(File.ReadAllBytes(BuffAsset.path));
            return asset;
        }
        private void OnEnable()
        {
            asset = EnsureBuff();
            state = state ?? new TreeViewState();
            tree = new Tree(this, state);
            sp = new IFramework.EditorTools.SplitView();
            ShowBuff(_buff_id);
        }
        private void OnDisable()
        {
            Save();
        }
        private int add;
        private void Header()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(20));
            GUILayout.Space(10);
            var src = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 30;
            add = EditorGUILayout.IntField("New", add, GUILayout.Width(120));
            EditorGUIUtility.labelWidth = src;

            if (GUILayout.Button("+", EditorStyles.toolbarButton, GUILayout.Width(20)))
                Add();

            GUILayout.FlexibleSpace();
            if (GUILayout.Button(nameof(Save), EditorStyles.toolbarButton))
                Save();

            EditorGUILayout.EndHorizontal();
        }
        private void Left(Rect rect)
        {
            tree.OnGUI(rect);
        }
        private void Right()
        {
            ShowBuff(buff);
        }
        private static TriggerType _type;

        public static void ShowBuff(BuffAsset.Buff buff, bool enable = true)
        {
            if (buff == null) return;
            EditorGUI.BeginChangeCheck();
            GUI.enabled = enable;
            EditorEX.CreateEditor(buff).OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
            {
                ReFresh();
            }
            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            var names = Enum.GetNames(typeof(TriggerType));
            if (buff.trigger == BuffAsset.Buff.TriggerType.None)
            {
                ArrayUtility.Remove(ref names, nameof(TriggerType.Jump));
                buff.Effects.RemoveAll(x => x.trigger == TriggerType.Jump);
                if (buff.addType == BuffAsset.Buff.AddType.Immediately)
                {
                    ArrayUtility.Remove(ref names, nameof(TriggerType.Remove));
                    buff.Effects.RemoveAll(x => x.trigger == TriggerType.Remove);
                }
            }
            var index = Array.IndexOf(names, _type.ToString());
            index = Mathf.Max(0, index);
            for (int i = 0; i < names.Length; i++)
            {
                var value = (TriggerType)Enum.Parse(typeof(TriggerType), names[i]);

                names[i] = $"{names[i]}  {buff.Effects.Count(x => x.trigger == value)}";
            }
            index = GUILayout.Toolbar(index, names);
            _type = Enum.Parse<TriggerType>(names[index].Split(" ").First());


            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(EditorStyles.helpBox);

            GUILayout.Label("Effects");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+"))
            {
                var types = typeof(BuffEffect).GetSubTypesInAssemblies()
                       .Where(x => !x.IsAbstract);
                GenericMenu menu = new GenericMenu();
                foreach (var type in types)
                {

                    menu.AddItem(new GUIContent(EditorEX.GetTypeName(type)), false, () =>
                    {
                        var effect = Activator.CreateInstance(type) as BuffEffect;
                        effect.trigger = _type;
                        buff.Effects.Add(effect);
                        ReFresh();
                    });
                }
                menu.ShowAsContext();
            }
            GUILayout.EndHorizontal();
            GUI.enabled = true;

            scroll = GUILayout.BeginScrollView(scroll);
            GUI.enabled = enable;
            for (int i = 0; i < buff.Effects.Count; i++)
            {

                var effect = buff.Effects[i];
                if (effect.trigger != _type) continue;

                GUILayout.BeginVertical(EditorStyles.helpBox);

                GUILayout.BeginHorizontal(EditorStyles.toolbar);
                GUILayout.Label(effect.GetTypeName());
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("x"))
                {
                    buff.Effects.RemoveAt(i);
                    GUIUtility.ExitGUI();
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();

              EditorEX.CreateEditor(effect).OnInspectorGUI();

            }
            GUI.enabled = true;


            GUILayout.EndScrollView();
        }
        private static Vector2 scroll;
        private void OnGUI()
        {
            Header();
            sp.OnGUI(new Rect(0, 20, position.width, position.height - 20));
            var rects = sp.rects;
            Left(rects[0]);
            GUILayout.BeginArea(rects[1]);
            Right();
            GUILayout.EndArea();
        }

        private class Tree : TreeView
        {
            private readonly BuffAssetWindow window;
            private BuffAsset asset => window.asset;
            private SearchField searchField;
            public Tree(BuffAssetWindow window, TreeViewState state) : base(state)
            {
                this.window = window;
                this.showAlternatingRowBackgrounds = true;
                searchField = new SearchField();
                this.multiColumnHeader = new MultiColumnHeader(new(new MultiColumnHeaderState.Column[] {
                    new ()
                    {
                       autoResize = true,
                       width=20
                    },
                    new ()
                    {
                       autoResize = true,
                    },
                    new ()
                    {
                       autoResize = true,
                    },

                }))


                {
                    canSort = false,
                    height = 0,

                };
                Reload();
            }
            private Dictionary<int, BuffAsset.Buff> map;
            public new void Reload()
            {
                map = asset.buffs;
                base.Reload();
                this.multiColumnHeader.ResizeToFit();

            }
            protected override void RowGUI(RowGUIArgs args)
            {
                var id = args.item.id;
                var buff = map[id];
                if (GUI.Button(args.GetCellRect(0), "x"))
                {
                    SingleClickedItem(-1);
                    asset.buffs.Remove(id);
                    Reload();
                    return;
                }
                GUI.Label(args.GetCellRect(1), buff.Id.ToString());
                GUI.Label(args.GetCellRect(2), buff.Name);

            }
            public override void OnGUI(Rect rect)
            {
                var rs = IFramework.EditorTools.RectEx.HorizontalSplit(rect, 20);
                var tem = searchField.OnGUI(rs[0], this.searchString);
                if (tem != this.searchString)
                {
                    this.searchString = tem;
                    Reload();
                }
                base.OnGUI(rs[1]);
            }
            protected override void SingleClickedItem(int id)
            {
                window.ShowBuff(id);
            }
            protected override bool CanBeParent(TreeViewItem item) => false;
            protected override bool CanMultiSelect(TreeViewItem item) => false;
            protected override TreeViewItem BuildRoot()
            {
                return new TreeViewItem()
                {
                    id = -1,
                    depth = -1
                };
            }
            protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
            {
                var rows = GetRows() ?? new List<TreeViewItem>();
                rows.Clear();
                foreach (var item in asset.buffs.Values)
                {
                    if (!string.IsNullOrEmpty(this.searchString) && !item.Id.ToString().Contains(this.searchString))
                    {
                        continue;
                    }
                    var view = new TreeViewItem()
                    {
                        id = item.Id,
                        depth = 1,
                        displayName = $"{item.Id}-{item.Name}",
                        parent = root,
                    };
                    rows.Add(view);
                }


                return rows;
            }

        }


    }

}


