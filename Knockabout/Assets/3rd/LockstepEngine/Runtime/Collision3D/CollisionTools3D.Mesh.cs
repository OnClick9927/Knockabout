namespace Lockstep.Collision
{
    /// <summary>
    /// CollisionTools3D 的网格组合部分。
    /// 逐三角形执行基础形状窄相位，并以穿透深度和三角形序号稳定选择最佳接触；
    /// 网格-网格还会执行三角形 SAT 与边对边最近点计算。
    /// </summary>
    public static partial class CollisionTools3D
    {
        /// <summary>遍历网格三角形，选择球与三角形穿透最深的稳定接触。</summary>
        public static bool TestSphereMesh(
            SphereCollision3D sphere, MeshCollision3D mesh, out CollisionContact3D contact)
        {
            contact = default(CollisionContact3D);
            var found = false;
            var bestPenetration = LFloat.MinValue;
            var vertices = mesh.worldVertices;
            var triangles = mesh.triangles;
            for (var triangle = 0; triangle < mesh.triangleCount; triangle++)
            {
                var offset = triangle * 3;
                var a = vertices[triangles[offset]];
                var b = vertices[triangles[offset + 1]];
                var c = vertices[triangles[offset + 2]];
                if (IsDegenerateTriangle(a, b, c)) continue;

                var pointTriangle = ClosestPointTriangle(sphere.pos, a, b, c);
                var delta = pointTriangle - sphere.pos;
                var sqrDistance = delta.sqrMagnitude;
                if (sqrDistance > sphere.radius * sphere.radius) continue;

                var distance = LMath.Sqrt(sqrDistance);
                var normal = NormalizeOr(delta, TriangleNormalToSurface(a, b, c, sphere.pos));
                var penetration = LMath.Max(LFloat.zero, sphere.radius - distance);
                if (!ShouldReplace(found, penetration, triangle, bestPenetration, contact.featureB)) continue;

                found = true;
                bestPenetration = penetration;
                contact = new CollisionContact3D(
                    normal,
                    sphere.pos + normal * sphere.radius,
                    pointTriangle,
                    penetration,
                    -1,
                    triangle);
            }
            return found;
        }

        /// <summary>比较胶囊轴线段与各三角形的最近点，再扩张胶囊半径。</summary>
        public static bool TestCapsuleMesh(
            CapsuleCollision3D capsule, MeshCollision3D mesh, out CollisionContact3D contact)
        {
            contact = default(CollisionContact3D);
            var found = false;
            var bestPenetration = LFloat.MinValue;
            var vertices = mesh.worldVertices;
            var triangles = mesh.triangles;
            var capsuleCenter = LVector3.Average(capsule.pointA, capsule.pointB);
            for (var triangle = 0; triangle < mesh.triangleCount; triangle++)
            {
                var offset = triangle * 3;
                var a = vertices[triangles[offset]];
                var b = vertices[triangles[offset + 1]];
                var c = vertices[triangles[offset + 2]];
                if (IsDegenerateTriangle(a, b, c)) continue;

                LVector3 pointAxis;
                LVector3 pointTriangle;
                ClosestPointsSegmentTriangle(
                    capsule.pointA, capsule.pointB, a, b, c, out pointAxis, out pointTriangle);
                var delta = pointTriangle - pointAxis;
                var sqrDistance = delta.sqrMagnitude;
                if (sqrDistance > capsule.radius * capsule.radius) continue;

                var distance = LMath.Sqrt(sqrDistance);
                var normal = NormalizeOr(delta, TriangleNormalToSurface(a, b, c, capsuleCenter));
                var penetration = LMath.Max(LFloat.zero, capsule.radius - distance);
                if (!ShouldReplace(found, penetration, triangle, bestPenetration, contact.featureB)) continue;

                found = true;
                bestPenetration = penetration;
                contact = new CollisionContact3D(
                    normal,
                    pointAxis + normal * capsule.radius,
                    pointTriangle,
                    penetration,
                    -1,
                    triangle);
            }
            return found;
        }

        /// <summary>对 OBB 与候选三角形执行 SAT，并保留最合适的接触特征。</summary>
        public static bool TestBoxMesh(
            BoxCollision3D box, MeshCollision3D mesh, out CollisionContact3D contact)
        {
            contact = default(CollisionContact3D);
            var found = false;
            var bestPenetration = LFloat.MinValue;
            var vertices = mesh.worldVertices;
            var triangles = mesh.triangles;
            for (var triangle = 0; triangle < mesh.triangleCount; triangle++)
            {
                var offset = triangle * 3;
                var a = vertices[triangles[offset]];
                var b = vertices[triangles[offset + 1]];
                var c = vertices[triangles[offset + 2]];
                if (IsDegenerateTriangle(a, b, c)) continue;

                CollisionContact3D candidate;
                if (!TestBoxTriangle(box, a, b, c, triangle, out candidate)) continue;
                if (!ShouldReplace(found, candidate.penetration, triangle, bestPenetration, contact.featureB))
                    continue;

                found = true;
                bestPenetration = candidate.penetration;
                contact = candidate;
            }
            return found;
        }

        public static bool TestMeshMesh(
            MeshCollision3D a, MeshCollision3D b, out CollisionContact3D contact)
        {
            contact = default(CollisionContact3D);
            var verticesA = a.worldVertices;
            var verticesB = b.worldVertices;
            var trianglesA = a.triangles;
            var trianglesB = b.triangles;
            for (var triangleA = 0; triangleA < a.triangleCount; triangleA++)
            {
                var offsetA = triangleA * 3;
                var a0 = verticesA[trianglesA[offsetA]];
                var a1 = verticesA[trianglesA[offsetA + 1]];
                var a2 = verticesA[trianglesA[offsetA + 2]];
                if (IsDegenerateTriangle(a0, a1, a2)) continue;

                for (var triangleB = 0; triangleB < b.triangleCount; triangleB++)
                {
                    var offsetB = triangleB * 3;
                    var b0 = verticesB[trianglesB[offsetB]];
                    var b1 = verticesB[trianglesB[offsetB + 1]];
                    var b2 = verticesB[trianglesB[offsetB + 2]];
                    if (IsDegenerateTriangle(b0, b1, b2)) continue;

                    CollisionContact3D candidate;
                    if (!TestTriangleTriangle(
                        a0, a1, a2, b0, b1, b2, triangleA, triangleB, out candidate))
                        continue;
                    contact = candidate;
                    return true;
                }
            }
            return false;
        }

        private static bool TestBoxTriangle(
            BoxCollision3D box, LVector3 a, LVector3 b, LVector3 c, int triangle,
            out CollisionContact3D contact)
        {
            var edge0 = b - a;
            var edge1 = c - b;
            var edge2 = a - c;
            var triangleCenter = LVector3.Average(a, b, c);
            var centerDelta = triangleCenter - box.pos;
            var minOverlap = LFloat.MaxValue;
            var bestAxis = LVector3.zero;

            for (var i = 0; i < 3; i++)
            {
                if (!TestBoxTriangleAxis(
                    box, a, b, c, GetBoxAxis(box, i), centerDelta,
                    ref minOverlap, ref bestAxis))
                {
                    contact = default(CollisionContact3D);
                    return false;
                }
            }
            if (!TestBoxTriangleAxis(
                box, a, b, c, LVector3.Cross(edge0, c - a), centerDelta,
                ref minOverlap, ref bestAxis))
            {
                contact = default(CollisionContact3D);
                return false;
            }
            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    var edge = j == 0 ? edge0 : (j == 1 ? edge1 : edge2);
                    if (TestBoxTriangleAxis(
                        box, a, b, c, LVector3.Cross(GetBoxAxis(box, i), edge), centerDelta,
                        ref minOverlap, ref bestAxis)) continue;
                    contact = default(CollisionContact3D);
                    return false;
                }
            }

            if (bestAxis == LVector3.zero)
                bestAxis = NormalizeOr(centerDelta, TriangleNormalToSurface(a, b, c, box.pos));
            var pointA = Support(box, bestAxis);
            var pointB = ClosestPointTriangle(pointA, a, b, c);
            contact = new CollisionContact3D(
                bestAxis, pointA, pointB, LMath.Max(LFloat.zero, minOverlap), -1, triangle);
            return true;
        }

        private static bool TestBoxTriangleAxis(
            BoxCollision3D box, LVector3 a, LVector3 b, LVector3 c,
            LVector3 axis, LVector3 centerDelta,
            ref LFloat minOverlap, ref LVector3 bestAxis)
        {
            if (axis.sqrMagnitude <= AxisEpsilon) return true;
            axis = axis.normalized;
            var boxCenterProjection = LVector3.Dot(box.pos, axis);
            var boxRadius = ProjectRadius(box, axis);
            LFloat triangleMin;
            LFloat triangleMax;
            ProjectTriangle(a, b, c, axis, out triangleMin, out triangleMax);
            var overlap = IntervalOverlap(
                boxCenterProjection - boxRadius, boxCenterProjection + boxRadius,
                triangleMin, triangleMax);
            if (overlap < LFloat.zero) return false;
            if (overlap < minOverlap)
            {
                minOverlap = overlap;
                bestAxis = LVector3.Dot(centerDelta, axis) < LFloat.zero ? -axis : axis;
            }
            return true;
        }

        private static bool TestTriangleTriangle(
            LVector3 a0, LVector3 a1, LVector3 a2,
            LVector3 b0, LVector3 b1, LVector3 b2,
            int triangleA, int triangleB, out CollisionContact3D contact)
        {
            var normalA = LVector3.Cross(a1 - a0, a2 - a0);
            var normalB = LVector3.Cross(b1 - b0, b2 - b0);
            var centerDelta = LVector3.Average(b0, b1, b2) - LVector3.Average(a0, a1, a2);
            var bestAxis = LVector3.zero;
            var minOverlap = LFloat.MaxValue;

            if (!TestTriangleAxis(a0, a1, a2, b0, b1, b2, normalA, centerDelta, ref minOverlap, ref bestAxis)
                || !TestTriangleAxis(a0, a1, a2, b0, b1, b2, normalB, centerDelta, ref minOverlap, ref bestAxis))
            {
                contact = default(CollisionContact3D);
                return false;
            }

            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    var axis = LVector3.Cross(
                        GetTriangleEdge(a0, a1, a2, i), GetTriangleEdge(b0, b1, b2, j));
                    if (!TestTriangleAxis(
                        a0, a1, a2, b0, b1, b2, axis, centerDelta, ref minOverlap, ref bestAxis))
                    {
                        contact = default(CollisionContact3D);
                        return false;
                    }
                }
            }

            var normalsCross = LVector3.Cross(normalA, normalB);
            var coplanar = normalsCross.sqrMagnitude <= AxisEpsilon
                && LMath.Abs(LVector3.Dot(normalA.normalized, b0 - a0)) <= AxisEpsilon;
            if (coplanar)
            {
                for (var i = 0; i < 3; i++)
                {
                    if (!TestTriangleAxis(
                        a0, a1, a2, b0, b1, b2,
                        LVector3.Cross(normalA, GetTriangleEdge(a0, a1, a2, i)),
                        centerDelta, ref minOverlap, ref bestAxis)
                        || !TestTriangleAxis(
                            a0, a1, a2, b0, b1, b2,
                            LVector3.Cross(normalA, GetTriangleEdge(b0, b1, b2, i)),
                            centerDelta, ref minOverlap, ref bestAxis))
                    {
                        contact = default(CollisionContact3D);
                        return false;
                    }
                }
            }

            LVector3 pointA;
            LVector3 pointB;
            ClosestPointsTriangles(a0, a1, a2, b0, b1, b2, out pointA, out pointB);
            if (bestAxis == LVector3.zero)
                bestAxis = NormalizeOr(centerDelta, normalA.normalized);
            contact = new CollisionContact3D(
                bestAxis, pointA, pointB, LFloat.zero, triangleA, triangleB);
            return true;
        }

        private static bool TestTriangleAxis(
            LVector3 a0, LVector3 a1, LVector3 a2,
            LVector3 b0, LVector3 b1, LVector3 b2,
            LVector3 axis, LVector3 centerDelta, ref LFloat minOverlap, ref LVector3 bestAxis)
        {
            if (axis.sqrMagnitude <= AxisEpsilon) return true;
            axis = axis.normalized;
            LFloat minA;
            LFloat maxA;
            LFloat minB;
            LFloat maxB;
            ProjectTriangle(a0, a1, a2, axis, out minA, out maxA);
            ProjectTriangle(b0, b1, b2, axis, out minB, out maxB);
            var overlap = IntervalOverlap(minA, maxA, minB, maxB);
            if (overlap < LFloat.zero) return false;
            if (overlap < minOverlap)
            {
                minOverlap = overlap;
                bestAxis = LVector3.Dot(centerDelta, axis) < LFloat.zero ? -axis : axis;
            }
            return true;
        }

        private static void ClosestPointsTriangles(
            LVector3 a0, LVector3 a1, LVector3 a2,
            LVector3 b0, LVector3 b1, LVector3 b2,
            out LVector3 pointA, out LVector3 pointB)
        {
            pointA = a0;
            pointB = ClosestPointTriangle(a0, b0, b1, b2);
            var best = (pointA - pointB).sqrMagnitude;
            TryPair(a1, ClosestPointTriangle(a1, b0, b1, b2), ref best, ref pointA, ref pointB);
            TryPair(a2, ClosestPointTriangle(a2, b0, b1, b2), ref best, ref pointA, ref pointB);

            var candidateB = b0;
            var candidateA = ClosestPointTriangle(b0, a0, a1, a2);
            TryPair(candidateA, candidateB, ref best, ref pointA, ref pointB);
            candidateB = b1;
            candidateA = ClosestPointTriangle(b1, a0, a1, a2);
            TryPair(candidateA, candidateB, ref best, ref pointA, ref pointB);
            candidateB = b2;
            candidateA = ClosestPointTriangle(b2, a0, a1, a2);
            TryPair(candidateA, candidateB, ref best, ref pointA, ref pointB);

            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    TrySegmentPair(
                        GetTriangleVertex(a0, a1, a2, i), GetTriangleVertex(a0, a1, a2, (i + 1) % 3),
                        GetTriangleVertex(b0, b1, b2, j), GetTriangleVertex(b0, b1, b2, (j + 1) % 3),
                        ref best, ref pointA, ref pointB);
                }
            }
        }

        private static LVector3 TriangleNormalToSurface(
            LVector3 a, LVector3 b, LVector3 c, LVector3 source)
        {
            var normal = NormalizeOr(LVector3.Cross(b - a, c - a), LVector3.up);
            return LVector3.Dot(source - a, normal) > LFloat.zero ? -normal : normal;
        }

        private static bool IsDegenerateTriangle(LVector3 a, LVector3 b, LVector3 c)
        {
            return LVector3.Cross(b - a, c - a).sqrMagnitude <= AxisEpsilon;
        }

        private static void ProjectTriangle(
            LVector3 a, LVector3 b, LVector3 c, LVector3 axis,
            out LFloat min, out LFloat max)
        {
            var projectionA = LVector3.Dot(a, axis);
            var projectionB = LVector3.Dot(b, axis);
            var projectionC = LVector3.Dot(c, axis);
            min = LMath.Min(projectionA, LMath.Min(projectionB, projectionC));
            max = LMath.Max(projectionA, LMath.Max(projectionB, projectionC));
        }

        private static LFloat IntervalOverlap(
            LFloat minA, LFloat maxA, LFloat minB, LFloat maxB)
        {
            return LMath.Min(maxA, maxB) - LMath.Max(minA, minB);
        }

        private static bool ShouldReplace(
            bool found, LFloat penetration, int triangle,
            LFloat bestPenetration, int bestTriangle)
        {
            return !found || penetration > bestPenetration
                || (penetration == bestPenetration && triangle < bestTriangle);
        }

        private static LVector3 GetTriangleVertex(
            LVector3 a, LVector3 b, LVector3 c, int index)
        {
            return index == 0 ? a : (index == 1 ? b : c);
        }

        private static LVector3 GetTriangleEdge(
            LVector3 a, LVector3 b, LVector3 c, int index)
        {
            return GetTriangleVertex(a, b, c, (index + 1) % 3)
                - GetTriangleVertex(a, b, c, index);
        }
    }
}
