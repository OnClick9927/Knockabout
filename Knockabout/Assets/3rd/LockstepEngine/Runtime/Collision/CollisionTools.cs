
using System.Collections.Generic;

namespace Lockstep.Collision
{
    /// <summary>
    /// 二维碰撞窄相位与基础几何算法集合。
    /// 包含圆、多边形、AABB/OBB、线段和射线组合；所有运算使用定点类型。
    /// 输出法线通常从第一个形状指向第二个形状或指向被命中表面的外侧，
    /// 调用时应结合具体方法的参数顺序解释方向。
    /// </summary>
    public static class CollisionTools
    {

        /// <summary>
        /// 检测圆与任意简单多边形。依次处理顶点、边投影和圆心位于多边形内部三类情况。
        /// </summary>
        public static bool TestCirclePolygon(LVector2 c, LFloat r, IReadOnlyList<LVector2> points, 
            out LVector2 polygonNormal, out LVector2 contactPoint)
        {
            if (points == null || points.Count < 3)
            {
                polygonNormal = LVector2.zero;
                contactPoint = LVector2.zero;
                return false;
            }

            var vertexCount = points.Count;
            // 初始化法线为零向量（无碰撞时保持零）
            polygonNormal = LVector2.zero;
            contactPoint = LVector2.zero; // 初始化

            var radiusSquared = r * r;
            var circleCenter = c;
            var nearestDistance = LFloat.MaxValue;
            int nearestVertex = -1;

            // 边界检查：防止传入空列表或顶点数不匹配
            if (vertexCount <= 0 || points.Count < vertexCount)
            {
                return false;
            }

            for (var i = 0; i < vertexCount; i++)
            {
                // 替换指针访问 _points[i] 为列表索引访问 points[i]
                LVector2 axis = circleCenter - points[i];
                var distance = axis.sqrMagnitude - radiusSquared;
                if (distance <= 0)
                {
                    // 碰撞在顶点上：法线为从顶点指向圆心的方向（多边形外法线）
                    polygonNormal = axis.normalized;
                    contactPoint = points[i];  // 碰撞点为顶点本身

                    return true;
                }

                if (distance < nearestDistance)
                {
                    nearestVertex = i;
                    nearestDistance = distance;
                }
            }

            // 嵌套方法：安全获取指定索引的顶点（支持循环索引）
            if (IsPointInPolygon(c, points))
            {
                GetNearestPolygonEdge(c, points, out contactPoint, out polygonNormal);
                return true;
            }

            LVector2 GetPoint(int index)
            {
                if (index < 0)
                {
                    index += vertexCount;
                }
                else if (index >= vertexCount)
                {
                    index -= vertexCount;
                }
                // 替换指针访问为列表索引访问
                return points[index];
            }

            var vertex = GetPoint(nearestVertex - 1);
            // 标记是否碰撞在边上，用于后续计算边的法线
            //bool hitOnEdge = false;
            for (var i = 0; i < 2; i++)
            {
                var nextVertex = GetPoint(nearestVertex + i);
                var edge = nextVertex - vertex;
                var edgeLengthSquared = edge.sqrMagnitude;
                if (edgeLengthSquared != 0)
                {
                    LVector2 axis = circleCenter - vertex;
                    var dot = LVector2.Dot(edge, axis);
                    if (dot >= 0 && dot <= edgeLengthSquared)
                    {
                        LVector2 projection = vertex + (dot / edgeLengthSquared) * edge;
                        axis = projection - circleCenter;
                        if (axis.sqrMagnitude <= radiusSquared)
                        {
                            // 碰撞在边上：计算边的垂直法线（多边形外法线）
                            // 边的垂直向量：(edge.y, -edge.x) 或 (-edge.y, edge.x)，需判断朝向
                            LVector2 edgeNormal = new LVector2(edge.y, -edge.x).normalized;
                            // 确保法线指向多边形外侧（朝向圆心方向）
                            if (LVector2.Dot(edgeNormal, circleCenter - vertex) < 0)
                            {
                                edgeNormal = -edgeNormal;
                            }
                            polygonNormal = edgeNormal;
                            contactPoint = projection;  // 碰撞点为边上的投影点

                            //hitOnEdge = true;
                            return true;
                        }
                        else
                        {
                            if (edge.x > 0)
                            {
                                if (axis.y > 0)
                                {
                                    return false;
                                }
                            }
                            else if (edge.x < 0)
                            {
                                if (axis.y < 0)
                                {
                                    return false;
                                }
                            }
                            else if (edge.y > 0)
                            {
                                if (axis.x < 0)
                                {
                                    return false;
                                }
                            }
                            else
                            {
                                if (axis.x > 0)
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }

                vertex = nextVertex;
            }

            // 若未碰撞在边/顶点上，返回false（法线保持零）
            return false; // 原代码此处返回true是逻辑错误，已修正
        }

        private static void GetNearestPolygonEdge(LVector2 point, IReadOnlyList<LVector2> polygon,
            out LVector2 contactPoint, out LVector2 normal)
        {
            contactPoint = LVector2.zero;
            normal = LVector2.zero;
            var nearestDistance = LFloat.MaxValue;
            var nearestStart = LVector2.zero;
            var nearestEnd = LVector2.zero;

            for (int i = 0; i < polygon.Count; i++)
            {
                var start = polygon[i];
                var end = polygon[(i + 1) % polygon.Count];
                var edge = end - start;
                var edgeLengthSquared = edge.sqrMagnitude;
                if (edgeLengthSquared == LFloat.zero)
                    continue;

                var dot = LVector2.Dot(edge, point - start);
                LFloat t;
                if (dot <= LFloat.zero)
                    t = LFloat.zero;
                else if (dot >= edgeLengthSquared)
                    t = LFloat.one;
                else
                    t = dot / edgeLengthSquared;

                var projection = start + edge * t;
                var distanceSquared = (point - projection).sqrMagnitude;
                if (distanceSquared < nearestDistance)
                {
                    nearestDistance = distanceSquared;
                    contactPoint = projection;
                    nearestStart = start;
                    nearestEnd = end;
                }
            }

            if (nearestDistance == LFloat.MaxValue)
                return;

            normal = GetPolygonEdgeNormal(nearestStart, nearestEnd, GetPolygonSignedArea(polygon) > LFloat.zero);
        }

        private static LFloat GetPolygonSignedArea(IReadOnlyList<LVector2> polygon)
        {
            LFloat area = LFloat.zero;
            for (int i = 0; i < polygon.Count; i++)
            {
                int next = (i + 1) % polygon.Count;
                area += polygon[i].x * polygon[next].y - polygon[next].x * polygon[i].y;
            }
            return area;
        }

        private static LVector2 GetPolygonEdgeNormal(LVector2 edgeStart, LVector2 edgeEnd, bool isCounterClockwise)
        {
            var edge = edgeEnd - edgeStart;
            var normal = isCounterClockwise
                ? new LVector2(edge.y, -edge.x)
                : new LVector2(-edge.y, edge.x);
            return normal.normalized;
        }

        private static bool IsPointInPolygon(LVector2 point, IReadOnlyList<LVector2> polygon)
        {
            var minX = polygon[0]._x;
            var maxX = polygon[0]._x;
            var minY = polygon[0]._y;
            var maxY = polygon[0]._y;
            for (int i = 1; i < polygon.Count; i++)
            {
                var p = polygon[i];
                minX = LMath.Min(p._x, minX);
                maxX = LMath.Max(p._x, maxX);
                minY = LMath.Min(p._y, minY);
                maxY = LMath.Max(p._y, maxY);
            }

            if (point._x < minX || point._x > maxX || point._y < minY || point._y > maxY)
                return false;

            var inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                if ((polygon[i]._y > point._y) != (polygon[j]._y > point._y) &&
                    point._x < (polygon[j]._x - polygon[i]._x) * (point._y - polygon[i]._y) /
                    (polygon[j]._y - polygon[i]._y) + polygon[i]._x)
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        /// <summary>返回射线与线段交点沿射线的参数 t；无交点返回负值。</summary>
        public static LFloat TestRaySegment(LVector2 rayOrigin, LVector2 rayDir, LVector2 segStart, LVector2 segEnd)
        {
            // 步骤1：计算基础向量（修正原代码的diff计算逻辑）
            LVector2 segDir = segEnd - segStart;
            LVector2 originToSegStart = segStart - rayOrigin;
            var rayLengthSquared = LVector2.Dot(rayDir, rayDir);
            if (rayLengthSquared <= LFloat.EPSILON)
                return LFloat.negOne;

            var segmentLengthSquared = LVector2.Dot(segDir, segDir);
            if (segmentLengthSquared <= LFloat.EPSILON)
            {
                if (LMath.Abs(LMath.Cross2D(originToSegStart, rayDir)) >= LFloat.EPSILON)
                    return LFloat.negOne;

                var pointT = LVector2.Dot(originToSegStart, rayDir) / rayLengthSquared;
                return pointT >= LFloat.zero ? pointT : LFloat.negOne;
            }

            // 步骤2：二维叉乘（核心：判断射线与线段是否平行）
            LFloat crossRaySeg = LMath.Cross2D(rayDir, segDir);
            if (LMath.Abs(crossRaySeg) < LFloat.EPSILON)
            {
                // 平行时，判断射线起点是否在直线上（额外校验，避免漏判）
                LFloat crossOriginSeg = LMath.Cross2D(originToSegStart, rayDir);
                if (LMath.Abs(crossOriginSeg) < LFloat.EPSILON)
                {
                    // 计算射线起点在seg上的投影参数
                    LFloat t0 = LVector2.Dot(segStart - rayOrigin, rayDir) / rayLengthSquared;
                    LFloat t1 = LVector2.Dot(segEnd - rayOrigin, rayDir) / rayLengthSquared;

                    // 取≥0的最小t值
                    if (t0 > t1)
                        (t0, t1) = (t1, t0);

                    if (t1 < LFloat.zero)
                        return LFloat.negOne;

                    return t0 >= LFloat.zero ? t0 : LFloat.zero;
                }
                return LFloat.negOne;
            }

            // 步骤3：计算参数（修正原代码的分子项顺序，这是失效的核心原因！）
            LFloat t = LMath.Cross2D(originToSegStart, segDir) / crossRaySeg; // 射线参数t
            LFloat s = LMath.Cross2D(originToSegStart, rayDir) / crossRaySeg; // 线段参数s

            // 步骤4：严格校验（t≥0：射线正向；s∈[0,1]：交点在线段上）
            if (t >= 0 && s >= 0 && s <= 1)
            {
                return t;
            }

            return LFloat.negOne;
        }

        /// <summary>
        /// 检测射线与多边形的碰撞（确保能稳定检测出碰撞）
        /// </summary>
        /// <param name="rayOrigin">射线起点</param>
        /// <param name="rayDir">射线方向（自动归一化）</param>
        /// <param name="polygonPoints">多边形顶点（至少3个，闭合）</param>
        /// <param name="hitPoint">输出碰撞点（无碰撞时为zero）</param>
        /// <param name="polygonNormal">输出碰撞法线（无碰撞时为zero）</param>
        /// <returns>是否碰撞</returns>
        public static bool TestRayPolygon(
            LVector2 rayOrigin,
            LVector2 rayDir,
            LVector2[] polygonPoints,
            out LVector2 hitPoint,
            ref LVector2 polygonNormal)
        {
            hitPoint = LVector2.zero;
            polygonNormal = LVector2.zero;
            if (polygonPoints == null || polygonPoints.Length < 3)
                return false;

            // 强制归一化射线方向，避免t值混乱
            LVector2 normalizedRayDir = rayDir.normalized;
            if (normalizedRayDir == LVector2.zero)
                return false;

            LFloat nearestT = LFloat.MaxValue;
            int hitEdgeIndex = -1;
            int vertexCount = polygonPoints.Length;

            for (var i = 0; i < vertexCount; i++)
            {
                var edgeStart = polygonPoints[i];
                var edgeEnd = polygonPoints[(i + 1) % vertexCount];
                var t = TestRaySegment(rayOrigin, normalizedRayDir, edgeStart, edgeEnd);
                if (t >= 0 && t < nearestT)
                {
                    nearestT = t;
                    hitEdgeIndex = i;
                }
            }

            if (hitEdgeIndex >= 0)
            {
                hitPoint = rayOrigin + normalizedRayDir * nearestT;
                var edgeStart = polygonPoints[hitEdgeIndex];
                var edgeEnd = polygonPoints[(hitEdgeIndex + 1) % vertexCount];
                // 【仅修改此处】传入射线原点和归一化方向，用于外法线二次校验
                polygonNormal = CalculatePolygonEdgeNormal(edgeStart, edgeEnd, polygonPoints, rayOrigin, normalizedRayDir);
                return true;
            }

            return false;
        }

        private static LVector2 CalculatePolygonEdgeNormal(LVector2 edgeStart, LVector2 edgeEnd, LVector2[] polygonPoints, LVector2 rayOrigin, LVector2 rayDir)
        {
            LVector2 edgeDir = edgeEnd - edgeStart;
            LFloat polygonArea = 0;
            for (int i = 0; i < polygonPoints.Length; i++)
            {
                int j = (i + 1) % polygonPoints.Length;
                polygonArea += (polygonPoints[i].x * polygonPoints[j].y) - (polygonPoints[j].x * polygonPoints[i].y);
            }
            bool isCounterClockwise = polygonArea > 0;

            LVector2 outerNormal;
            if (isCounterClockwise)
            {
                // 【关键修复】调整y轴符号：原 (edgeDir.y, -edgeDir.x) → 改为 (-edgeDir.y, edgeDir.x)
                outerNormal = new LVector2(-edgeDir.y, edgeDir.x);
            }
            else
            {
                // 【关键修复】调整y轴符号：原 (-edgeDir.y, edgeDir.x) → 改为 (edgeDir.y, -edgeDir.x)
                outerNormal = new LVector2(edgeDir.y, -edgeDir.x);
            }
            outerNormal = outerNormal.normalized;

            // 保留二次校验（确保凹多边形也正确）
            if (LVector2.Dot(outerNormal, rayDir) > 0)
            {
                outerNormal = -outerNormal;
            }

            return outerNormal;
        }























        public static LFloat TestSegmentSegment(LVector2 p0, LVector2 p1, LVector2 p2, LVector2 p3)
        {
            var diff = p2 - p0;
            var d1 = p1 - p0;
            var d2 = p3 - p2;


            var demo = LMath.Cross2D(d1, d2); //det
            if (LMath.Abs(demo) < LFloat.EPSILON) //parallel
                return LFloat.negOne;

            var t1 = LMath.Cross2D(d2, diff) / demo; // Cross2D(diff,-d2)
            var t2 = LMath.Cross2D(d1, diff) / demo; //Dot(v1,pd0) == cross(d0,d1)

            if ((t1 >= 0 && t1 <= 1) && (t2 >= 0 && t2 <= 1))
                return t1; // return p0 + (p1-p0) * t1
            return LFloat.negOne;
        }
        //http://geomalgorithms.com/

        //https://stackoverflow.com/questions/1073336/circle-line-segment-collision-detection-algorithm
        /// <summary>使用二次方程求射线与圆的最近非负交点及圆面外法线。</summary>
        public static bool TestRayCircle(LVector2 cPos, LFloat cR, LVector2 rB, LVector2 rDir, out LVector2 hitPoint, ref LVector2 normal)
        {
            hitPoint = normal = LVector2.zero;
            LFloat t;
            var d = rDir;
            var f = rB - cPos;
            var a = LVector2.Dot(d, d);
            if (a <= LFloat.EPSILON)
                return false;

            var b = 2 * LVector2.Dot(f, d);
            var c = LVector2.Dot(f, f) - cR * cR;
            var discriminant = b * b - 4 * a * c;
            if (discriminant < 0) return false;
            else
            {
                discriminant = LMath.Sqrt(discriminant);
                var t1 = (-b - discriminant) / (2 * a);
                var t2 = (-b + discriminant) / (2 * a);
                // 优先选择最近的有效交点
                if (t1 >= 0) t = t1;
                else if (t2 >= 0) t = t2;
                else return false;

                // 计算碰撞点
                hitPoint = rB + rDir * t;
                // 计算法线：(碰撞点 - 圆心) 归一化（圆形法线指向外）
                normal = (hitPoint - cPos).normalized;

                return true;
            }
        }

        public static bool TestRayOBB(LVector2 o, LVector2 d, LVector2 c, LVector2 size, LFloat deg,
                     out LVector2 hitPoint, ref LVector2 normal)
        {
            hitPoint = LVector2.zero; // 初始化碰撞点
            normal = LVector2.zero;   // 初始化法线

            var fo = o - c;
            fo = fo.Rotate(deg);
            var fd = d.Rotate(deg);

            // 先声明AABB空间的法线和碰撞点，用于临时存储
            LVector2 aabbNormal = LVector2.zero;
            LVector2 aabbHitPoint = LVector2.zero;

            // 调用修改后的TestRayAABB，获取AABB空间的碰撞点和法线
            bool hit = TestRayAABB(fo, fd, -size, size, out aabbHitPoint, out aabbNormal);

            if (hit)
            {
                // 将AABB空间的碰撞点旋转回世界空间（先旋转再平移）
                hitPoint = aabbHitPoint.Rotate(-deg) + c;
                // 将AABB空间的法线旋转回世界空间（抵消OBB的旋转）
                normal = aabbNormal.Rotate(-deg);
                // 法线归一化（修复旋转后长度偏移问题）
                LFloat mag = normal.magnitude;
                if (mag > LFloat.EPSILON)
                {
                    normal = normal / mag;
                }
            }

            return hit;
        }


        // 新增out LVector2 aabbNormal参数返回AABB空间的法线
        // 注意：确保tNear、tFar、isParallel是该方法内的局部变量（若为类成员需确认作用域）
        /// <summary>
        /// 使用 slab 方法检测射线与 AABB，并返回进入点法线；平行轴会单独检查起点范围。
        /// </summary>
        public static bool TestRayAABB(LVector2 o, LVector2 d, LVector2 min, LVector2 max,
                                    out LVector2 hitPoint, out LVector2 aabbNormal)
        {
            hitPoint = LVector2.zero; // 初始化碰撞点
            aabbNormal = LVector2.zero; // 初始化AABB空间法线
            LFloat tmin = LFloat.zero; // 碰撞时间
            LFloat tmax = LFloat.FLT_MAX;
            if (d == LVector2.zero)
                return false;

            LVector2 tNear = LVector2.zero;
            LVector2 tFar = LVector2.zero;
            var isParallelX = false;
            var isParallelY = false;


            // 第一步：基础AABB射线检测（原有逻辑完全保留）
            for (int i = 0; i < 2; i++)
            {
                if (LMath.Abs(d[i]) < LFloat.EPSILON)
                {
                    if (i == 0)
                        isParallelX = true;
                    else
                        isParallelY = true;
                    if (o[i] < min[i] || o[i] > max[i])
                    {
                        return false; // 平行且不在范围内，直接返回
                    }
                }
                else
                {
                    LFloat ood = LFloat.one / d[i];
                    tNear[i] = (min[i] - o[i]) * ood;
                    tFar[i] = (max[i] - o[i]) * ood;

                    if (tNear[i] > tFar[i])
                    {
                        (tNear[i], tFar[i]) = (tFar[i], tNear[i]); // 交换tNear/tFar
                    }

                    tmin = LMath.Max(tmin, tNear[i]);
                    tmax = LMath.Min(tmax, tFar[i]);

                    if (tmin > tmax || tmax < LFloat.zero)
                    {
                        return false; // 无有效碰撞
                    }
                }
            }

            // 第二步：计算AABB空间下的精确碰撞点（原有逻辑完全保留）
            hitPoint = o + d * tmin;

            // 第三步：核心修改 - 仅当射线方向、角点、AABB中心共线时，法线才指向角点
            // 3.1 计算AABB中心（新增）
            LVector2 aabbCenter = (min + max) / 2;
            // 计算AABB半长（每个轴的长度的一半，用于计算角点）
            LVector2 aabbExtents = max - aabbCenter;
            // 3.2 判断是否是角点碰撞（原有逻辑保留）
            bool isCornerHit = true;
            LVector2 cornerDir = LVector2.zero; // 从AABB中心指向角点的向量（单位方向）
            for (int i = 0; i < 2; i++)
            {
                // 检查碰撞点是否在当前轴的边界上（考虑浮点精度）
                bool isOnMinEdge = LMath.Abs(hitPoint[i] - min[i]) <= LFloat.EPSILON * 1000;
                bool isOnMaxEdge = LMath.Abs(hitPoint[i] - max[i]) <= LFloat.EPSILON * 1000;

                if (!isOnMinEdge && !isOnMaxEdge)
                {
                    isCornerHit = false; // 只要有一个轴不在边界上，就不是角点碰撞
                    break;
                }

                // 记录当前轴的角点方向（相对于AABB中心）
                cornerDir[i] = isOnMinEdge ? -LFloat.one : LFloat.one;
            }

            // 3.3 新增：判断射线方向、AABB角点、AABB中心是否共线（核心逻辑）
            bool isCollinear = false;
            if (isCornerHit)
            {
                // 修正：向量×标量（每个轴单独计算），避免向量×向量的错误
                LVector2 cornerPoint = new LVector2(
                    aabbCenter.x + cornerDir.x * aabbExtents.x,
                    aabbCenter.y + cornerDir.y * aabbExtents.y
                );
                // 向量1：AABB中心 -> 角点
                LVector2 dirCenterToCorner = cornerPoint - aabbCenter;
                // 向量2：射线方向（归一化，避免长度影响）
                LVector2 rayDirNormalized = d.normalized;
                // 叉乘判断共线（浮点容错）
                LFloat cross = LMath.Abs(dirCenterToCorner.x * rayDirNormalized.y - dirCenterToCorner.y * rayDirNormalized.x);
                isCollinear = cross <= LFloat.EPSILON * 1000;
            }

            // 3.4 根据规则计算法线（核心修改）
            if (isCornerHit && isCollinear)
            {
                // 仅角点碰撞且三点共线：法线为AABB中心指向角点（归一化）
                aabbNormal = cornerDir.normalized;
            }
            else
            {
                // 非角点碰撞 或 角点但不共线：沿用原有单轴法线逻辑
                for (int i = 0; i < 2; i++)
                {
                    var isParallel = i == 0 ? isParallelX : isParallelY;
                    if (!isParallel && LMath.Abs(tmin - tNear[i]) < LFloat.EPSILON)
                    {
                        aabbNormal[i] = d[i] > 0 ? -LFloat.one : LFloat.one;
                        aabbNormal[1 - i] = LFloat.zero;
                        break;
                    }
                }
            }

            return true;
        }
























        public static bool TestCircleOBB(LVector2 posA, LFloat rA, LVector2 posB, LFloat rB, LVector2 sizeB,
       LVector2 up, out LVector2 obbNormal, out LVector2 contactPoint)
        {
            var diff = posA - posB;
            var allRadius = rA + rB;

            // 原有前置碰撞筛选逻辑保留
            if (diff.sqrMagnitude > allRadius * allRadius)
            {
                obbNormal = LVector2.zero; // 无碰撞时法线置零
                contactPoint = LVector2.zero; // 无碰撞置零

                return false;
            }

            // 空间转换（原有逻辑完全保留）
            LVector2 right = new LVector2(up.y, -up.x); // OBB的右方向（垂直于up）
            var absX = LMath.Abs(LVector2.Dot(diff, right));
            var absY = LMath.Abs(LVector2.Dot(diff, up));
            var size = sizeB;
            var radius = rA;
            var x = LMath.Max(absX - size.x, LFloat.zero);
            var y = LMath.Max(absY - size.y, LFloat.zero);

            // 判断是否真的碰撞
            if (x * x + y * y >= radius * radius)
            {
                obbNormal = LVector2.zero;
                contactPoint = LVector2.zero; // 无碰撞置零

                return false;
            }

            // 核心最小改动：仅在圆心-OBB中心-角点共线时，法线才指向角点
            obbNormal = LVector2.zero;
            LFloat distX = absX - size.x;
            LFloat distY = absY - size.y;

            // 新增：先计算角点坐标（仅角碰撞时用到）
            LVector2 cornerPoint = LVector2.zero;
            bool isCollinear = false;
            // 仅角碰撞时才判断共线
            if (distX >= LFloat.EPSILON * 1000 && distY >= LFloat.EPSILON * 1000)
            {
                LFloat signX = LMath.Sign(LVector2.Dot(diff, right));
                LFloat signY = LMath.Sign(LVector2.Dot(diff, up));
                cornerPoint = posB + right * size.x * signX + up * size.y * signY;

                // 判断：圆心(posA)、角点(cornerPoint)、OBB中心(posB)是否共线（浮点精度容错）
                // 共线条件：向量(posB->cornerPoint) 与 向量(posB->posA) 的叉乘绝对值小于极小值
                LVector2 dirToCorner = cornerPoint - posB;
                LVector2 dirToCircle = posA - posB;
                LFloat cross = LMath.Abs(dirToCorner.x * dirToCircle.y - dirToCorner.y * dirToCircle.x);
                isCollinear = cross <= LFloat.EPSILON * 1000; // 放宽一点容错，避免精度问题
            }

            // 核心逻辑：仅共线时返回OBB中心指向角点的法线，否则沿用原有逻辑
            if (isCollinear)
            {
                obbNormal = (cornerPoint - posB).normalized; // OBB中心指向角点
            }
            else if (distX > distY)
            {
                // 原有X轴法线逻辑保留
                LFloat sign = LMath.Sign(LVector2.Dot(diff, right));
                obbNormal = right * sign;
            }
            else
            {
                // 原有Y轴法线逻辑保留
                LFloat sign = LMath.Sign(LVector2.Dot(diff, up));
                obbNormal = up * sign;
            }

            LFloat signR = LMath.Sign(LVector2.Dot(obbNormal, right));
            LFloat signU = LMath.Sign(LVector2.Dot(obbNormal, up));
            contactPoint = posB + right * signR * size.x + up * signU * size.y;

            return true;
        }

        public static bool TestAABBOBB(LVector2 posA, LFloat rA, LVector2 sizeA, LVector2 posB, LFloat rB,
            LVector2 sizeB,
            LVector2 upB)
        {
            var diff = posA - posB;
            var allRadius = rA + rB;
            //circle 判定
            if (diff.sqrMagnitude > allRadius * allRadius)
            {
                return false;
            }

            var absUPX = LMath.Abs(upB.x); //abs(up dot aabb.right)
            var absUPY = LMath.Abs(upB.y); //abs(right dot aabb.right)
            {
                //轴 投影 AABBx
                var distX = absUPX * sizeB.y + absUPY * sizeB.x;
                if (LMath.Abs(diff.x) > distX + sizeA.x)
                {
                    return false;
                }

                //轴 投影 AABBy
                //absUPX is abs(right dot aabb.up)
                //absUPY is abs(up dot aabb.up)
                var distY = absUPY * sizeB.y + absUPX * sizeB.x;
                if (LMath.Abs(diff.y) > distY + sizeA.y)
                {
                    return false;
                }
            }
            {
                var right = new LVector2(upB.y, -upB.x);
                var diffPObbX = LVector2.Dot(diff, right);
                var diffPObbY = LVector2.Dot(diff, upB);

                //absUPX is abs(aabb.up dot right )
                //absUPY is abs(aabb.right dot right)
                //轴 投影 OBBx
                var distX = absUPX * sizeA.y + absUPY * sizeA.x;
                if (LMath.Abs(diffPObbX) > distX + sizeB.x)
                {
                    return false;
                }

                //absUPX is abs(aabb.right dot up )
                //absUPY is abs(aabb.up dot up)
                //轴 投影 OBBy
                var distY = absUPY * sizeA.y + absUPX * sizeA.x;
                if (LMath.Abs(diffPObbY) > distY + sizeB.y)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>使用分离轴定理检测两个 OBB，并返回最小穿透轴作为接触法线。</summary>
        public static bool TestOBBOBB(LVector2 posA, LFloat rA, LVector2 sizeA, LVector2 upA,
                                      LVector2 posB, LFloat rB, LVector2 sizeB, LVector2 upB,
                                      out LVector2 normal, out LVector2 contactPoint)
        {
            normal = LVector2.zero;
            contactPoint = LVector2.zero;
            var diff = posA - posB;
            var allRadius = rA + rB;
            if (diff.sqrMagnitude > allRadius * allRadius)
                return false;

            var rightA = new LVector2(upA.y, -upA.x);
            var rightB = new LVector2(upB.y, -upB.x);

            var minPenetration = LFloat.MaxValue;
            LVector2 bestAxis = LVector2.zero; // 带符号的单位轴（指向 diff 方向）

            // ── 轴 A.right ──
            {
                var BuProjAr = LMath.Abs(LVector2.Dot(upB, rightA));
                var BrProjAr = LMath.Abs(LVector2.Dot(rightB, rightA));
                var DiffProjAr = LMath.Abs(LVector2.Dot(diff, rightA));
                var distX = BuProjAr * sizeB.y + BrProjAr * sizeB.x;
                if (DiffProjAr > distX + sizeA.x)
                    return false;
                var penetration = (distX + sizeA.x) - DiffProjAr;
                if (penetration < minPenetration)
                {
                    minPenetration = penetration;
                    var sign = LMath.Sign(LVector2.Dot(diff, rightA));
                    bestAxis = rightA * sign;
                }
            }

            // ── 轴 A.up ──
            {
                var BuProjAu = LMath.Abs(LVector2.Dot(upB, upA));
                var BrProjAu = LMath.Abs(LVector2.Dot(rightB, upA));
                var DiffProjAu = LMath.Abs(LVector2.Dot(diff, upA));
                var distY = BuProjAu * sizeB.y + BrProjAu * sizeB.x;
                if (DiffProjAu > distY + sizeA.y)
                    return false;
                var penetration = (distY + sizeA.y) - DiffProjAu;
                if (penetration < minPenetration)
                {
                    minPenetration = penetration;
                    var sign = LMath.Sign(LVector2.Dot(diff, upA));
                    bestAxis = upA * sign;
                }
            }

            // ── 轴 B.right ──
            {
                var AuProjBr = LMath.Abs(LVector2.Dot(upA, rightB));
                var ArProjBr = LMath.Abs(LVector2.Dot(rightA, rightB));
                var DiffProjBr = LMath.Abs(LVector2.Dot(diff, rightB));
                var distX = AuProjBr * sizeA.y + ArProjBr * sizeA.x;
                if (DiffProjBr > distX + sizeB.x)
                    return false;
                var penetration = (distX + sizeB.x) - DiffProjBr;
                if (penetration < minPenetration)
                {
                    minPenetration = penetration;
                    var sign = LMath.Sign(LVector2.Dot(diff, rightB));
                    bestAxis = rightB * sign;
                }
            }

            // ── 轴 B.up ──
            {
                var AuProjBu = LMath.Abs(LVector2.Dot(upA, upB));
                var ArProjBu = LMath.Abs(LVector2.Dot(rightA, upB));
                var DiffProjBu = LMath.Abs(LVector2.Dot(diff, upB));
                var distY = AuProjBu * sizeA.y + ArProjBu * sizeA.x;
                if (DiffProjBu > distY + sizeB.y)
                    return false;
                var penetration = (distY + sizeB.y) - DiffProjBu;
                if (penetration < minPenetration)
                {
                    minPenetration = penetration;
                    var sign = LMath.Sign(LVector2.Dot(diff, upB));
                    bestAxis = upB * sign;
                }
            }

            // 碰撞已确认，计算法线
            if (bestAxis.sqrMagnitude > LFloat.EPSILON)
                normal = bestAxis.normalized;
            else
                normal = new LVector2(1, 0); // 默认（例如两中心完全重合）

            // 计算 B 的支撑点（沿法线方向最远的边界点）
            LFloat signR = LMath.Sign(LVector2.Dot(normal, rightB));
            LFloat signU = LMath.Sign(LVector2.Dot(normal, upB));
            contactPoint = posB + rightB * signR * sizeB.x + upB * signU * sizeB.y;

            return true;
        }

        public static bool TestCircleCircle(LVector2 posA, LFloat rA, LVector2 posB, LFloat rB, out LVector2 normalB, 
            out LVector2 contactPoint)
        {
            var diff = posA - posB;
            var allRadius = rA + rB;

            // 原有碰撞检测逻辑完全保留
            if (diff.sqrMagnitude > allRadius * allRadius)
            {
                // 无碰撞时法线置零
                normalB = LVector2.zero;
                contactPoint = LVector2.zero;
                return false;
            }

            // 计算圆B的碰撞法线（核心新增逻辑）
            if (diff.sqrMagnitude < LFloat.EPSILON)
            {
                // 特殊情况：两圆圆心重合，默认法线方向（如向上）
                normalB = new LVector2(0, 1);
            }
            else
            {
                // 圆B的法线：从B指向A的方向归一化（圆B的外法线，指向碰撞方向）
                normalB = diff.normalized;
            }
            // 返回碰撞点：圆B表面沿法线方向的点
            contactPoint = posB + normalB * rB;
            return true;
        }
        public static bool TestCircleAABB(LVector2 posA, LFloat rA, LVector2 posB, LFloat rB, LVector2 sizeB)
        {
            var diff = posA - posB;
            var allRadius = rA + rB;
            //circle 判定
            if (diff.sqrMagnitude > allRadius * allRadius)
            {
                return false;
            }

            var absX = LMath.Abs(diff.x);
            var absY = LMath.Abs(diff.y);

            //AABB & circle
            var size = sizeB;
            var radius = rA;
            var x = LMath.Max(absX - size.x, LFloat.zero);
            var y = LMath.Max(absY - size.y, LFloat.zero);
            return x * x + y * y < radius * radius;
        }

        public static bool TestAABBAABB(LVector2 posA, LFloat rA, LVector2 sizeA, LVector2 posB, LFloat rB,
            LVector2 sizeB)
        {
            var diff = posA - posB;
            var allRadius = rA + rB;
            //circle 判定
            if (diff.sqrMagnitude > allRadius * allRadius)
            {
                return false;
            }

            var absX = LMath.Abs(diff.x);
            var absY = LMath.Abs(diff.y);

            //AABB and AABB
            var allSize = sizeA + sizeB;
            if (absX > allSize.x) return false;
            if (absY > allSize.y) return false;
            return true;
        }

        /// <summary>
        /// 判定两线段是否相交 并求交点
        /// https://stackoverflow.com/questions/563198/how-do-you-detect-where-two-line-segments-intersect/565282#
        /// </summary>
        public static bool IntersectSegment(ref LVector2 seg1Src, ref LVector2 seg1Vec, ref LVector2 seg2Src,
            ref LVector2 seg2Vec, out LVector2 interPoint)
        {
            interPoint = LVector2.zero;
            long denom = (long)seg1Vec._x * seg2Vec._y - (long)seg2Vec._x * seg1Vec._y; //sacle LFloat.Precision
            if (denom == 0L)
                return false; // Collinear
            bool denomPositive = denom > 0L;
            var s02_x = seg1Src._x - seg2Src._x;
            var s02_y = seg1Src._y - seg2Src._y;
            long s_numer = (long)seg1Vec._x * s02_y - (long)seg1Vec._y * s02_x; //scale LFloat.Precision
            if ((s_numer < 0L) == denomPositive)
                return false; // No collision
            long t_numer = seg2Vec._x * s02_y - seg2Vec._y * s02_x; //scale LFloat.Precision
            if ((t_numer < 0L) == denomPositive)
                return false; // No collision
            if (((s_numer > denom) == denomPositive) || ((t_numer > denom) == denomPositive))
                return false; // No collisionR
                              // Collision detected
            var t = t_numer * LFloat.Precision / denom; //sacle LFloat.Precision
            interPoint._x = seg1Src._x + ((long)((t * seg1Vec._x)) / LFloat.Precision);
            interPoint._y = seg1Src._y + ((long)((t * seg1Vec._y)) / LFloat.Precision);
            return true;
        }

        /// <summary>
        ///  判定点是否在多边形内
        /// https://stackoverflow.com/questions/217578/how-can-i-determine-whether-a-2d-point-is-within-a-polygon
        /// </summary>
        public static bool IsPointInPolygon(LVector2 p, LVector2[] polygon)
        {
            if (polygon == null || polygon.Length < 3)
                return false;

            var minX = polygon[0]._x;
            var maxX = polygon[0]._x;
            var minY = polygon[0]._y;
            var maxY = polygon[0]._y;
            for (int i = 1; i < polygon.Length; i++)
            {
                LVector2 q = polygon[i];
                minX = LMath.Min(q._x, minX);
                maxX = LMath.Max(q._x, maxX);
                minY = LMath.Min(q._y, minY);
                maxY = LMath.Max(q._y, maxY);
            }

            if (p._x < minX || p._x > maxX || p._y < minY || p._y > maxY)
            {
                return false;
            }

            // http://www.ecse.rpi.edu/Homepages/wrf/Research/Short_Notes/pnpoly.html
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                if ((polygon[i]._y > p._y) != (polygon[j]._y > p._y) &&
                    p._x < (polygon[j]._x - polygon[i]._x) * (p._y - polygon[i]._y) /
                    (polygon[j]._y - polygon[i]._y) +
                    polygon[i]._x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }
    }

}
