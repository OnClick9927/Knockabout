using ActionEditor;
using ActionAttribute;
using ActionEditor.Nodes;
using System;
using System.Collections.Generic;

namespace GamePlay
{
    [System.Serializable, Name("技能"),AssetFileExtension("skill.bytes")]
    public class SkillAsset : ActionEditor.Nodes.GraphAsset
    {
        [System.Serializable]
        public class DynamicField
        {
            public FieldType fieldType;
            public string name;
            public enum FieldType
            {
                Int,
                Bool,
            }
        }
        public enum CDType
        {
            //无CD
            None = 0,
            //需要技能通知开始CD
            WaitSkill,
            //play之后立即CD
            Normal
        }



        public CDType cdType;
        private bool ShowCooldown => cdType == CDType.Normal || cdType == CDType.WaitSkill;
        [Condition(ConditionMode.Show, nameof(ShowCooldown))]
        public float cd;
        [Name("动态字段")]

        public List<DynamicField> dynamicFields = new List<DynamicField>();



        [Name("肉鸽名字")]

        public List<string> modifyNames = new List<string>();



#if UNITY_5_3_OR_NEWER
        [UnityEngine.Space(10)]
#endif
        [Name("需要标签")]
        [TagSelector] public List<string> needTags;
        [Name("禁止标签")]
        [TagSelector] public List<string> noTags;
        [Name("属性消耗")]
        public List<PropertyCost> costs = new List<PropertyCost>();




