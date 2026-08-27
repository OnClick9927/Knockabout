
using System;
using System.Collections.Generic;


namespace Lockstep.Collision
{
    /// <summary>二维碰撞数据映射到 Unity 世界时使用 XY 平面或 XZ 平面。</summary>
    public enum CollisionType
    {
        XY, XZ
    }

    /// <summary>
    /// 可动态扩容的二维四叉碰撞树。
    /// 根范围不足时按代理所在象限向外翻倍；代理移动后由 Update 批量迁移，
    /// 查询支持层过滤、业务过滤、重叠、射线和最近邻，并复用调用方结果列表减少 GC。
    /// </summary>
    public class CollisionTree : CollisionTreeBase<CollisionAgent, Collision>
    {
        public const int AgentMaxCountPerCell = 4;
        public static LFloat CellMinSize = LFloat.one;
        public CollisionType type;
        [Flags]
        enum Grow
        {
            None = 0,
            Left = 2,
            Right = 4,
            Down = 16,
            Up = 8,
            LeftUp = Left | Up,
            LeftDown = Left | Down,
            RightDown = Right | Down,
            RightUp = Right | Up

        }
        private CollisionNode root;
        private readonly List<CollisionAgent> moved = new List<CollisionAgent>();
        private bool needCalcBounds = false;
        public CollisionTree(LRect rect, CollisionType type)
        {
            root = CollisionNode.New(rect, this);
            this.type = type;
        }

        /// <summary>移除并回收全部代理与节点，树对象本身仍可继续使用。</summary>
        public void Clear()
        {
            for (int i = agents.Count - 1; i >= 0; i--)
                Remove(agents[i]);
            Update();
        }

        private static void GetX(LRect rect, Grow mainChildIndexByte, out LFloat xMin, out LFloat xMiddle, out LFloat xMax)
        {
            if ((mainChildIndexByte & Grow.Right) != 0)
            {
                xMin = rect.x;
                xMiddle = rect.xMax;
                xMax = rect.xMax + rect.width;
            }
            else
            {
                xMin = rect.x - rect.width;
                xMiddle = rect.x;
                xMax = rect.xMax;
            }
        }
        private static void GetY(LRect rect, Grow mainChildIndexByte, out LFloat yMin, out LFloat yMiddle, out LFloat yMax)
        {

            if ((mainChildIndexByte & Grow.Down) != 0)
            {
                yMin = rect.y;
                yMiddle = rect.yMax;
                yMax = rect.yMax + rect.height;
            }
            else
            {
                yMin = rect.y - rect.height;
                yMiddle = rect.y;
                yMax = rect.yMax;
            }
        }





