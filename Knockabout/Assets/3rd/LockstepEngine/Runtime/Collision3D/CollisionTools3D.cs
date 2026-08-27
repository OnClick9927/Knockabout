namespace Lockstep.Collision
{
    /// <summary>
    /// 三维基础形状的窄相位、射线和几何辅助算法。
    /// 类型分派覆盖球、OBB、胶囊和三角网格；接触法线统一保持从参数 A 指向参数 B。
    /// 盒体组合使用分离轴定理，胶囊组合使用线段最近点，退化轴通过 AxisEpsilon 保护。
    /// </summary>
    public static partial class CollisionTools3D
    {
        private static readonly LFloat AxisEpsilon = LFloat.FromRaw(4);

        /// <summary>根据双方运行时类型分派窄相位，不支持的组合返回 false。</summary>
        public static bool Test(Collision3D a, Collision3D b, out CollisionContact3D contact)
        {
            contact = default(CollisionContact3D);
            if (a == null || b == null || !a.bounds.Overlaps(b.bounds))
                return false;

            var sphereA = a as SphereCollision3D;
            if (sphereA != null)
            {
                var sphereB = b as SphereCollision3D;
                if (sphereB != null) return TestSphereSphere(sphereA, sphereB, out contact);
                var boxB = b as BoxCollision3D;
                if (boxB != null) return TestSphereBox(sphereA, boxB, out contact);
                var capsuleB = b as CapsuleCollision3D;
                if (capsuleB != null) return TestSphereCapsule(sphereA, capsuleB, out contact);
                var meshB = b as MeshCollision3D;
                if (meshB != null) return TestSphereMesh(sphereA, meshB, out contact);
            }

            var boxA = a as BoxCollision3D;
            if (boxA != null)
            {
                var sphereB = b as SphereCollision3D;
                if (sphereB != null) return Flip(TestSphereBox(sphereB, boxA, out contact), ref contact);
                var boxB = b as BoxCollision3D;
                if (boxB != null) return TestBoxBox(boxA, boxB, out contact);
                var capsuleB = b as CapsuleCollision3D;
                if (capsuleB != null) return TestBoxCapsule(boxA, capsuleB, out contact);
                var meshB = b as MeshCollision3D;
                if (meshB != null) return TestBoxMesh(boxA, meshB, out contact);
            }

            var capsuleA = a as CapsuleCollision3D;
            if (capsuleA != null)
            {
                var sphereB = b as SphereCollision3D;
                if (sphereB != null) return Flip(TestSphereCapsule(sphereB, capsuleA, out contact), ref contact);
                var boxB = b as BoxCollision3D;
                if (boxB != null) return Flip(TestBoxCapsule(boxB, capsuleA, out contact), ref contact);
                var capsuleB = b as CapsuleCollision3D;
                if (capsuleB != null) return TestCapsuleCapsule(capsuleA, capsuleB, out contact);
                var meshB = b as MeshCollision3D;
                if (meshB != null) return TestCapsuleMesh(capsuleA, meshB, out contact);
            }

            var meshA = a as MeshCollision3D;
            if (meshA != null)
            {
                var sphereB = b as SphereCollision3D;
                if (sphereB != null) return Flip(TestSphereMesh(sphereB, meshA, out contact), ref contact);
                var boxB = b as BoxCollision3D;
                if (boxB != null) return Flip(TestBoxMesh(boxB, meshA, out contact), ref contact);
                var capsuleB = b as CapsuleCollision3D;
                if (capsuleB != null) return Flip(TestCapsuleMesh(capsuleB, meshA, out contact), ref contact);
                var meshB = b as MeshCollision3D;
                if (meshB != null) return TestMeshMesh(meshA, meshB, out contact);
            }

            return false;
        }

        /// <summary>
        /// 射线与 3D 碰撞形状的窄相位检测入口。
        /// direction 必须是单位向量；公开的 Collision3D.RayCast 会负责归一化。
        /// </summary>
        internal static bool TestRay(
            Collision3D collision,
            LVector3 origin,
            LVector3 direction,
            out LVector3 hitPoint,
            out LVector3 normal,
            out int feature)
        {
            hitPoint = LVector3.zero;
            normal = LVector3.zero;
            feature = -1;
            if (collision == null || direction == LVector3.zero) return false;

            var sphere = collision as SphereCollision3D;
            if (sphere != null)
                return TestRaySphere(sphere, origin, direction, out hitPoint, out normal);

            var box = collision as BoxCollision3D;
            if (box != null)
                return TestRayBox(box, origin, direction, out hitPoint, out normal);

            var capsule = collision as CapsuleCollision3D;
            if (capsule != null)
                return TestRayCapsule(capsule, origin, direction, out hitPoint, out normal);

            var mesh = collision as MeshCollision3D;
            if (mesh != null)
                return TestRayMesh(
                    mesh, origin, direction, out hitPoint, out normal, out feature);

            return false;
        }

        /// <summary>
        /// 射线与轴对齐包围盒的快速检测，只用于树查询的宽相位剔除。
        /// </summary>
        internal static bool TestRayBounds(
            LVector3 origin,
            LVector3 direction,
            LBounds bounds,
            LFloat maxDistance)
        {
            // 起点位于包围盒内时，宽相位距离应视为 0。若使用离开包围盒的距离，
            // 有限射线可能在盒内先命中真实表面，却被一个更远的离开点错误剔除。
            if (bounds.Contains(origin)) return true;

            LFloat distance;
            LVector3 normal;
            return TestRayAabb(
                origin, direction, bounds.min, bounds.max, out distance, out normal)
                && distance <= maxDistance;
        }

        private static bool TestRaySphere(
            SphereCollision3D sphere,
            LVector3 origin,
            LVector3 direction,
            out LVector3 hitPoint,
            out LVector3 normal)
        {
            LFloat distance;
            if (!TryRaySphereDistance(
                origin, direction, sphere.pos, sphere.radius, out distance))
            {
                hitPoint = LVector3.zero;
                normal = LVector3.zero;
                return false;
            }

            hitPoint = origin + direction * distance;
            normal = NormalizeOr(hitPoint - sphere.pos, -direction);
            return true;
        }

        private static bool TestRayBox(
            BoxCollision3D box,
            LVector3 origin,
            LVector3 direction,
            out LVector3 hitPoint,
            out LVector3 normal)
        {
            // 将射线逆旋转到 OBB 的局部空间后，问题就退化成 AABB 射线检测。
            // box.halfSize 已包含碰撞体的统一缩放，因此这里不再额外缩放射线。
            var inverseRotation = LQuaternion.Inverse(box.rotation);
            var localOrigin = inverseRotation * (origin - box.pos);
            var localDirection = inverseRotation * direction;
            var half = box.halfSize;

            LFloat distance;
            LVector3 localNormal;
            if (!TestRayAabb(
                localOrigin, localDirection, -half, half, out distance, out localNormal))
            {
                hitPoint = LVector3.zero;
                normal = LVector3.zero;
                return false;
            }

            hitPoint = origin + direction * distance;
            normal = NormalizeOr(box.rotation * localNormal, -direction);
            return true;
        }

        private static bool TestRayCapsule(
            CapsuleCollision3D capsule,
            LVector3 origin,
            LVector3 direction,
            out LVector3 hitPoint,
            out LVector3 normal)
        {
            var segment = capsule.pointB - capsule.pointA;
            var segmentLengthSquared = segment.sqrMagnitude;
            var originFromA = origin - capsule.pointA;
            var bestDistance = LFloat.MaxValue;
            var bestNormal = LVector3.zero;
            var found = false;

            if (segmentLengthSquared > AxisEpsilon)
            {
                // 胶囊侧面可视为一段有限圆柱。下面先求无限圆柱的两个根，
                // 再通过轴向投影把交点限制在线段 A-B 的范围内。
                var directionOnAxis = LVector3.Dot(direction, segment);
                var originOnAxis = LVector3.Dot(originFromA, segment);
                var quadraticA = segmentLengthSquared
                    - directionOnAxis * directionOnAxis;
                var quadraticB = segmentLengthSquared * LVector3.Dot(originFromA, direction)
                    - originOnAxis * directionOnAxis;
                var quadraticC = segmentLengthSquared * originFromA.sqrMagnitude
                    - originOnAxis * originOnAxis
                    - capsule.radius * capsule.radius * segmentLengthSquared;
                var discriminant = quadraticB * quadraticB - quadraticA * quadraticC;

                if (quadraticA > AxisEpsilon && discriminant >= LFloat.zero)
                {
                    var root = LMath.Sqrt(discriminant);
                    TryCapsuleCylinderRoot(
                        (-quadraticB - root) / quadraticA,
                        origin, direction, capsule.pointA, segment,
                        segmentLengthSquared, originOnAxis, directionOnAxis,
                        ref found, ref bestDistance, ref bestNormal);
                    TryCapsuleCylinderRoot(
                        (-quadraticB + root) / quadraticA,
                        origin, direction, capsule.pointA, segment,
                        segmentLengthSquared, originOnAxis, directionOnAxis,
                        ref found, ref bestDistance, ref bestNormal);
                }

                // 两端使用“半球”而不是完整球体。轴向条件可排除位于圆柱内部的
                // 球面交点，尤其能保证射线从胶囊内部发射时返回真正的外表面。
                TryCapsuleCap(
                    capsule.pointA, false, origin, direction, segment,
                    capsule.radius,
                    ref found, ref bestDistance, ref bestNormal);
                TryCapsuleCap(
                    capsule.pointB, true, origin, direction, segment,
                    capsule.radius,
                    ref found, ref bestDistance, ref bestNormal);
            }
            else
            {
                // 高度等于直径时，胶囊的轴线退化为一点，形状等价于球。
                LFloat sphereDistance;
                if (TryRaySphereDistance(
                    origin, direction, capsule.pos, capsule.radius, out sphereDistance))
                {
                    found = true;
                    bestDistance = sphereDistance;
                    var point = origin + direction * sphereDistance;
                    bestNormal = NormalizeOr(point - capsule.pos, -direction);
                }
            }

            if (!found)
            {
                hitPoint = LVector3.zero;
                normal = LVector3.zero;
                return false;
            }

            hitPoint = origin + direction * bestDistance;
            normal = bestNormal;
            return true;
        }

        private static bool TestRayMesh(
            MeshCollision3D mesh,
            LVector3 origin,
            LVector3 direction,
            out LVector3 hitPoint,
            out LVector3 normal,
            out int triangleIndex)
        {
            hitPoint = LVector3.zero;
            normal = LVector3.zero;
            triangleIndex = -1;
            if (mesh.worldVertices == null || mesh.triangles == null) return false;

            var bestDistance = LFloat.MaxValue;
            for (var i = 0; i < mesh.triangles.Length; i += 3)
            {
                var a = mesh.worldVertices[mesh.triangles[i]];
                var b = mesh.worldVertices[mesh.triangles[i + 1]];
                var c = mesh.worldVertices[mesh.triangles[i + 2]];
                LFloat distance;
                LVector3 candidateNormal;
                if (!TestRayTriangle(
                    origin, direction, a, b, c, out distance, out candidateNormal)) continue;
                if (distance >= bestDistance) continue;

                bestDistance = distance;
                normal = candidateNormal;
                triangleIndex = i / 3;
            }

            if (triangleIndex < 0) return false;
            hitPoint = origin + direction * bestDistance;
            return true;
        }

        private static bool TestRayTriangle(
            LVector3 origin,
            LVector3 direction,
            LVector3 a,
            LVector3 b,
            LVector3 c,
            out LFloat distance,
            out LVector3 normal)
        {
            // Moller-Trumbore 算法直接在三角形重心坐标中求交。
            // 不剔除背面，使薄的 MeshCollision3D 可从两侧被射线命中。
            distance = LFloat.zero;
            normal = LVector3.zero;
            var edgeAB = b - a;
            var edgeAC = c - a;
            var p = LVector3.Cross(direction, edgeAC);
            var determinant = LVector3.Dot(edgeAB, p);
            if (LMath.Abs(determinant) <= AxisEpsilon) return false;

            var inverseDeterminant = LFloat.one / determinant;
            var fromA = origin - a;
            var u = LVector3.Dot(fromA, p) * inverseDeterminant;
            if (u < LFloat.zero || u > LFloat.one) return false;

            var q = LVector3.Cross(fromA, edgeAB);
            var v = LVector3.Dot(direction, q) * inverseDeterminant;
            if (v < LFloat.zero || u + v > LFloat.one) return false;

            distance = LVector3.Dot(edgeAC, q) * inverseDeterminant;
            if (distance < LFloat.zero) return false;

            normal = NormalizeOr(LVector3.Cross(edgeAB, edgeAC), -direction);
            if (LVector3.Dot(normal, direction) > LFloat.zero)
                normal = -normal;
            return true;
        }

        private static bool TryRaySphereDistance(
            LVector3 origin,
            LVector3 direction,
            LVector3 center,
            LFloat radius,
            out LFloat distance)
        {
            // direction 是单位向量，标准二次方程可约去 a 项。
            var fromCenter = origin - center;
            var projected = LVector3.Dot(fromCenter, direction);
            var constant = fromCenter.sqrMagnitude - radius * radius;
            var discriminant = projected * projected - constant;
            if (discriminant < LFloat.zero)
            {
                distance = LFloat.zero;
                return false;
            }

            var root = LMath.Sqrt(discriminant);
            var near = -projected - root;
            var far = -projected + root;
            distance = near >= LFloat.zero ? near : far;
            return distance >= LFloat.zero;
        }

        private static void TryCapsuleCylinderRoot(
            LFloat distance,
            LVector3 origin,
            LVector3 direction,
            LVector3 segmentA,
            LVector3 segment,
            LFloat segmentLengthSquared,
            LFloat originOnAxis,
            LFloat directionOnAxis,
            ref bool found,
            ref LFloat bestDistance,
            ref LVector3 bestNormal)
        {
            if (distance < LFloat.zero || distance >= bestDistance) return;
            var axisPosition = originOnAxis + distance * directionOnAxis;
            if (axisPosition < LFloat.zero || axisPosition > segmentLengthSquared) return;

            var point = origin + direction * distance;
            var pointOnAxis = segmentA + segment * (axisPosition / segmentLengthSquared);
            found = true;
            bestDistance = distance;
            bestNormal = NormalizeOr(point - pointOnAxis, -direction);
        }

        private static void TryCapsuleCap(
            LVector3 center,
            bool isEndCap,
            LVector3 origin,
            LVector3 direction,
            LVector3 segment,
            LFloat radius,
            ref bool found,
            ref LFloat bestDistance,
            ref LVector3 bestNormal)
        {
            var fromCenter = origin - center;
            var projected = LVector3.Dot(fromCenter, direction);
            var constant = fromCenter.sqrMagnitude - radius * radius;
            var discriminant = projected * projected - constant;
            if (discriminant < LFloat.zero) return;

            var root = LMath.Sqrt(discriminant);
            TryCapsuleCapRoot(
                -projected - root, center, isEndCap, origin, direction,
                segment,
                ref found, ref bestDistance, ref bestNormal);
            TryCapsuleCapRoot(
                -projected + root, center, isEndCap, origin, direction,
                segment,
                ref found, ref bestDistance, ref bestNormal);
        }

        private static void TryCapsuleCapRoot(
            LFloat distance,
            LVector3 center,
            bool isEndCap,
            LVector3 origin,
            LVector3 direction,
            LVector3 segment,
            ref bool found,
            ref LFloat bestDistance,
            ref LVector3 bestNormal)
        {
            if (distance < LFloat.zero || distance >= bestDistance) return;
            var point = origin + direction * distance;
            var axial = LVector3.Dot(point - center, segment);
            if (isEndCap ? axial < LFloat.zero : axial > LFloat.zero) return;

            found = true;
            bestDistance = distance;
            bestNormal = NormalizeOr(point - center, -direction);
        }

        private static bool TestRayAabb(
            LVector3 origin,
            LVector3 direction,
            LVector3 min,
            LVector3 max,
            out LFloat distance,
            out LVector3 normal)
        {
            // 从负无穷开始记录进入时间，才能区分“起点在盒内”和
            // “起点恰好位于表面且沿盒内方向发射”这两种情况。
            var enter = LFloat.MinValue;
            var exit = LFloat.MaxValue;
            var enterNormal = LVector3.zero;
            var exitNormal = LVector3.zero;

            for (var axis = 0; axis < 3; axis++)
            {
                if (LMath.Abs(direction[axis]) <= AxisEpsilon)
                {
                    // 射线与当前轴的两平面平行；起点在范围外时永远无法进入盒体。
                    if (origin[axis] < min[axis] || origin[axis] > max[axis])
                    {
                        distance = LFloat.zero;
                        normal = LVector3.zero;
                        return false;
                    }
                    continue;
                }

                var near = (min[axis] - origin[axis]) / direction[axis];
                var far = (max[axis] - origin[axis]) / direction[axis];
                var nearNormal = AxisVector(axis, -LFloat.one);
                var farNormal = AxisVector(axis, LFloat.one);
                if (near > far)
                {
                    var swapDistance = near;
                    near = far;
                    far = swapDistance;
                    var swapNormal = nearNormal;
                    nearNormal = farNormal;
                    farNormal = swapNormal;
                }

                if (near > enter)
                {
                    enter = near;
                    enterNormal = nearNormal;
                }
                if (far < exit)
                {
                    exit = far;
                    exitNormal = farNormal;
                }
                if (enter > exit || exit < LFloat.zero)
                {
                    distance = LFloat.zero;
                    normal = LVector3.zero;
                    return false;
                }
            }

            // 起点在盒内时进入时间为负数，应返回射线前方的离开面；
            // 起点恰好位于表面时 enter 为 0，则保留距离为 0 的表面命中。
            if (enter < LFloat.zero)
            {
                distance = exit;
                normal = exitNormal;
            }
            else
            {
                distance = enter;
                normal = enterNormal;
            }
            return distance >= LFloat.zero && normal != LVector3.zero;
        }

        private static LVector3 AxisVector(int axis, LFloat value)
        {
            return axis == 0
                ? new LVector3(value, LFloat.zero, LFloat.zero)
                : axis == 1
                    ? new LVector3(LFloat.zero, value, LFloat.zero)
                    : new LVector3(LFloat.zero, LFloat.zero, value);
        }

        private static bool Flip(bool hit, ref CollisionContact3D contact)
        {
            if (hit) contact = contact.Flipped();
            return hit;
        }

        /// <summary>比较球心距离与半径和，并生成双方表面点和穿透深度。</summary>
        public static bool TestSphereSphere(
            SphereCollision3D a, SphereCollision3D b, out CollisionContact3D contact)
        {
            var delta = b.pos - a.pos;
            var radius = a.radius + b.radius;
            var sqrDistance = delta.sqrMagnitude;
            if (sqrDistance > radius * radius)
            {
                contact = default(CollisionContact3D);
                return false;
            }

            var distance = LMath.Sqrt(sqrDistance);
            var normal = NormalizeOr(delta, LVector3.right);
            contact = new CollisionContact3D(
                normal,
                a.pos + normal * a.radius,
                b.pos - normal * b.radius,
                LMath.Max(LFloat.zero, radius - distance));
            return true;
        }

        public static bool TestSphereBox(
            SphereCollision3D sphere, BoxCollision3D box, out CollisionContact3D contact)
        {
            var localCenter = ToBoxLocal(box, sphere.pos);
            var half = box.halfSize;
            var closestLocal = Clamp(localCenter, -half, half);
            var pointBox = FromBoxLocal(box, closestLocal);
            var delta = pointBox - sphere.pos;
            var sqrDistance = delta.sqrMagnitude;

            if (sqrDistance > sphere.radius * sphere.radius)
            {
                contact = default(CollisionContact3D);
                return false;
            }

            LVector3 normal;
            LFloat penetration;
            if (sqrDistance > AxisEpsilon)
            {
                var distance = LMath.Sqrt(sqrDistance);
                normal = delta / distance;
                penetration = sphere.radius - distance;
            }
            else
            {
                LFloat faceDistance;
                LVector3 localNormal;
                ClosestFace(localCenter, half, out localNormal, out faceDistance);
                normal = box.rotation * localNormal;
                pointBox = FromBoxLocal(box, localCenter + localNormal * faceDistance);
                penetration = sphere.radius + faceDistance;
            }

            contact = new CollisionContact3D(
                normal,
                sphere.pos + normal * sphere.radius,
                pointBox,
                LMath.Max(LFloat.zero, penetration));
            return true;
        }

        public static bool TestSphereCapsule(
            SphereCollision3D sphere, CapsuleCollision3D capsule, out CollisionContact3D contact)
        {
            var pointOnAxis = ClosestPointSegment(sphere.pos, capsule.pointA, capsule.pointB);
            var delta = pointOnAxis - sphere.pos;
            var radius = sphere.radius + capsule.radius;
            var sqrDistance = delta.sqrMagnitude;
            if (sqrDistance > radius * radius)
            {
                contact = default(CollisionContact3D);
                return false;
            }

            var distance = LMath.Sqrt(sqrDistance);
            var fallback = Perpendicular(capsule.pointB - capsule.pointA);
            var normal = NormalizeOr(delta, fallback);
            contact = new CollisionContact3D(
                normal,
                sphere.pos + normal * sphere.radius,
                pointOnAxis - normal * capsule.radius,
                LMath.Max(LFloat.zero, radius - distance));
            return true;
        }

        public static bool TestCapsuleCapsule(
            CapsuleCollision3D a, CapsuleCollision3D b, out CollisionContact3D contact)
        {
            LVector3 pointA;
            LVector3 pointB;
            ClosestPointsSegments(a.pointA, a.pointB, b.pointA, b.pointB, out pointA, out pointB);
            var delta = pointB - pointA;
            var radius = a.radius + b.radius;
            var sqrDistance = delta.sqrMagnitude;
            if (sqrDistance > radius * radius)
            {
                contact = default(CollisionContact3D);
                return false;
            }

            var distance = LMath.Sqrt(sqrDistance);
            var normal = NormalizeOr(delta, Perpendicular(a.pointB - a.pointA));
            contact = new CollisionContact3D(
                normal,
                pointA + normal * a.radius,
                pointB - normal * b.radius,
                LMath.Max(LFloat.zero, radius - distance));
            return true;
        }

        /// <summary>
        /// 对双方 3+3 个面法线及 9 个边叉积轴执行 SAT，选取最小重叠轴作为接触法线。
        /// </summary>
        public static bool TestBoxBox(BoxCollision3D a, BoxCollision3D b, out CollisionContact3D contact)
        {
            var centerDelta = b.pos - a.pos;
            var bestAxis = LVector3.zero;
            var minOverlap = LFloat.MaxValue;

            for (var i = 0; i < 3; i++)
            {
                var axisA = GetBoxAxis(a, i);
                var axisB = GetBoxAxis(b, i);
                if (!TestObbAxis(a, b, axisA, centerDelta, ref minOverlap, ref bestAxis))
                {
                    contact = default(CollisionContact3D);
                    return false;
                }
                if (!TestObbAxis(a, b, axisB, centerDelta, ref minOverlap, ref bestAxis))
                {
                    contact = default(CollisionContact3D);
                    return false;
                }
            }

            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    var axis = LVector3.Cross(GetBoxAxis(a, i), GetBoxAxis(b, j));
                    if (axis.sqrMagnitude <= AxisEpsilon) continue;
                    if (!TestObbAxis(a, b, axis, centerDelta, ref minOverlap, ref bestAxis))
                    {
                        contact = default(CollisionContact3D);
                        return false;
                    }
                }
            }

            if (bestAxis == LVector3.zero) bestAxis = NormalizeOr(centerDelta, LVector3.right);
            var pointA = Support(a, bestAxis);
            var pointB = Support(b, -bestAxis);
            contact = new CollisionContact3D(
                bestAxis, pointA, pointB, LMath.Max(LFloat.zero, minOverlap));
            return true;
        }

        public static bool TestBoxCapsule(
            BoxCollision3D box, CapsuleCollision3D capsule, out CollisionContact3D contact)
        {
            LVector3 pointBox;
            LVector3 pointAxis;
            ClosestPointsBoxSegment(box, capsule.pointA, capsule.pointB, out pointBox, out pointAxis);
            var delta = pointAxis - pointBox;
            var sqrDistance = delta.sqrMagnitude;
            var inside = SegmentIntersectsBox(box, capsule.pointA, capsule.pointB);
            if (!inside && sqrDistance > capsule.radius * capsule.radius)
            {
                contact = default(CollisionContact3D);
                return false;
            }

            LVector3 normal;
            LFloat penetration;
            if (sqrDistance > AxisEpsilon)
            {
                var distance = LMath.Sqrt(sqrDistance);
                normal = delta / distance;
                penetration = inside ? capsule.radius + distance : capsule.radius - distance;
            }
            else
            {
                var localPoint = ToBoxLocal(box, pointAxis);
                LFloat faceDistance;
                LVector3 localNormal;
                ClosestFace(localPoint, box.halfSize, out localNormal, out faceDistance);
                normal = box.rotation * localNormal;
                pointBox = FromBoxLocal(box, localPoint + localNormal * faceDistance);
                penetration = capsule.radius + faceDistance;
            }

            contact = new CollisionContact3D(
                normal,
                pointBox,
                pointAxis - normal * capsule.radius,
                LMath.Max(LFloat.zero, penetration));
            return true;
        }

        private static bool TestObbAxis(
            BoxCollision3D a, BoxCollision3D b, LVector3 axis, LVector3 centerDelta,
            ref LFloat minOverlap, ref LVector3 bestAxis)
        {
            axis = axis.normalized;
            if (axis == LVector3.zero) return true;
            var radiusA = ProjectRadius(a, axis);
            var radiusB = ProjectRadius(b, axis);
            var distance = LMath.Abs(LVector3.Dot(centerDelta, axis));
            var overlap = radiusA + radiusB - distance;
            if (overlap < LFloat.zero) return false;
            if (overlap < minOverlap)
            {
                minOverlap = overlap;
                bestAxis = LVector3.Dot(centerDelta, axis) < LFloat.zero ? -axis : axis;
            }
            return true;
        }

        private static LFloat ProjectRadius(BoxCollision3D box, LVector3 axis)
        {
            return LMath.Abs(LVector3.Dot(box.axisX, axis)) * box.halfSize.x
                 + LMath.Abs(LVector3.Dot(box.axisY, axis)) * box.halfSize.y
                 + LMath.Abs(LVector3.Dot(box.axisZ, axis)) * box.halfSize.z;
        }

        internal static LVector3 Support(BoxCollision3D box, LVector3 direction)
        {
            var half = box.halfSize;
            return box.pos
                 + box.axisX * (LVector3.Dot(box.axisX, direction) >= LFloat.zero ? half.x : -half.x)
                 + box.axisY * (LVector3.Dot(box.axisY, direction) >= LFloat.zero ? half.y : -half.y)
                 + box.axisZ * (LVector3.Dot(box.axisZ, direction) >= LFloat.zero ? half.z : -half.z);
        }

        internal static LVector3 NormalizeOr(LVector3 value, LVector3 fallback)
        {
            return value.sqrMagnitude <= AxisEpsilon ? fallback.normalized : value.normalized;
        }

        internal static LVector3 Perpendicular(LVector3 value)
        {
            if (value.sqrMagnitude <= AxisEpsilon) return LVector3.right;
            var axis = LMath.Abs(value.x) <= LMath.Abs(value.y)
                && LMath.Abs(value.x) <= LMath.Abs(value.z) ? LVector3.right
                : (LMath.Abs(value.y) <= LMath.Abs(value.z) ? LVector3.up : LVector3.forward);
            return NormalizeOr(LVector3.Cross(value, axis), LVector3.right);
        }

        internal static LVector3 Clamp(LVector3 value, LVector3 min, LVector3 max)
        {
            return new LVector3(
                LMath.Clamp(value.x, min.x, max.x),
                LMath.Clamp(value.y, min.y, max.y),
                LMath.Clamp(value.z, min.z, max.z));
        }

        internal static LVector3 ClosestPointSegment(LVector3 point, LVector3 a, LVector3 b)
        {
            var ab = b - a;
            var denominator = LVector3.Dot(ab, ab);
            if (denominator <= AxisEpsilon) return a;
            var t = LMath.Clamp01(LVector3.Dot(point - a, ab) / denominator);
            return a + ab * t;
        }

        internal static void ClosestPointsSegments(
            LVector3 p1, LVector3 q1, LVector3 p2, LVector3 q2,
            out LVector3 point1, out LVector3 point2)
        {
            var d1 = q1 - p1;
            var d2 = q2 - p2;
            var r = p1 - p2;
            var a = LVector3.Dot(d1, d1);
            var e = LVector3.Dot(d2, d2);
            var f = LVector3.Dot(d2, r);
            LFloat s;
            LFloat t;

            if (a <= AxisEpsilon && e <= AxisEpsilon)
            {
                point1 = p1;
                point2 = p2;
                return;
            }

            if (a <= AxisEpsilon)
            {
                s = LFloat.zero;
                t = e <= AxisEpsilon ? LFloat.zero : LMath.Clamp01(f / e);
            }
            else
            {
                var c = LVector3.Dot(d1, r);
                if (e <= AxisEpsilon)
                {
                    t = LFloat.zero;
                    s = LMath.Clamp01(-c / a);
                }
                else
                {
                    var b = LVector3.Dot(d1, d2);
                    var denominator = a * e - b * b;
                    s = LMath.Abs(denominator) <= AxisEpsilon
                        ? LFloat.zero
                        : LMath.Clamp01((b * f - c * e) / denominator);
                    t = (b * s + f) / e;
                    if (t < LFloat.zero)
                    {
                        t = LFloat.zero;
                        s = LMath.Clamp01(-c / a);
                    }
                    else if (t > LFloat.one)
                    {
                        t = LFloat.one;
                        s = LMath.Clamp01((b - c) / a);
                    }
                }
            }

            point1 = p1 + d1 * s;
            point2 = p2 + d2 * t;
        }

        internal static LVector3 ClosestPointTriangle(
            LVector3 point, LVector3 a, LVector3 b, LVector3 c)
        {
            var ab = b - a;
            var ac = c - a;
            var ap = point - a;
            var d1 = LVector3.Dot(ab, ap);
            var d2 = LVector3.Dot(ac, ap);
            if (d1 <= LFloat.zero && d2 <= LFloat.zero) return a;

            var bp = point - b;
            var d3 = LVector3.Dot(ab, bp);
            var d4 = LVector3.Dot(ac, bp);
            if (d3 >= LFloat.zero && d4 <= d3) return b;

            var vc = d1 * d4 - d3 * d2;
            if (vc <= LFloat.zero && d1 >= LFloat.zero && d3 <= LFloat.zero)
            {
                var v = d1 / (d1 - d3);
                return a + ab * v;
            }

            var cp = point - c;
            var d5 = LVector3.Dot(ab, cp);
            var d6 = LVector3.Dot(ac, cp);
            if (d6 >= LFloat.zero && d5 <= d6) return c;

            var vb = d5 * d2 - d1 * d6;
            if (vb <= LFloat.zero && d2 >= LFloat.zero && d6 <= LFloat.zero)
            {
                var w = d2 / (d2 - d6);
                return a + ac * w;
            }

            var va = d3 * d6 - d5 * d4;
            if (va <= LFloat.zero && d4 - d3 >= LFloat.zero && d5 - d6 >= LFloat.zero)
            {
                var w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                return b + (c - b) * w;
            }

            var denominator = va + vb + vc;
            if (LMath.Abs(denominator) <= AxisEpsilon) return a;
            var inverse = LFloat.one / denominator;
            var baryV = vb * inverse;
            var baryW = vc * inverse;
            return a + ab * baryV + ac * baryW;
        }

        internal static void ClosestPointsSegmentTriangle(
            LVector3 segmentA, LVector3 segmentB, LVector3 a, LVector3 b, LVector3 c,
            out LVector3 pointSegment, out LVector3 pointTriangle)
        {
            LVector3 intersection;
            if (SegmentIntersectsTriangle(segmentA, segmentB, a, b, c, out intersection))
            {
                pointSegment = pointTriangle = intersection;
                return;
            }

            pointSegment = segmentA;
            pointTriangle = ClosestPointTriangle(segmentA, a, b, c);
            var best = (pointSegment - pointTriangle).sqrMagnitude;
            TryPair(segmentB, ClosestPointTriangle(segmentB, a, b, c), ref best, ref pointSegment, ref pointTriangle);
            TryPair(ClosestPointSegment(a, segmentA, segmentB), a, ref best, ref pointSegment, ref pointTriangle);
            TryPair(ClosestPointSegment(b, segmentA, segmentB), b, ref best, ref pointSegment, ref pointTriangle);
            TryPair(ClosestPointSegment(c, segmentA, segmentB), c, ref best, ref pointSegment, ref pointTriangle);
            TrySegmentPair(segmentA, segmentB, a, b, ref best, ref pointSegment, ref pointTriangle);
            TrySegmentPair(segmentA, segmentB, b, c, ref best, ref pointSegment, ref pointTriangle);
            TrySegmentPair(segmentA, segmentB, c, a, ref best, ref pointSegment, ref pointTriangle);
        }

        internal static bool SegmentIntersectsTriangle(
            LVector3 segmentA, LVector3 segmentB, LVector3 a, LVector3 b, LVector3 c,
            out LVector3 intersection)
        {
            var direction = segmentB - segmentA;
            var normal = LVector3.Cross(b - a, c - a);
            var denominator = LVector3.Dot(normal, direction);
            if (LMath.Abs(denominator) <= AxisEpsilon)
            {
                intersection = LVector3.zero;
                return false;
            }

            var t = LVector3.Dot(normal, a - segmentA) / denominator;
            if (t < LFloat.zero || t > LFloat.one)
            {
                intersection = LVector3.zero;
                return false;
            }

            intersection = segmentA + direction * t;
            if (PointInTriangle(intersection, a, b, c, normal)) return true;
            intersection = LVector3.zero;
            return false;
        }

        internal static bool PointInTriangle(
            LVector3 point, LVector3 a, LVector3 b, LVector3 c, LVector3 normal)
        {
            var c0 = LVector3.Dot(normal, LVector3.Cross(b - a, point - a));
            var c1 = LVector3.Dot(normal, LVector3.Cross(c - b, point - b));
            var c2 = LVector3.Dot(normal, LVector3.Cross(a - c, point - c));
            return c0 >= -AxisEpsilon && c1 >= -AxisEpsilon && c2 >= -AxisEpsilon;
        }

        private static void TryPair(
            LVector3 first, LVector3 second, ref LFloat best,
            ref LVector3 pointFirst, ref LVector3 pointSecond)
        {
            var distance = (first - second).sqrMagnitude;
            if (distance >= best) return;
            best = distance;
            pointFirst = first;
            pointSecond = second;
        }

        private static void TrySegmentPair(
            LVector3 a0, LVector3 a1, LVector3 b0, LVector3 b1, ref LFloat best,
            ref LVector3 pointA, ref LVector3 pointB)
        {
            LVector3 candidateA;
            LVector3 candidateB;
            ClosestPointsSegments(a0, a1, b0, b1, out candidateA, out candidateB);
            TryPair(candidateA, candidateB, ref best, ref pointA, ref pointB);
        }

        private static void ClosestFace(
            LVector3 localPoint, LVector3 half, out LVector3 normal, out LFloat distance)
        {
            var dx = half.x - LMath.Abs(localPoint.x);
            var dy = half.y - LMath.Abs(localPoint.y);
            var dz = half.z - LMath.Abs(localPoint.z);
            distance = dx;
            normal = localPoint.x < LFloat.zero ? LVector3.left : LVector3.right;
            if (dy < distance)
            {
                distance = dy;
                normal = localPoint.y < LFloat.zero ? LVector3.down : LVector3.up;
            }
            if (dz < distance)
            {
                distance = dz;
                normal = localPoint.z < LFloat.zero ? LVector3.back : LVector3.forward;
            }
            distance = LMath.Max(LFloat.zero, distance);
        }

        private static readonly int[] BoxTriangleIndices =
        {
            0, 2, 1, 0, 3, 2,
            4, 5, 6, 4, 6, 7,
            0, 1, 5, 0, 5, 4,
            3, 7, 6, 3, 6, 2,
            0, 4, 7, 0, 7, 3,
            1, 2, 6, 1, 6, 5
        };

        private static void ClosestPointsBoxSegment(
            BoxCollision3D box, LVector3 segmentA, LVector3 segmentB,
            out LVector3 pointBox, out LVector3 pointSegment)
        {
            pointBox = GetBoxVertex(box, 0);
            pointSegment = ClosestPointSegment(pointBox, segmentA, segmentB);
            var best = (pointBox - pointSegment).sqrMagnitude;
            for (var i = 0; i < BoxTriangleIndices.Length; i += 3)
            {
                LVector3 candidateSegment;
                LVector3 candidateBox;
                ClosestPointsSegmentTriangle(
                    segmentA, segmentB,
                    GetBoxVertex(box, BoxTriangleIndices[i]),
                    GetBoxVertex(box, BoxTriangleIndices[i + 1]),
                    GetBoxVertex(box, BoxTriangleIndices[i + 2]),
                    out candidateSegment, out candidateBox);
                TryPair(candidateBox, candidateSegment, ref best, ref pointBox, ref pointSegment);
            }
        }

        private static LVector3 GetBoxAxis(BoxCollision3D box, int index)
        {
            switch (index)
            {
                case 0: return box.axisX;
                case 1: return box.axisY;
                default: return box.axisZ;
            }
        }

        private static LVector3 GetBoxVertex(BoxCollision3D box, int index)
        {
            var half = box.halfSize;
            var x = (index == 1 || index == 2 || index == 5 || index == 6) ? half.x : -half.x;
            var y = (index == 2 || index == 3 || index == 6 || index == 7) ? half.y : -half.y;
            var z = index >= 4 ? half.z : -half.z;
            return box.pos + box.axisX * x + box.axisY * y + box.axisZ * z;
        }

        private static bool SegmentIntersectsBox(BoxCollision3D box, LVector3 a, LVector3 b)
        {
            var localA = ToBoxLocal(box, a);
            var localB = ToBoxLocal(box, b);
            var half = box.halfSize;
            if (PointInsideAabb(localA, half) || PointInsideAabb(localB, half)) return true;
            var direction = localB - localA;
            var minT = LFloat.zero;
            var maxT = LFloat.one;
            for (var axis = 0; axis < 3; axis++)
            {
                var origin = localA[axis];
                var delta = direction[axis];
                if (LMath.Abs(delta) <= AxisEpsilon)
                {
                    if (origin < -half[axis] || origin > half[axis]) return false;
                    continue;
                }
                var t1 = (-half[axis] - origin) / delta;
                var t2 = (half[axis] - origin) / delta;
                if (t1 > t2)
                {
                    var swap = t1;
                    t1 = t2;
                    t2 = swap;
                }
                minT = LMath.Max(minT, t1);
                maxT = LMath.Min(maxT, t2);
                if (minT > maxT) return false;
            }
            return true;
        }

        private static bool PointInsideAabb(LVector3 point, LVector3 half)
        {
            return point.x >= -half.x && point.x <= half.x
                && point.y >= -half.y && point.y <= half.y
                && point.z >= -half.z && point.z <= half.z;
        }

        private static LVector3 ToBoxLocal(BoxCollision3D box, LVector3 worldPoint)
        {
            return LQuaternion.Inverse(box.rotation) * (worldPoint - box.pos);
        }

        private static LVector3 FromBoxLocal(BoxCollision3D box, LVector3 localPoint)
        {
            return box.pos + box.rotation * localPoint;
        }
    }
}
