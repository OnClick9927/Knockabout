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

namespace GamePlay
{
    class ActorModifyAssetWindow : EditorWindow
    {
        [MenuItem("Tools/战斗/角色肉鸽")]
        public static ActorModifyAssetWindow Open()
        {
            return GetWindow<ActorModifyAssetWindow>();
        }

        private ActorModifyAsset asset;
        private static void ReFresh()
        {
            tree?.Reload();
        }
        static void Save(ActorModifyAsset asset)
        {
            File.WriteAllBytes(ActorModifyAsset.path, asset.ToBytes());
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
            asset.buffs.Add(add,new ActorModifyAsset.Modify()
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
        [NonSerialized] private ActorModifyAsset.Modify buff;
        public void ShowBuff(int id)
        {
            _buff_id = id;
            buff = asset.buffs.Values.FirstOrDefault(x => x.Id == id);
            tree?.SetSelection(new List<int> { id });
        }
        private int _buff_id;

        public static ActorModifyAsset EnsureBuff()
        {
            if (ActorModifyAsset.instance != null) return ActorModifyAsset.instance;
            ActorModifyAsset asset;
            if (!File.Exists(ActorModifyAsset.path))
            {
                asset = new ActorModifyAsset();
                Save(asset);
            }
            else
                asset = ActorModifyAsset.FromBytes(File.ReadAllBytes(ActorModifyAsset.path));
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

        public static void ShowBuff(ActorModifyAsset.Modify buff, bool enable = true)
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
            GUILayout.Label("Effects");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+"))
            {
                var types = typeof(ActorModifyEffect).GetSubTypesInAssemblies()
                       .Where(x => !x.IsAbstract);
                GenericMenu menu = new GenericMenu();
                foreach (var type in types)
                {

                    menu.AddItem(new GUIContent(EditorEX.GetTypeName(type)), false, () =>
                    {
                        buff.Effects.Add(Activator.CreateInstance(type) as ActorModifyEffect);
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
            private readonly ActorModifyAssetWindow window;
            private ActorModifyAsset asset => window.asset;
            private SearchField searchField;
            public Tree(ActorModifyAssetWindow window, TreeViewState state) : base(state)
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
            private Dictionary<int, ActorModifyAsset.Modify> map;
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


