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
using static GamePlay.AbilityEffect;
using static GamePlay.BuffAsset;

namespace GamePlay
{



    class AbilityAssetWindow : EditorWindow
    {
        [MenuItem("Tools/战斗/能力")]
        public static AbilityAssetWindow Open()
        {
            return GetWindow<AbilityAssetWindow>();
        }

        private AbilityAsset asset;
        private static void ReFresh()
        {
            tree?.Reload();
        }
        static void Save(AbilityAsset asset)
        {
            File.WriteAllBytes(AbilityAsset.path, asset.ToBytes());
            AssetDatabase.Refresh();
        }
        void Save() => Save(asset);
        void Add()
        {
            if (asset.abilitys.Values.Any(x => x.Id == add))
            {
                ShowNotification(new GUIContent("Same Ability Id"));
                return;
            }
            var ab = new Ability();
            ab.Id = add;
            asset.abilitys.Add(add, ab);
            ReFresh();

            tree.SetSelection(new List<int> { add });
            tree.SetFocus();
            ShowBuff(add);
        }
        private TreeViewState state;
        private static Tree tree;
        IFramework.EditorTools.SplitView sp;
        [NonSerialized] private Ability ability;
        public void ShowBuff(int id)
        {
            _buff_id = id;
            ability = asset.abilitys.Values.FirstOrDefault(x => x.Id == id);
            tree?.SetSelection(new List<int> { id });
        }
        private int _buff_id;

        public static AbilityAsset EnsureBuff()
        {
            if (AbilityAsset.instance != null) return AbilityAsset.instance;
            AbilityAsset asset;
            if (!File.Exists(AbilityAsset.path))
            {
                asset = new AbilityAsset();
                Save(asset);
            }
            else
                asset = AbilityAsset.FromBytes(File.ReadAllBytes(AbilityAsset.path));
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
            if (GUILayout.Button("Buff", EditorStyles.toolbarButton))
                BuffAssetWindow.Open();
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
            ShowBuff(ability);
        }
        private static TriggerType _type;
        public static void ShowBuff(Ability ability, bool enable = true)
        {
            if (ability == null) return;
            //GUILayout.BeginHorizontal();
            //GUILayout.Label(EditorEX.GetTypeName(ability.GetType()),GUILayout.Width(EditorGUIUtility.labelWidth-12));
            //ActionEditor.EditorEX.DrawPingScript(ability.GetType());
            //GUILayout.EndHorizontal();
            GUI.enabled = enable;
            EditorGUI.BeginChangeCheck();
            EditorEX.CreateEditor(ability).OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
            {
                ReFresh();
            }
            var names = Enum.GetNames(typeof(TriggerType));
            if (ability.Type == Ability.AbilityType.None)
            {
                ArrayUtility.Remove(ref names, nameof(TriggerType.Update));

                ability.effects.RemoveAll(x => x.trigger == TriggerType.Update);
            }
            var index = Array.IndexOf(names, _type.ToString());
            index = Mathf.Max(0, index);
            for (int i = 0; i < names.Length; i++)
            {
                var value = (TriggerType)Enum.Parse(typeof(TriggerType), names[i]);

                names[i] = $"{names[i]}  {ability.effects.Count(x => x.trigger == value)}";
            }
            index = GUILayout.Toolbar(index, names);
            _type = Enum.Parse<TriggerType>(names[index].Split(" ").First());


            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label("Effects");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+"))
            {
                var types = typeof(AbilityEffect).GetSubTypesInAssemblies()
                       .Where(x => !x.IsAbstract);
                GenericMenu menu = new GenericMenu();
                foreach (var type in types)
                {

                    menu.AddItem(new GUIContent(EditorEX.GetTypeName(type)), false, () =>
                    {
                        var effect = Activator.CreateInstance(type) as AbilityEffect;
                        effect.trigger = _type;
                        ability.effects.Add(effect);
                        ReFresh();
                    });
                }
                menu.ShowAsContext();
            }
            GUILayout.EndHorizontal();
            GUI.enabled = true;

            scroll = GUILayout.BeginScrollView(scroll);
            GUI.enabled = enable;
            for (int i = 0; i < ability.effects.Count; i++)
            {

                var effect = ability.effects[i];
                if (effect.trigger != _type) continue;
                GUILayout.BeginVertical(EditorStyles.helpBox);

                GUILayout.BeginHorizontal(EditorStyles.toolbar);
                GUILayout.Label(effect.GetTypeName());
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("x"))
                {
                    ability.effects.RemoveAt(i);
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
            private readonly AbilityAssetWindow window;
            private AbilityAsset asset => window.asset;
            private SearchField searchField;
            public Tree(AbilityAssetWindow window, TreeViewState state) : base(state)
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
            private Dictionary<int, Ability> map;
            public new void Reload()
            {
                map = asset.abilitys;
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
                    asset.abilitys.Remove(id);
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
                foreach (var item in asset.abilitys.Values)
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