        [System.NonSerialized] private Dictionary<SkillEventType, List<SkillClipSequence>> seqs;
        public List<SkillClipSequence> GetEvents(SkillEventType type)
        {
            if (seqs == null) return null;
            if (this.seqs.TryGetValue(type, out var list)) return list;
            return null;
        }
        [System.NonSerialized, NodePort(NodePortAttribute.Direction.Output)] public SkillPropertyCollection property;
        [System.NonSerialized, NodePort(NodePortAttribute.Direction.Output, false, type = typeof(SkillModify))] public List<SkillModify> modifies;
        public override void PrepareForRuntime()
        {
            base.PrepareForRuntime();
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node is SkillAssetRoot root)
                    ReadRoot(root);
                else if (node is SkillModify modify)
                    ReadRoot(modify);
                else if (node is SkillClipSequence seq)
                    ReadRoot(seq);
                else if (node is SkillForClip _for)
                    ReadRoot(_for);
                else if (node is SkillAndCondition and)
                    ReadRoot(and);
                else if (node is SkillORCondition or)
                    ReadRoot(or);
                else if (node is SkillIFSignal _if)
                    ReadRoot(_if);
                else if (node is SkillNotCondition not)
                    ReadRoot(not);
            }


        }
        private void ReadRoot(SkillORCondition root)
        {
            for (int i = 0; i < root.outPorts.Count; i++)
            {
                var port = root.outPorts[i];
                if (port.connections.Count <= 0) continue;
                if (port.name == nameof(SkillORCondition.Conditions))
                {
                    for (int j = 0; j < port.connections.Count; j++)
                    {
                        var con = port.connections[j];
                        root.Conditions = root.Conditions ?? new();
                        root.Conditions.Add(con.input.node as SkillCondition);
                    }
                }

            }

        }
        private void ReadRoot(SkillAndCondition root)
        {
            for (int i = 0; i < root.outPorts.Count; i++)
            {
                var port = root.outPorts[i];
                if (port.connections.Count <= 0) continue;
                if (port.name == nameof(SkillAndCondition.Conditions))
                {
                    for (int j = 0; j < port.connections.Count; j++)
                    {
                        var con = port.connections[j];
                        root.Conditions = root.Conditions ?? new();
                        root.Conditions.Add(con.input.node as SkillCondition);
                    }
                }

            }

        }
        private void ReadRoot(SkillIFSignal root)
        {
            for (int i = 0; i < root.outPorts.Count; i++)
            {
                var port = root.outPorts[i];
                if (port.connections.Count <= 0) continue;
                if (port.name == nameof(SkillIFSignal.Condition))
                {
                    if (port.connections.Count == 1)
                        root.Condition = port.connections[0].input.node as SkillCondition;


                }
                else if (port.name == nameof(SkillIFSignal.Success))
                {
                    for (int j = 0; j < port.connections.Count; j++)
                    {
                        var con = port.connections[j];
                        root.Success = root.Success ?? new();
                        root.Success.Add(con.input.node as SkillSignal);
                    }
                }
                else if (port.name == nameof(SkillIFSignal.Fail))
                {
                    for (int j = 0; j < port.connections.Count; j++)
                    {
                        var con = port.connections[j];
                        root.Fail = root.Fail ?? new();
                        root.Fail.Add(con.input.node as SkillSignal);
                    }
                }

            }

        }
        private void ReadRoot(SkillNotCondition root)
        {
            for (int i = 0; i < root.outPorts.Count; i++)
            {
                var port = root.outPorts[i];
                if (port.connections.Count <= 0) continue;
                if (port.name == nameof(SkillNotCondition.Condition))
                {
                    if (port.connections.Count == 1)
                        root.Condition = port.connections[0].input.node as SkillCondition;


                }


            }

        }



        private void ReadRoot(SkillForClip root)
        {
            for (int i = 0; i < root.outPorts.Count; i++)
            {
                var port = root.outPorts[i];
                if (port.connections.Count <= 0) continue;
                if (port.name == nameof(SkillForClip.clips))
                {
                    for (int j = 0; j < port.connections.Count; j++)
                    {
                        var con = port.connections[j];
                        root.clips = root.clips ?? new();
                        root.clips.Add(con.input.node as SkillClip);
                    }
                }

            }

        }

        private void ReadRoot(SkillClipSequence root)
        {
            for (int i = 0; i < root.outPorts.Count; i++)
            {
                var port = root.outPorts[i];
                if (port.connections.Count <= 0) continue;

                if (port.name == nameof(SkillClipSequence.conditions))
                {
                    for (int j = 0; j < port.connections.Count; j++)
                    {
                        var con = port.connections[j];
                        root.conditions = root.conditions ?? new();
                        root.conditions.Add(con.input.node as SkillCondition);
                    }
                }
                else if (port.name == nameof(SkillClipSequence.clips))
                {
                    for (int j = 0; j < port.connections.Count; j++)
                    {
                        var con = port.connections[j];
                        root.clips = root.clips ?? new();
                        root.clips.Add(con.input.node as SkillClip);
                    }
                }

            }

        }




        private void ReadRoot(SkillModify root)
        {
            for (int i = 0; i < root.outPorts.Count; i++)
            {
                var port = root.outPorts[i];
                if (port.connections.Count <= 0) continue;

                if (port.name == nameof(SkillModify.property))
                {
                    if (port.connections.Count == 1)
                        root.property = port.connections[0].input.node as SkillModifyProperty;
                }
                else if (port.name == nameof(SkillModify.sets))
                {
                    for (int j = 0; j < port.connections.Count; j++)
                    {
                        var con = port.connections[j];
                        root.sets
                            = root.sets ?? new();
                        root.sets.Add(con.input.node as SkillSetValueSignal);
                    }
                }

            }

        }

        private void ReadRoot(SkillAssetRoot root)
        {
            var names = Enum.GetNames(typeof(SkillEventType));
            for (int i = 0; i < root.outPorts.Count; i++)
            {
                var port = root.outPorts[i];
                if (port.connections.Count <= 0) continue;

                if (port.name == nameof(SkillAsset.property))
                {
                    if (port.connections.Count == 1)
                        this.property = port.connections[0].input.node as SkillPropertyCollection;
                }
                else if (port.name == nameof(SkillAsset.modifies))
                {
                    for (int j = 0; j < port.connections.Count; j++)
                    {
                        var con = port.connections[j];
                        this.modifies = this.modifies ?? new List<SkillModify>();
                        this.modifies.Add(con.input.node as SkillModify);
                    }
                }
                else
                {
                    var _name = string.Empty;

                    for (int j = 0; j < names.Length; j++)
                    {
                        if (names[j] == port.name)
                        {
                            _name = names[j];

                            break;
                        }

                    }
                    if (string.IsNullOrEmpty(_name)) continue;
                    SkillEventType type = (SkillEventType)Enum.Parse(typeof(SkillEventType), _name);
                    seqs = seqs ?? new Dictionary<SkillEventType, List<SkillClipSequence>>();
                    List<SkillClipSequence> list = null;
                    for (int j = 0; j < port.connections.Count; j++)
                    {
                        var conn = port.connections[j];
                        var seq = conn.input.node as SkillClipSequence;
                        list = list ?? new List<SkillClipSequence>();
                        list.Add(seq);
                    }
                    if (list != null)
                        seqs[type] = list;


                }

            }
        }

    }

}