        private static LRect GetFirstChildRect(CollisionNode root, LRect rect, Grow dir)
        {
            // 获取生长后的四叉树所需的 xy 轴的 最小、中间、最大 坐标
            GetX(rect, dir, out LFloat xMin, out LFloat xMiddle, out LFloat xMax);
            GetY(rect, dir, out LFloat yMin, out LFloat yMiddle, out LFloat yMax);
            var width = rect.width;
            var height = rect.height;
            return dir == Grow.RightDown ? root.rect : new LRect(xMin, yMin, width, height);

        }
        private static void GetChildren(
            CollisionNode root,
            LRect rect,
            Grow dir,
            CollisionTree tree,
            List<CollisionNode> result)
        {
            // 获取生长后的四叉树所需的 xy 轴的 最小、中间、最大 坐标
            GetX(rect, dir, out LFloat xMin, out LFloat xMiddle, out LFloat xMax);
            GetY(rect, dir, out LFloat yMin, out LFloat yMiddle, out LFloat yMax);

            var width = rect.width;
            var height = rect.height;
            result.Clear();
            result.Add(
                // 左上
                dir ==  Grow.RightDown ? root : CollisionNode.New(new LRect(xMin, yMin, width, height), tree));
            result.Add(
                //右上
                dir ==  Grow.LeftDown ? root : CollisionNode.New(new LRect(xMiddle, yMin, width, height), tree));
            result.Add(
                // 右下
                dir == Grow.LeftUp ? root : CollisionNode.New(new LRect(xMiddle, yMiddle, width, height), tree));
            result.Add(
                //左下
                dir == Grow.RightUp ? root : CollisionNode.New(new LRect(xMin, yMiddle, width, height), tree));
        }
        private static Grow GetMainChildIndexByte(LRect rect, LVector2 pos)
        {
            Grow indexByte = Grow.None;
            if (pos.x < rect.x)//左
                indexByte |= Grow.Left;
            else
                indexByte |= Grow.Right;
            if (pos.y < rect.y)//上
                indexByte |= Grow.Up;
            else
                indexByte |= Grow.Down;
            return indexByte;
        }
        /// <summary>
        /// 把未归属其他树的代理加入四叉树；必要时持续扩张根节点直到能够容纳位置。
        /// </summary>
        public void Add(CollisionAgent agent)
        {
            if (agent == null || agent.collision == null)
            {
                Debug.LogError("Cannot add an uninitialized collision agent.");
                return;
            }
            if (agent.node != null || agent.treeIndex >= 0)
            {
                Debug.LogError("Collision agent already belongs to a collision tree.");
                return;
            }

        Again:
            if (root.ContainsPoint(agent.pos))
            {
                root.AddAgent(agent);
                needCalcBounds = true;
                RegisterAgent(agent);
            }
            else
            {
                var rect = root.rect;
                var count = root.GetAgentCount();
                var dir = GetMainChildIndexByte(rect, agent.pos);
                if (AgentMaxCountPerCell < count)
                {
                    List<CollisionNode> children = StaticPool.Get<List<CollisionNode>>();
                    try
                    {
                        GetChildren(root, rect, dir, this, children);
                        // 创建新的四叉树根节点的区域
                        LRect newRootArea = new LRect(children[0].rect.position, children[0].rect.size * 2);
                        CollisionNode newRoot = CollisionNode.New(newRootArea, this);
                        newRoot.Read(children);
                        newRoot.SetChildrenParentAsThis();
                        root = newRoot;
                    }
                    finally
                    {
                        children.Clear();
                        StaticPool.Set(children);
                    }
                }
                else
                {
                    var oldRoot = root;
                    var agents = oldRoot.agents;
                    var childRect = GetFirstChildRect(root, rect, dir);
                    LRect newRootArea = new LRect(childRect.position, childRect.size * 2); ;
                    CollisionNode newRoot = CollisionNode.New(newRootArea, this);
                    root = newRoot;
                    for (int i = 0; i < agents.Count; i++)
                        newRoot.AddAgent(agents[i]);
                    oldRoot.Cycle();
                }
                goto Again;

            }
        }
        private bool _Remove(CollisionAgent agent)
        {
            if (agent == null)
                return false;

            if (!IsRegistered(agent))
            {
                Debug.LogError("Collision agent index is inconsistent with its tree membership.");
                return false;
            }

            if (!root.Remove(agent))
            {
                Debug.LogError("Collision tree membership is inconsistent with its nodes.");
                return false;
            }

            UnregisterAgentSwapBack(agent);
            needCalcBounds = true;
            return true;
        }
        public void Remove(CollisionAgent agent)
        {
            if (!_Remove(agent))
            {
                Debug.LogError("Cannot remove a collision agent that is not in this tree.");
                return;
            }
            agent.Cycle();
        }

        /// <summary>
        /// 处理本帧尺寸和位置脏标记，迁移越出原节点的代理，并统一重算动态 bounds。
        /// 所有查询前应先调用一次。
        /// </summary>
        public override void Update()
        {
            moved.Clear();
            for (int i = 0; i < agents.Count; i++)
            {
                var agent = agents[i];
                if (agent.RadiusChanged || agent.Moved)
                {
                    agent.collision.CalcBounds();

                    if (agent.RadiusChanged)
                    {
                        needCalcBounds = true;
                        agent.RadiusChanged = false;
                    }
                    if (agent.Moved)
                    {
                        needCalcBounds = true;
                        if (agent.node == null || !agent.node.StillContains(agent))
                            moved.Add(agent);
                        else
                            agent.Moved = false;
                    }
                }

            }
            for (int i = 0; i < moved.Count; i++)
            {
                var agent = moved[i];
                if (_Remove(agent))
                    Add(agent);
                agent.Moved = false;
            }

            if (needCalcBounds)
            {
                root.CalcBounds();
                needCalcBounds = false;
            }



        }

        /// <summary>查询与指定形状重叠的全部代理，并按距离从近到远排序。</summary>
        public List<CollisionResult> OverLap(Collision collision, List<CollisionResult> result,
            Func<CollisionAgent, bool> fit = null, params int[] layers)
        {
            result = result ?? new List<CollisionResult>();
            result.Clear();
            result = root.OverLap(collision, fit, result, layers);
            if (result.Count > 1)
                result.Sort(static (x, y) => x.dis.CompareTo(y.dis));
            return result;
        }

        /// <summary>发射无限长射线，方向会归一化，结果按命中距离从近到远排序。</summary>
        public List<RayCastResult> RayCast(LVector2 o, LVector2 d, List<RayCastResult> result,
            Func<CollisionAgent, bool> fit = null, params int[] layers)
        {
            result = result ?? new List<RayCastResult>();
            result.Clear();
            var normalized = d.normalized;
            if (normalized == LVector2.zero) return result;

            result = root.RayCast(o, normalized, fit, result, layers);
            if (result.Count > 1)
                result.Sort(static (x, y) => x.dis.CompareTo(y.dis));
            return result;
        }

        public CollisionAgent Nearest(LVector2 point, ref LFloat dis, Func<CollisionAgent, bool> fit = null, params int[] layers)
        {
            CollisionAgent result = null;
            root.SearchNearest(point, ref dis, ref result, fit, layers);
            return result;
        }

        public void DrawGizmos() { root.DrawGizmos(); }


    }

}
