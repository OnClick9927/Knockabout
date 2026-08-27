using System;
using System.Collections.Generic;
using Lockstep;
using Lockstep.Collision;
using UnityEngine;
using UnityEngine.Serialization;

namespace LockstepExamples.CollisionDemo2D
{
    /// <summary>
    /// LockstepEngine 2D 碰撞与射线查询示例。
    /// <para>
    /// 示例运行在 XZ 平面：每帧把场景对象同步到 CollisionTree，检测 OBB 对 OBB 重叠，
    /// 再发射一条有限长度射线。颜色、交点、法线和碰撞盒关系全部来自锁步定点碰撞系统，
    /// 不依赖 Unity Physics。
    /// </para>
    /// </summary>
    public class Test : MonoBehaviour
    {
        [Serializable]
        private struct BoxOverlapDebug
        {
            public Agent a;
            public Agent b;
            public LVector2 normal;

            public BoxOverlapDebug(Agent a, Agent b, LVector2 normal)
            {
                this.a = a;
                this.b = b;
                this.normal = normal;
            }
        }

        [Header("示例对象")]
        [SerializeField] private List<Agent> collisions = new List<Agent>();

        [Header("碰撞盒检测演示")]
        [Tooltip("让列表中的第一个 OBB 沿世界 Z 轴往返移动，以持续展示 OBB 对 OBB 碰撞。")]
        [SerializeField] private bool animateBox = true;
        [SerializeField, Min(0f)] private float boxMoveDistance = 2.4f;
        [SerializeField, Min(0f)] private float boxMoveSpeed = 1f;

        [Header("射线检测演示")]
        [FormerlySerializedAs("start")]
        [Tooltip("射线的世界空间起点；查询会投影到 XZ 平面。")]
        [SerializeField] private Vector3 rayOrigin = new Vector3(-4f, 0f, 1f);
        [FormerlySerializedAs("dir")]
        [Tooltip("射线方向，不要求归一化；Y 分量会被忽略。")]
        [SerializeField] private Vector3 rayDirection = Vector3.right;
        [Tooltip("CollisionTree 提供无限射线，本示例按命中距离裁剪为有限射线。")]
        [SerializeField, Min(0f)] private float rayDistance = 12f;

        [Header("调试显示")]
        [SerializeField] private bool drawCollisionTree;

        // 查询和调试集合贯穿示例生命周期复用，Update 不创建临时结果列表。
        private readonly List<CollisionResult> overlapResults = new List<CollisionResult>();
        private readonly List<RayCastResult> rayResults = new List<RayCastResult>();
        private readonly List<BoxOverlapDebug> boxOverlaps = new List<BoxOverlapDebug>();

        private CollisionTree tree;
        private Agent movingBox;
        private Vector3 movingBoxOrigin;

        private void Start()
        {
            // 初始根区域只影响第一次节点划分；CollisionTree 会在对象越界时自动扩张。
            tree = new CollisionTree(
                new LRect(
                    LMath.ToLFloat(-8f),
                    LMath.ToLFloat(-8f),
                    LMath.ToLFloat(16f),
                    LMath.ToLFloat(16f)),
                CollisionType.XZ);

            CollisionLayer layer = CollisionLayer.Get(0);
            for (int i = 0; i < collisions.Count; i++)
            {
                Agent shape = collisions[i];
                if (shape == null)
                    throw new InvalidOperationException($"Collision example item {i} is null.");

                shape.Configure(i);
                tree.Add(shape.Create(layer));
                shape.ResetState();

                if (movingBox == null && shape.shapeType == Agent.Shape.OBB)
                {
                    movingBox = shape;
                    movingBoxOrigin = shape.transform.position;
                }
            }
        }

        private void Update()
        {
            if (tree == null) return;

            AnimateBox();
            for (int i = 0; i < collisions.Count; i++)
            {
                Agent shape = collisions[i];
                if (shape == null) continue;
                shape.Sync();
                shape.ResetState();
            }

            // 所有代理写入完毕后统一更新树，随后两类查询读取完全相同的本帧状态。
            tree.Update();
            UpdateBoxOverlapExample();
            UpdateRayCastExample();
        }

        /// <summary>只检测 OBB 对 OBB；圆和多边形仍会参与后续射线查询。</summary>
        private void UpdateBoxOverlapExample()
        {
            boxOverlaps.Clear();
            for (int i = 0; i < collisions.Count; i++)
            {
                Agent box = collisions[i];
                if (box == null || box.agent == null || box.shapeType != Agent.Shape.OBB)
                    continue;

                tree.OverLap(box.agent.collision, overlapResults);
                for (int j = 0; j < overlapResults.Count; j++)
                {
                    CollisionResult result = overlapResults[j];
                    Agent other = result.agent.userData as Agent;
                    if (other == null || other.shapeType != Agent.Shape.OBB)
                        continue;

                    box.SetBoxOverlap();
                    other.SetBoxOverlap();

                    // A 查 B 与 B 查 A 会返回同一对盒体，只按稳定序号保存其中一次。
                    if (box.index < other.index)
                        boxOverlaps.Add(new BoxOverlapDebug(box, other, result.normal));
                }
            }
        }

