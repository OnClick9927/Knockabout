using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
namespace GamePlay
{
    class TagAssetEditor : EditorWindow
    {
        [MenuItem("Tools/战斗/Tags")]
        static void GetWindow()
        {
            GetWindow<TagAssetEditor>("Tags Editor");
        }

        private TreeViewState state = new TreeViewState();
        private Tree tree;
        private string inputText = "";

        private void OnEnable()
        {
            tree = new Tree(TagAsset.Get(), state);
        }

        private void OnGUI()
        {
            DrawToolbar();
            Rect rect = GUILayoutUtility.GetRect(0, 10000, 0, 10000);
            tree.OnGUI(rect);
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUI.SetNextControlName("SearchAddField");
            EditorGUI.BeginChangeCheck();
            string newInput = EditorGUILayout.TextField(inputText, GUILayout.Width(200));
            if (EditorGUI.EndChangeCheck())
            {
                inputText = newInput;
                tree.searchString = inputText;
                tree.Reload();
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return &&
                GUI.GetNameOfFocusedControl() == "SearchAddField")
            {
                AddTagFromInput();
                Event.current.Use();
            }

            if (GUILayout.Button("Add", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                AddTagFromInput();
            }

            if (GUILayout.Button("Delete", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                DeleteSelected();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void AddTagFromInput()
        {
            string tag = inputText.Trim();
            if (string.IsNullOrEmpty(tag))
            {
                Debug.LogWarning("Tag name cannot be empty.");
                return;
            }
            var asset = TagAsset.Get();
            if (asset.Tags.Contains(tag))
            {
                Debug.LogWarning($"Tag '{tag}' already exists.");
                return;
            }
            asset.Tags.Add(tag);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            inputText = "";
            tree.searchString = "";
            tree.Reload();
        }

        private void DeleteSelected()
        {
            var selected = tree.GetSelection();
            if (selected.Count == 0) return;
            var asset = TagAsset.Get();
            var all = selected.Select(x => tree.FindItem(x).displayName).Where(x => !asset.fixeds.Contains(x));
            foreach (var _tag in all)
                asset.Tags.RemoveAll(x => Tags.ValueIsTag(x, _tag));

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            tree.Reload();
        }

        // ------------------- 内部 TreeView 实现 -------------------
        class Tree : TreeView
        {
            private readonly TagAsset asset;

            public Tree(TagAsset asset, TreeViewState state) : base(state)
            {
                this.asset = asset;
                this.showAlternatingRowBackgrounds = true;
                Reload();
                //ExpandAll(); // 默认展开，方便查看层级
            }

            public TreeViewItem FindItem(int id)
            {
                return FindItem(id, rootItem);
            }

            protected override TreeViewItem BuildRoot()
            {
                return new TreeViewItem() { depth = -1 };
            }

            protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
            {
                asset.Valid();

                // 排序：固定标签优先，其余按字母序
                var sortedTags = asset.Tags
                    .OrderBy(t => !asset.fixeds.Contains(t)) // 固定标签为 true 排前面
                    .ThenBy(t => t)
                    .ToList();

                // 搜索过滤（不区分大小写）
                var filteredTags = sortedTags
                    .Where(t => string.IsNullOrEmpty(searchString) ||
                                t.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

                // 构建节点字典（tag -> TreeViewItem）
                var itemDict = new Dictionary<string, TreeViewItem>();
                foreach (var tag in filteredTags)
                {
                    int id = asset.Tags.IndexOf(tag); // 用原始列表索引作为 ID
                    bool isFixed = asset.fixeds.Contains(tag);
                    var item = new TreeViewItem(id, 0, isFixed ? tag + " (fixed)" : tag);
                    itemDict[tag] = item;
                }

                // 建立父子关系（根据 _ 分隔符）
                foreach (var tag in filteredTags)
                {
                    var item = itemDict[tag];
                    // 查找最长前缀（父标签）
                    string parentTag = null;
                    int maxLen = -1;
                    foreach (var other in filteredTags)
                    {
                        if (other == tag) continue;
                        if (tag.StartsWith(other + Tags.sp) && other.Length > maxLen)
                        {
                            maxLen = other.Length;
                            parentTag = other;
                        }
                    }
                    if (parentTag != null && itemDict.ContainsKey(parentTag))
                    {
                        var parentItem = itemDict[parentTag];
                        if (parentItem.children == null)
                            parentItem.children = new List<TreeViewItem>();
                        parentItem.children.Add(item);
                    }
                }

                // 收集所有根节点（没有父节点的标签）
                root.children = new List<TreeViewItem>();
                foreach (var tag in filteredTags)
                {
                    bool hasParent = false;
                    foreach (var other in filteredTags)
                    {
                        if (other == tag) continue;
                        if (tag.StartsWith(other + Tags.sp))
                        {
                            hasParent = true;
                            break;
                        }
                    }
                    if (!hasParent)
                    {
                        root.children.Add(itemDict[tag]);
                    }
                }

                // 递归设置深度
                void SetDepth(TreeViewItem node, int depth)
                {
                    node.depth = depth;
                    if (node.children != null)
                    {
                        foreach (var child in node.children)
                            SetDepth(child, depth + 1);
                    }
                }
                SetDepth(root, -1);

                // 收集所有行（深度优先遍历）
                List<TreeViewItem> rows = new List<TreeViewItem>();
                void Collect(TreeViewItem node)
                {
                    if (node.depth >= 0) // 排除虚拟根
                        rows.Add(node);
                    if (node.children != null)
                    {
                        if (IsExpanded(node.id))
                            foreach (var child in node.children)
                                Collect(child);
                        else
                        {
                            node.children = CreateChildListForCollapsedParent();
                        }
                    }
                }
                if (root.children != null)
                {
                    foreach (var child in root.children)
                        Collect(child);
                }

                return rows;
            }

            // 右键菜单
            protected override void ContextClickedItem(int id)
            {
                var item = FindItem(id);
                if (item == null) return;

                GenericMenu menu = new GenericMenu();
                string tag = asset.Tags[item.id];
                bool isFixed = asset.fixeds.Contains(tag);

                menu.AddItem(new GUIContent("Copy Tag Name"), false, () => CopyTagName(item));
                menu.ShowAsContext();
            }

            private void CopyTagName(TreeViewItem item)
            {
                string tag = asset.Tags[item.id];
                EditorGUIUtility.systemCopyBuffer = tag;
                Debug.Log($"Copied tag '{tag}' to clipboard.");
            }



            // 无 KeyDown 方法
        }
    }
}
