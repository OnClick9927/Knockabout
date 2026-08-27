namespace Lockstep.Collision
{
    /// <summary>
    /// 三维窄相位接触流形的单点表示。
    /// normal 从形状 A 指向形状 B，pointA/pointB 分别位于双方表面，
    /// penetration 为非负穿透深度；feature 用于标识网格三角形等子特征。
    /// </summary>
    public struct CollisionContact3D
    {
        public CollisionContact3D(
            LVector3 normal,
            LVector3 pointA,
            LVector3 pointB,
            LFloat penetration,
            int featureA = -1,
            int featureB = -1)
        {
            this.normal = normal;
            this.pointA = pointA;
            this.pointB = pointB;
            this.penetration = penetration;
            this.featureA = featureA;
            this.featureB = featureB;
        }

        public LVector3 normal { get; private set; }
        public LVector3 pointA { get; private set; }
        public LVector3 pointB { get; private set; }
        public LFloat penetration { get; private set; }
        public int featureA { get; private set; }
        public int featureB { get; private set; }

        /// <summary>交换 A/B 语义，同时反转法线和特征编号。</summary>
        public CollisionContact3D Flipped()
        {
            return new CollisionContact3D(
                -normal,
                pointB,
                pointA,
                penetration,
                featureB,
                featureA);
        }
    }
}