        /// <summary>执行无限射线查询，再按 Inspector 配置的最大距离裁剪为有限射线结果。</summary>
        private void UpdateRayCastExample()
        {
            LVector2 origin = rayOrigin.ToLVector2XZ();
            LVector2 direction = rayDirection.ToLVector2XZ();
            LFloat maxDistance = LMath.ToLFloat(Mathf.Max(0f, rayDistance));
            tree.RayCast(origin, direction, rayResults);

            // RayCast 已按距离升序排列，因此从末尾删除超距项不会改变剩余结果顺序。
            for (int i = rayResults.Count - 1; i >= 0; i--)
            {
                if (rayResults[i].dis <= maxDistance) break;
                rayResults.RemoveAt(i);
            }

            for (int i = 0; i < rayResults.Count; i++)
            {
                Agent shape = rayResults[i].agent.userData as Agent;
                if (shape != null)
                    shape.SetRayHit(i == 0);
            }
        }

        private void AnimateBox()
        {
            if (!animateBox || movingBox == null) return;

            Vector3 position = movingBoxOrigin;
            position.z += Mathf.PingPong(
                Time.time * Mathf.Max(0f, boxMoveSpeed),
                Mathf.Max(0f, boxMoveDistance));
            movingBox.transform.position = position;
        }

        private void OnDrawGizmos()
        {
            DrawRayGizmos();
            if (tree == null) return;

            if (drawCollisionTree)
                tree.DrawGizmos();

            // 红线连接当前重叠的两个 OBB，青色箭头显示窄相位返回的接触法线。
            for (int i = 0; i < boxOverlaps.Count; i++)
            {
                BoxOverlapDebug overlap = boxOverlaps[i];
                if (overlap.a == null || overlap.b == null) continue;

                Vector3 pointA = WithHeight(overlap.a.transform.position, 0.18f);
                Vector3 pointB = WithHeight(overlap.b.transform.position, 0.18f);
                Vector3 middle = (pointA + pointB) * 0.5f;
                Gizmos.color = new Color(0.92f, 0.22f, 0.16f, 1f);
                Gizmos.DrawLine(pointA, pointB);
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(middle, ToWorldDirection(overlap.normal) * 0.75f);
            }
        }

        private void DrawRayGizmos()
        {
            Vector3 planarDirection = new Vector3(rayDirection.x, 0f, rayDirection.z);
            Vector3 normalized = planarDirection.sqrMagnitude > Mathf.Epsilon
                ? planarDirection.normalized
                : Vector3.zero;
            float distance = Mathf.Max(0f, rayDistance);
            Vector3 start = WithHeight(rayOrigin, 0.12f);
            Vector3 end = start + normalized * distance;

            Gizmos.color = Color.white;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(start, 0.08f);

            if (rayResults.Count > 0)
            {
                Gizmos.color = new Color(1f, 0.2f, 0.72f, 1f);
                Gizmos.DrawLine(start, ToWorldPoint(rayResults[0].hitPoint, 0.12f));
            }

            for (int i = 0; i < rayResults.Count; i++)
            {
                RayCastResult result = rayResults[i];
                Vector3 point = ToWorldPoint(result.hitPoint, 0.12f);
                Gizmos.color = i == 0
                    ? new Color(1f, 0.2f, 0.72f, 1f)
                    : new Color(1f, 0.72f, 0.12f, 1f);
                Gizmos.DrawSphere(point, 0.09f);
                Gizmos.DrawRay(point, ToWorldDirection(result.normal) * 0.65f);
            }
        }

        private void OnDestroy()
        {
            if (tree != null)
            {
                // Clear 会回收代理及其 Collision；组件只需随后清除失效引用。
                tree.Clear();
                tree = null;
            }

            for (int i = 0; i < collisions.Count; i++)
            {
                if (collisions[i] != null)
                    collisions[i].ReleaseAgent();
            }
        }

        private static Vector3 ToWorldPoint(LVector2 point, float height)
        {
            return new Vector3(point.x.ToFloat(), height, point.y.ToFloat());
        }

        private static Vector3 ToWorldDirection(LVector2 direction)
        {
            return new Vector3(direction.x.ToFloat(), 0f, direction.y.ToFloat());
        }

        private static Vector3 WithHeight(Vector3 point, float height)
        {
            point.y = height;
            return point;
        }
    }
}
