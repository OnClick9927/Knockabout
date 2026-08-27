// Copyright 2019 谭杰鹏. All Rights Reserved //https://github.com/JiepengTan 

namespace Lockstep
{

#if UNITY_5_3_OR_NEWER
    using UnityEngine;

    /// <summary>
    /// Unity 数值类型与锁步定点类型之间的边界转换。
    /// <para>
    /// 这些方法只应出现在表现层、资源导入和编辑器边界。权威模拟应尽早把 Unity 浮点数据
    /// 量化为定点，并避免在模拟过程中反复转换，否则每次量化都会丢失低于
    /// <see cref="LFloat.Precision"/> 的精度。
    /// </para>
    /// <para>
    /// 本类型及全部 Unity 引用都受 <c>UNITY_5_3_OR_NEWER</c> 宏保护，LockstepEngine 在服务器、
    /// 命令行工具或其他非 Unity 环境中编译时不会引入 UnityEngine 依赖。
    /// </para>
    /// </summary>
    public static partial class LMathExtension
    {
        #region Quaternion

        /// <summary>
        /// 把 Unity 四元数的四个分量直接量化为 Lockstep 定点四元数。
        /// 本方法不主动归一化，能够保留调用方传入的原始分量语义。
        /// </summary>
        public static LQuaternion ToLQuaternion(this Quaternion value)
        {
            return new LQuaternion(
                LMath.ToLFloat(value.x),
                LMath.ToLFloat(value.y),
                LMath.ToLFloat(value.z),
                LMath.ToLFloat(value.w));
        }

        /// <summary>
        /// 把 Lockstep 定点四元数转换为 Unity 四元数。
        /// 本方法只转换分量，不调用 <see cref="Quaternion.Normalize(Quaternion)"/>，避免显示层转换
        /// 擅自修正权威数据；需要单位四元数时应由调用方明确归一化。
        /// </summary>
        public static Quaternion ToQuaternion(this LQuaternion value)
        {
            return new Quaternion(
                value.x.ToFloat(),
                value.y.ToFloat(),
                value.z.ToFloat(),
                value.w.ToFloat());
        }

        #endregion

        #region Integer vectors

        /// <summary>把 Unity 二维整数向量精确转换为同值的二维定点向量。</summary>
        public static LVector2 ToLVector2(this Vector2Int vec)
        {
            return LVector2.CreateFromRaw(
                vec.x * LFloat.Precision,
                vec.y * LFloat.Precision);
        }

        /// <summary>把 Unity 三维整数向量精确转换为同值的三维定点向量。</summary>
        public static LVector3 ToLVector3(this Vector3Int vec)
        {
            return LVector3.CreateFromRaw(
                vec.x * LFloat.Precision,
                vec.y * LFloat.Precision,
                vec.z * LFloat.Precision);
        }

        /// <summary>把 Unity 二维整数向量转换为 Lockstep 二维整数向量，不发生精度变化。</summary>
        public static LVector2Int ToLVector2Int(this Vector2Int vec)
        {
            return new LVector2Int(vec.x, vec.y);
        }

        /// <summary>把 Unity 三维整数向量转换为 Lockstep 三维整数向量，不发生精度变化。</summary>
        public static LVector3Int ToLVector3Int(this Vector3Int vec)
        {
            return new LVector3Int(vec.x, vec.y, vec.z);
        }

        /// <summary>把 Lockstep 二维整数向量转换为 Unity 二维整数向量，不发生精度变化。</summary>
        public static Vector2Int ToVector2Int(this LVector2Int vec)
        {
            return new Vector2Int(vec.x, vec.y);
        }

        /// <summary>把 Lockstep 三维整数向量转换为 Unity 三维整数向量，不发生精度变化。</summary>
        public static Vector3Int ToVector3Int(this LVector3Int vec)
        {
            return new Vector3Int(vec.x, vec.y, vec.z);
        }

        /// <summary>
        /// 把二维定点向量转换为 Unity 二维整数向量。每个分量向零截断，与
        /// <see cref="LFloat.ToInt"/> 以及现有 <c>ToLVector2Int</c> 语义保持一致。
        /// </summary>
        public static Vector2Int ToVector2Int(this LVector2 vec)
        {
            return new Vector2Int(vec.x.ToInt(), vec.y.ToInt());
        }

        /// <summary>
        /// 把三维定点向量转换为 Unity 三维整数向量。每个分量向零截断，与
        /// <see cref="LFloat.ToInt"/> 以及现有 <c>ToLVector3Int</c> 语义保持一致。
        /// </summary>
        public static Vector3Int ToVector3Int(this LVector3 vec)
        {
            return new Vector3Int(vec.x.ToInt(), vec.y.ToInt(), vec.z.ToInt());
        }

        #endregion

        #region Floating-point vectors

        /// <summary>逐分量量化 Unity 二维浮点向量。</summary>
        public static LVector2 ToLVector2(this Vector2 vec)
        {
            return new LVector2(
                LMath.ToLFloat(vec.x),
                LMath.ToLFloat(vec.y));
        }

        /// <summary>逐分量量化 Unity 三维浮点向量。</summary>
        public static LVector3 ToLVector3(this Vector3 vec)
        {
            return new LVector3(
                LMath.ToLFloat(vec.x),
                LMath.ToLFloat(vec.y),
                LMath.ToLFloat(vec.z));
        }

        /// <summary>提取 Unity 三维向量的 X、Z 分量，作为 Lockstep 二维平面坐标。</summary>
        public static LVector2 ToLVector2XZ(this Vector3 vec)
        {
            return new LVector2(
                LMath.ToLFloat(vec.x),
                LMath.ToLFloat(vec.z));
        }

        /// <summary>把 Lockstep 二维定点向量转换为 Unity 二维浮点向量。</summary>
        public static Vector2 ToVector2(this LVector2 vec)
        {
            return new Vector2(vec.x.ToFloat(), vec.y.ToFloat());
        }

        /// <summary>把二维定点向量映射到 Unity XY 平面，Z 固定为 0。</summary>
        public static Vector3 ToVector3(this LVector2 vec)
        {
            return new Vector3(vec.x.ToFloat(), vec.y.ToFloat(), 0f);
        }

        /// <summary>把二维定点向量映射到 Unity XZ 平面，并使用给定定点数作为 Y 高度。</summary>
        public static Vector3 ToVector3XZ(this LVector2 vec, LFloat y)
        {
            return new Vector3(vec.x.ToFloat(), y.ToFloat(), vec.y.ToFloat());
        }

        /// <summary>把二维定点向量映射到 Unity XZ 平面，Y 固定为 0。</summary>
        public static Vector3 ToVector3XZ(this LVector2 vec)
        {
            return new Vector3(vec.x.ToFloat(), 0f, vec.y.ToFloat());
        }

        /// <summary>把 Lockstep 三维定点向量转换为 Unity 三维浮点向量。</summary>
        public static Vector3 ToVector3(this LVector3 vec)
        {
            return new Vector3(vec.x.ToFloat(), vec.y.ToFloat(), vec.z.ToFloat());
        }

        #endregion

        #region Rectangles

        /// <summary>把 Unity 浮点矩形的左下角和尺寸逐分量量化为 Lockstep 矩形。</summary>
        public static LRect ToLRect(this Rect value)
        {
            return new LRect(
                value.position.ToLVector2(),
                value.size.ToLVector2());
        }

        /// <summary>把 Lockstep 矩形转换为 Unity 浮点矩形。</summary>
        public static Rect ToRect(this LRect value)
        {
            return new Rect(value.position.ToVector2(), value.size.ToVector2());
        }

#if UNITY_2017_2_OR_NEWER
        /// <summary>把 Unity 整数矩形精确转换为 Lockstep 矩形。</summary>
        public static LRect ToLRect(this RectInt value)
        {
            return new LRect(
                value.x.ToLFloat(),
                value.y.ToLFloat(),
                value.width.ToLFloat(),
                value.height.ToLFloat());
        }

        /// <summary>
        /// 把 Lockstep 矩形转换为 Unity 整数矩形。位置和尺寸均向零截断；如果需要覆盖原浮点范围，
        /// 应由调用方按业务需求分别使用 Floor/Ceil，而不要依赖本便捷方法。
        /// </summary>
        public static RectInt ToRectInt(this LRect value)
        {
            return new RectInt(
                value.x.ToInt(),
                value.y.ToInt(),
                value.width.ToInt(),
                value.height.ToInt());
        }
#endif

        #endregion

        #region Bounds

        /// <summary>
        /// 把 Unity 轴对齐包围盒转换为 Lockstep 基础包围盒。
        /// 使用 min/max 而不是 center/size，避免量化中心与尺寸后再次运算造成额外误差。
        /// </summary>
        public static LBounds ToLBounds(this Bounds value)
        {
            var result = new LBounds();
            result.SetMinMax(value.min.ToLVector3(), value.max.ToLVector3());
            return result;
        }

        /// <summary>把 Lockstep 轴对齐包围盒转换为 Unity Bounds。</summary>
        public static Bounds ToBounds(this LBounds value)
        {
            var result = new Bounds();
            result.SetMinMax(value.min.ToVector3(), value.max.ToVector3());
            return result;
        }

#if UNITY_2017_2_OR_NEWER
        /// <summary>把 Unity 整数包围盒精确转换为 Lockstep 基础包围盒。</summary>
        public static LBounds ToLBounds(this BoundsInt value)
        {
            var result = new LBounds();
            result.SetMinMax(value.min.ToLVector3(), value.max.ToLVector3());
            return result;
        }

        /// <summary>
        /// 把 Lockstep 包围盒转换为 Unity 整数包围盒。最小角和尺寸均向零截断；该方法适合
        /// 整数网格数据，若原包围盒包含小数且要求完整覆盖，应由调用方显式选择取整方向。
        /// </summary>
        public static BoundsInt ToBoundsInt(this LBounds value)
        {
            return new BoundsInt(
                value.min.ToVector3Int(),
                value.size.ToVector3Int());
        }
#endif

        #endregion

        #region Matrices and axes

        /// <summary>
        /// 提取 Unity Matrix4x4 左上角的 3×3 线性变换并量化为 <see cref="LMatrix33"/>。
        /// 第四列平移、最后一行齐次/透视分量不会进入结果。
        /// </summary>
        public static LMatrix33 ToLMatrix33(this Matrix4x4 value)
        {
            var result = new LMatrix33();
            result[0, 0] = LMath.ToLFloat(value.m00);
            result[0, 1] = LMath.ToLFloat(value.m01);
            result[0, 2] = LMath.ToLFloat(value.m02);
            result[1, 0] = LMath.ToLFloat(value.m10);
            result[1, 1] = LMath.ToLFloat(value.m11);
            result[1, 2] = LMath.ToLFloat(value.m12);
            result[2, 0] = LMath.ToLFloat(value.m20);
            result[2, 1] = LMath.ToLFloat(value.m21);
            result[2, 2] = LMath.ToLFloat(value.m22);
            return result;
        }

        /// <summary>
        /// 把 Lockstep 3×3 矩阵嵌入 Unity Matrix4x4 的左上角。
        /// 平移固定为零，齐次分量 m33 固定为 1，因此结果只表达旋转、缩放或其他线性变换。
        /// </summary>
        public static Matrix4x4 ToMatrix4x4(this LMatrix33 value)
        {
            Matrix4x4 result = Matrix4x4.identity;
            result.m00 = value[0, 0].ToFloat();
            result.m01 = value[0, 1].ToFloat();
            result.m02 = value[0, 2].ToFloat();
            result.m10 = value[1, 0].ToFloat();
            result.m11 = value[1, 1].ToFloat();
            result.m12 = value[1, 2].ToFloat();
            result.m20 = value[2, 0].ToFloat();
            result.m21 = value[2, 1].ToFloat();
            result.m22 = value[2, 2].ToFloat();
            return result;
        }

        /// <summary>
        /// 把 Unity Matrix4x4 的前两个列向量转换为 Lockstep 二维坐标基。
        /// 平移、第三轴和透视分量被忽略；方法不归一化或正交化输入轴。
        /// </summary>
        public static LAxis2D ToLAxis2D(this Matrix4x4 value)
        {
            return new LAxis2D(
                new Vector3(value.m00, value.m10, value.m20).ToLVector3(),
                new Vector3(value.m01, value.m11, value.m21).ToLVector3());
        }

        /// <summary>
        /// 把 Lockstep 二维坐标基写入 Unity Matrix4x4 的前两列，并以 <c>cross(x, y)</c>
        /// 生成第三轴。平移固定为零；输入轴不会被归一化，非正交输入也会按原值保留。
        /// </summary>
        public static Matrix4x4 ToMatrix4x4(this LAxis2D value)
        {
            LVector3 z = LMath.Cross(value.x, value.y);
            return new LMatrix33(value.x, value.y, z).ToMatrix4x4();
        }

        /// <summary>
        /// 把 Unity Matrix4x4 的前三个列向量转换为 Lockstep 三维坐标基。
        /// 平移和透视分量被忽略；方法不归一化或正交化，调用方应保证输入矩阵符合坐标基要求。
        /// </summary>
        public static LAxis3D ToLAxis3D(this Matrix4x4 value)
        {
            return new LAxis3D(
                new Vector3(value.m00, value.m10, value.m20).ToLVector3(),
                new Vector3(value.m01, value.m11, value.m21).ToLVector3(),
                new Vector3(value.m02, value.m12, value.m22).ToLVector3());
        }

        /// <summary>
        /// 把 Lockstep 三维坐标基写入 Unity Matrix4x4 的前三列。
        /// 平移固定为零，最后一列保持齐次单位列；轴向量不会被自动归一化。
        /// </summary>
        public static Matrix4x4 ToMatrix4x4(this LAxis3D value)
        {
            return new LMatrix33(value.x, value.y, value.z).ToMatrix4x4();
        }

        #endregion
    }
#endif
    /// <summary>
    /// 确定性数学函数集合。
    /// 三角函数使用预生成查表，平方根使用整数算法，其余运算只依赖 LFloat/整数，
    /// 从而让不同 CPU 和运行时得到相同结果。除特别注明外，角度参数均为弧度。
    /// </summary>
    public static class LMath
    {
        public static bool IsSame(LVector3 a, LVector3 b, LFloat epsilon)
        {
            return LMath.Abs(a.x - b.x) <= epsilon &&
        LMath.Abs(a.y - b.y) <= epsilon &&
        LMath.Abs(a.z - b.z) <= epsilon;
        }

        // 以下 long 常量均已乘以 LFloat.Precision，可直接通过 FromRaw 包装。
        public const long LPIQuad = 785398L;  //0.7853981
        public const long LPIHalf = 1570796L;  //1.5707963
        public const long LPI = 3141593L;  //3.1415926
        public const long LPI2 = 6283185L;  //6.2831853
        public const long LRad2Deg = 57295780L;  //57.2957795
        public const long LDeg2Rad = 17453L;  //0.0174532
        //Precision = 1000000
        public static readonly LFloat PIQuad = LFloat.FromRaw(LPIQuad);
        public static readonly LFloat PIHalf = LFloat.FromRaw(LPIHalf);
        public static readonly LFloat PI = LFloat.FromRaw(LPI);
        public static readonly LFloat PI2 = LFloat.FromRaw(LPI2);
        public static readonly LFloat Rad2Deg = LFloat.FromRaw(LRad2Deg);
        public static readonly LFloat Deg2Rad = LFloat.FromRaw(LDeg2Rad);
        public static LFloat Pi => PI;

        #region Atan2
        /// <summary>
        /// 原始定点版本的 atan2。先根据 x/y 符号和绝对值确定象限，
        /// 再把比值归约到查表覆盖区间，返回值是放大后的弧度原始值。
        /// </summary>
        public static long _Atan2(long y, long x)
        {
            //特殊情况处理
            if (y == 0)
            {
                if (x == 0)
                {
                    return 0;
                }

                return x < 0 ? LMath.LPI : 0;
            }

            if (x == 0)
            {
                return y > 0 ? LMath.LPIHalf : -LMath.LPIHalf;
            }

            //决定象限
            int idxV = 0;
            if (x < 0)
            {
                x = -x;
                idxV += 4;
            }

            if (y < 0)
            {
                y = -y;
                idxV += 2;
            }

            LFloat factor = 0;
            if (y > x)
            {
                idxV += 1;
                factor = new LFloat(y) / x;
            }
            else
            {
                factor = new LFloat(x) / y;
            }

            //逆时针 idx 为 0 1 5 4 6 7 3 2
            var info = idx2LutInfo[idxV];
            if (x == y)
            {
                return info.offset;
            }
            var deg = _LutATan(factor) - LMath.LPIQuad;
            return info.sign * deg + info.offset;
        }

        // 三个判定位组合成 0..7：x 符号、y 符号以及 |y| 是否大于 |x|。
        // 每项给出查表结果映射回真实象限时需要的符号和角度偏移。
        private static LutAtan2Helper[] idx2LutInfo = new LutAtan2Helper[] {
            new LutAtan2Helper(-1, LMath.LPIQuad),
            new LutAtan2Helper(1, LMath.LPIQuad),
            new LutAtan2Helper(1, -LMath.LPIQuad),
            new LutAtan2Helper(-1, -LMath.LPIQuad),

            new LutAtan2Helper(1, LMath.LPIQuad * 3),
            new LutAtan2Helper(-1, LMath.LPIQuad * 3),
            new LutAtan2Helper(-1, -LMath.LPIQuad * 3),
            new LutAtan2Helper(1, -LMath.LPIQuad * 3),
        };
        public struct LutAtan2Helper
        {
            public long sign;
            public long offset;

            public LutAtan2Helper(long sign, long offset)
            {
                this.sign = sign;
                this.offset = offset;
            }
        }

        public static long _LutATan(LFloat ydx)
        {
            Debug.Assert(ydx >= 1, $"{ydx} Need >=1");
            if (ydx >= LUTAtan2.MaxQueryIdx) return LMath.LPIHalf;
            var iydx = (int)ydx;
            var startIdx = LUTAtan2._startIdx[iydx - 1];
            var size = LUTAtan2._arySize[iydx - 1];
            var remaind = ydx - iydx;
            var idx = startIdx + (int)(remaind * size);
            return LUTAtan2._tblTbl[idx];
        }
        #endregion

        public static LFloat Atan2(LFloat y, LFloat x)
        {
            return Atan2(y._val, x._val);
        }

        public static LFloat Atan2(long y, long x)
        {
            return LFloat.FromRaw(_Atan2(y, x));
        }

        /// <summary>反余弦，输入会限制在 [-1, 1]，返回弧度。</summary>
        public static LFloat Acos(LFloat val)
        {
            int idx = (int)(val._val * LUTAcos.HALF_COUNT / LFloat.Precision) +
                      LUTAcos.HALF_COUNT;
            idx = Clamp(idx, 0, LUTAcos.COUNT);
            return LFloat.FromRaw(LUTAcos.table[idx]);
        }

        /// <summary>反正弦，输入会限制在 [-1, 1]，返回弧度。</summary>
        public static LFloat Asin(LFloat val)
        {
            int idx = (int)(val._val * LUTAsin.HALF_COUNT / LFloat.Precision) +
                      LUTAsin.HALF_COUNT;
            idx = Clamp(idx, 0, LUTAsin.COUNT);
            return LFloat.FromRaw(LUTAsin.table[idx]);
        }

        //ccw
        /// <summary>通过周期归约和查表计算正弦，参数单位为弧度。</summary>
        public static LFloat Sin(LFloat radians)
        {
            return LFloat.FromRaw(LUTSin.table[_GetIdx(radians)]);
        }

        //ccw
        /// <summary>通过周期归约和查表计算余弦，参数单位为弧度。</summary>
        public static LFloat Cos(LFloat radians)
        {
            return LFloat.FromRaw(LUTCos.table[_GetIdx(radians)]);
        }

        private static int _GetIdx(LFloat radians)
        {
            var rawVal = radians._val % LMath.LPI2;
            if (rawVal < 0) rawVal += LMath.LPI2;
            var val = LFloat.FromRaw(rawVal) / LMath.PI2;
            var idx = (int)(val * LUTCos.COUNT);
            idx = Clamp(idx, 0, LUTCos.COUNT);
            return idx;
        }

        //ccw
        public static void SinCos(out LFloat s, out LFloat c, LFloat radians)
        {
            int idx = _GetIdx(radians);
            s = LFloat.FromRaw(LUTSin.table[idx]);
            c = LFloat.FromRaw(LUTCos.table[idx]);
        }
        /// <summary>不使用浮点的 32 位无符号整数平方根，结果向下取整。</summary>
        public static uint Sqrt32(uint a)
        {
            ulong rem = 0;
            ulong root = 0;
            ulong divisor = 0;
            for (int i = 0; i < 16; i++)
            {
                root <<= 1;
                rem = ((rem << 2) + (a >> 30));
                a <<= 2;
                divisor = (root << 1) + 1;
                if (divisor <= rem)
                {
                    rem -= divisor;
                    root++;
                }
            }
            return (uint)root;
        }
        //x = 2*p + q  
        //x^2 = 4*p^2 + 4pq + q^2
        //q = (x^2 - 4*p^2)/(4*p+q)  
        //https://www.cnblogs.com/10cm/p/3922398.html
        /// <summary>不使用浮点的 64 位无符号整数平方根，结果向下取整。</summary>
        public static uint Sqrt64(ulong a)
        {
            ulong rem = 0;
            ulong root = 0;
            ulong divisor = 0;
            for (int i = 0; i < 32; i++)
            {
                root <<= 1;
                rem = ((rem << 2) + (a >> 62));//(x^2 - 4*p^2)  
                a <<= 2;
                divisor = (root << 1) + 1; //(4*p+q) 
                if (divisor <= rem)
                {
                    rem -= divisor;
                    root++;
                }
            }
            return (uint)root;
        }
        public static int Sqrt(int a)
        {
            if (a <= 0)
            {
                return 0;
            }

            return (int)LMath.Sqrt32((uint)a);
        }

        public static long Sqrt(long a)
        {
            if (a <= 0L)
            {
                return 0;
            }

            if (a <= (long)(0xffffffffu))
            {
                return (long)LMath.Sqrt32((uint)a);
            }

            return (long)LMath.Sqrt64((ulong)a);
        }

        public static LFloat Sqrt(LFloat a)
        {
            if (a._val <= 0)
            {
                return LFloat.zero;
            }

            return LFloat.FromRaw(Sqrt((long)a._val * LFloat.Precision));
        }

        public static LFloat Sqr(LFloat a)
        {
            return a * a;
        }


        public static uint RoundPowOfTwo(uint x)
        {
            if (x <= 1) return 1;
            if (x > 0x80000000U) return 0;

            x--;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return x + 1;
        }
        public static ulong RoundPowOfTwo(ulong x)
        {
            if (x <= 1) return 1;
            if (x > 0x8000000000000000UL) return 0;

            x--;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            x |= x >> 32;
            return x + 1;
        }

        public static int Clamp(int value, int min, int max)
        {
            if (value < min)
                value = min;
            else if (value > max)
                value = max;
            return value;
        }

        public static long Clamp(long a, long min, long max)
        {
            if (a < min)
            {
                return min;
            }

            if (a > max)
            {
                return max;
            }

            return a;
        }

        public static LFloat Clamp(LFloat a, LFloat min, LFloat max)
        {
            if (a < min)
            {
                return min;
            }

            if (a > max)
            {
                return max;
            }

            return a;
        }
        public static LFloat Clamp01(LFloat a)
        {
            if (a < LFloat.zero)
            {
                return LFloat.zero;
            }

            if (a > LFloat.one)
            {
                return LFloat.one;
            }

            return a;
        }


        public static bool SameSign(LFloat a, LFloat b)
        {
            return a._val > 0 && b._val > 0 || a._val < 0 && b._val < 0;
        }

        public static int Abs(int val)
        {
            if (val < 0)
            {
                if (val == int.MinValue) return int.MaxValue;
                return -val;
            }

            return val;
        }

        public static long Abs(long val)
        {
            if (val < 0L)
            {
                if (val == long.MinValue) return long.MaxValue;
                return -val;
            }

            return val;
        }

        public static LFloat Abs(LFloat val)
        {
            if (val._val < 0)
            {
                if (val._val == long.MinValue) return LFloat.MaxValue;
                return LFloat.FromRaw(-val._val);
            }

            return val;
        }

        public static int Sign(LFloat val)
        {
            return System.Math.Sign(val._val);
        }

        public static LFloat Round(LFloat val)
        {
            if (val <= 0)
            {
                var remainder = (-val._val) % LFloat.Precision;
                if (remainder > LFloat.HalfPrecision)
                {
                    return LFloat.FromRaw(val._val + remainder - LFloat.Precision);
                }
                else
                {
                    return LFloat.FromRaw(val._val + remainder);
                }
            }
            else
            {
                var remainder = (val._val) % LFloat.Precision;
                if (remainder > LFloat.HalfPrecision)
                {
                    return LFloat.FromRaw(val._val - remainder + LFloat.Precision);
                }
                else
                {
                    return LFloat.FromRaw(val._val - remainder);
                }
            }
        }

        public static long Max(long a, long b)
        {
            return (a <= b) ? b : a;
        }

        public static int Max(int a, int b)
        {
            return (a <= b) ? b : a;
        }

        public static long Min(long a, long b)
        {
            return (a > b) ? b : a;
        }

        public static int Min(int a, int b)
        {
            return (a > b) ? b : a;
        }
        public static int Min(params int[] values)
        {
            int length = values.Length;
            if (length == 0)
                return 0;
            int num = values[0];
            for (int index = 1; index < length; ++index)
            {
                if (values[index] < num)
                    num = values[index];
            }
            return num;
        }
        public static LFloat Min(params LFloat[] values)
        {
            int length = values.Length;
            if (length == 0)
                return LFloat.zero;
            LFloat num = values[0];
            for (int index = 1; index < length; ++index)
            {
                if (values[index] < num)
                    num = values[index];
            }
            return num;
        }
        public static int Max(params int[] values)
        {
            int length = values.Length;
            if (length == 0)
                return 0;
            int num = values[0];
            for (int index = 1; index < length; ++index)
            {
                if (values[index] > num)
                    num = values[index];
            }
            return num;
        }

        public static LFloat Max(params LFloat[] values)
        {
            int length = values.Length;
            if (length == 0)
                return LFloat.zero;
            var num = values[0];
            for (int index = 1; index < length; ++index)
            {
                if (values[index] > num)
                    num = values[index];
            }
            return num;
        }

        public static int FloorToInt(LFloat a)
        {
            var val = a._val;
            if (val < 0)
            {
                val = val - LFloat.Precision + 1;
            }
            return (int)(val / LFloat.Precision);
        }

        /// <summary>
        /// 表现层浮点数到定点数的显式边界转换；会按 Precision 截断到可表示精度。
        /// </summary>
        public static LFloat ToLFloat(float a)
        {
            return LFloat.FromRaw((long)(a * LFloat.Precision));
        }
        /// <summary>把双精度浮点数截断量化为 LFloat 的百万分之一精度。</summary>
        public static LFloat ToLFloat(double a)
        {
            return LFloat.FromRaw((long)(a * LFloat.Precision));
        }
        public static LFloat ToLFloat(int a)
        {
            return LFloat.FromRaw((long)(a * LFloat.Precision));
        }
        public static LFloat ToLFloat(long a)
        {
            return LFloat.FromRaw((long)(a * LFloat.Precision));
        }

        public static LFloat Min(LFloat a, LFloat b)
        {
            return LFloat.FromRaw(Min(a._val, b._val));
        }

        public static LFloat Max(LFloat a, LFloat b)
        {
            return LFloat.FromRaw(Max(a._val, b._val));
        }

        public static LFloat Lerp(LFloat a, LFloat b, LFloat f)
        {
            return LFloat.FromRaw((((long)(b._val - a._val) * f._val) / LFloat.Precision) + a._val);
        }

        public static LFloat InverseLerp(LFloat a, LFloat b, LFloat value)
        {
            if (a != b)
                return Clamp01(((value - a) / (b - a)));
            return LFloat.zero;
        }
        public static LVector2 Lerp(LVector2 a, LVector2 b, LFloat f)
        {
            return LVector2.CreateFromRaw(
                (((long)(b._x - a._x) * f._val) / LFloat.Precision) + a._x,
                (((long)(b._y - a._y) * f._val) / LFloat.Precision) + a._y);
        }

        public static LVector3 Lerp(LVector3 a, LVector3 b, LFloat f)
        {
            return LVector3.CreateFromRaw(
                (((long)(b._x - a._x) * f._val) / LFloat.Precision) + a._x,
                (((long)(b._y - a._y) * f._val) / LFloat.Precision) + a._y,
                (((long)(b._z - a._z) * f._val) / LFloat.Precision) + a._z);
        }

        public static bool IsPowerOfTwo(int x)
        {
            return (x & x - 1) == 0;
        }

        public static int CeilPowerOfTwo(int x)
        {
            x--;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            x++;
            return x;
        }

        public static LFloat Dot(LVector2 u, LVector2 v)
        {
            return LFloat.FromRaw(((long)u._x * v._x + (long)u._y * v._y) / LFloat.Precision);
        }

        public static LFloat Dot(LVector3 lhs, LVector3 rhs)
        {
            var val = ((long)lhs._x) * rhs._x + ((long)lhs._y) * rhs._y + ((long)lhs._z) * rhs._z;
            return LFloat.FromRaw(val / LFloat.Precision);
            ;
        }
        public static LVector3 Cross(LVector3 lhs, LVector3 rhs)
        {
            return LVector3.CreateFromRaw(
                ((long)lhs._y * rhs._z - (long)lhs._z * rhs._y) / LFloat.Precision,
                ((long)lhs._z * rhs._x - (long)lhs._x * rhs._z) / LFloat.Precision,
                ((long)lhs._x * rhs._y - (long)lhs._y * rhs._x) / LFloat.Precision
            );
        }

        public static LFloat Cross2D(LVector2 u, LVector2 v)
        {
            return LFloat.FromRaw(((long)u._x * v._y - (long)u._y * v._x) / LFloat.Precision);
        }
        public static LFloat Dot2D(LVector2 u, LVector2 v)
        {
            return LFloat.FromRaw(((long)u._x * v._x + (long)u._y * v._y) / LFloat.Precision);
        }


        public static LVector3 Transform(ref LVector3 point, ref LVector3 axis_x, ref LVector3 axis_y, ref LVector3 axis_z,
            ref LVector3 trans)
        {
            return LVector3.CreateFromRaw(
                ((axis_x._x * point._x + axis_y._x * point._y + axis_z._x * point._z) / LFloat.Precision) + trans._x,
                ((axis_x._y * point._x + axis_y._y * point._y + axis_z._y * point._z) / LFloat.Precision) + trans._y,
                ((axis_x._z * point._x + axis_y._z * point._y + axis_z._z * point._z) / LFloat.Precision) + trans._z);
        }

        public static LVector3 Transform(LVector3 point, ref LVector3 axis_x, ref LVector3 axis_y, ref LVector3 axis_z,
            ref LVector3 trans)
        {
            return LVector3.CreateFromRaw(
                ((axis_x._x * point._x + axis_y._x * point._y + axis_z._x * point._z) / LFloat.Precision) + trans._x,
                ((axis_x._y * point._x + axis_y._y * point._y + axis_z._y * point._z) / LFloat.Precision) + trans._y,
                ((axis_x._z * point._x + axis_y._z * point._y + axis_z._z * point._z) / LFloat.Precision) + trans._z);
        }

        public static LVector3 Transform(ref LVector3 point, ref LVector3 axis_x, ref LVector3 axis_y, ref LVector3 axis_z,
            ref LVector3 trans, ref LVector3 scale)
        {
            long num = (long)point._x * (long)scale._x / LFloat.Precision;
            long num2 = (long)point._y * (long)scale._y / LFloat.Precision;
            long num3 = (long)point._z * (long)scale._z / LFloat.Precision;
            return LVector3.CreateFromRaw(
                (((long)axis_x._x * num + (long)axis_y._x * num2 + (long)axis_z._x * num3) / LFloat.Precision) +
                trans._x,
                (((long)axis_x._y * num + (long)axis_y._y * num2 + (long)axis_z._y * num3) / LFloat.Precision) +
                trans._y,
                (((long)axis_x._z * num + (long)axis_y._z * num2 + (long)axis_z._z * num3) / LFloat.Precision) +
                trans._z);
        }

        public static LVector3 Transform(ref LVector3 point, ref LVector3 forward, ref LVector3 trans)
        {
            LVector3 up = LVector3.up;
            LVector3 vInt = Cross(LVector3.up, forward);
            return LMath.Transform(ref point, ref vInt, ref up, ref forward, ref trans);
        }

        public static LVector3 Transform(LVector3 point, LVector3 forward, LVector3 trans)
        {
            LVector3 up = LVector3.up;
            LVector3 vInt = Cross(LVector3.up, forward);
            return LMath.Transform(ref point, ref vInt, ref up, ref forward, ref trans);
        }

        public static LVector3 Transform(LVector3 point, LVector3 forward, LVector3 trans, LVector3 scale)
        {
            LVector3 up = LVector3.up;
            LVector3 vInt = Cross(LVector3.up, forward);
            return LMath.Transform(ref point, ref vInt, ref up, ref forward, ref trans, ref scale);
        }

        public static LVector3 MoveTowards(LVector3 from, LVector3 to, LFloat dt)
        {
            if ((to - from).sqrMagnitude <= (dt * dt))
            {
                return to;
            }

            return from + (to - from).Normalize(dt);
        }


        public static LFloat AngleInt(LVector3 lhs, LVector3 rhs)
        {
            return LMath.Acos(Dot(lhs, rhs));
        }



    }

    public static partial class LMathExtension
    {
        public static LVector3 ToLVector3XZ(this LVector2 vec)
        {
            return new LVector3(vec.x, LFloat.zero, vec.y);
        }
        public static LVector2 ToLVector2XZ(this LVector3 vec)
        {
            return new LVector2(vec.x, vec.z);
        }
        public static LVector2 ToLVector2(this LVector2Int vec)
        {
            return LVector2.CreateFromRaw(vec.x * LFloat.Precision, vec.y * LFloat.Precision);
        }

        public static LVector3 ToLVector3(this LVector3Int vec)
        {
            return LVector3.CreateFromRaw(vec.x * LFloat.Precision, vec.y * LFloat.Precision, vec.z * LFloat.Precision);
        }

        public static LVector2Int ToLVector2Int(this LVector2 vec)
        {
            return new LVector2Int(vec.x.ToInt(), vec.y.ToInt());
        }

        public static LVector3Int ToLVector3Int(this LVector3 vec)
        {
            return new LVector3Int(vec.x.ToInt(), vec.y.ToInt(), vec.z.ToInt());
        }

        public static LFloat ToLFloat(this float v)
        {
            return LMath.ToLFloat(v);
        }

        /// <summary>把双精度浮点数截断量化为 LFloat 的百万分之一精度。</summary>
        public static LFloat ToLFloat(this double v)
        {
            return LMath.ToLFloat(v);
        }

        public static LFloat ToLFloat(this int v)
        {
            return LMath.ToLFloat(v);
        }

        public static LFloat ToLFloat(this long v)
        {
            return LMath.ToLFloat(v);
        }
        public static LVector2Int Floor(this LVector2 vec)
        {
            return new LVector2Int(LMath.FloorToInt(vec.x), LMath.FloorToInt(vec.y));
        }

        public static LVector3Int Floor(this LVector3 vec)
        {
            return new LVector3Int(
                LMath.FloorToInt(vec.x),
                LMath.FloorToInt(vec.y),
                LMath.FloorToInt(vec.z)
            );
        }
        public static LVector2 RightVec(this LVector2 vec)
        {
            return LVector2.CreateFromRaw(vec._y, -vec._x);
        }

        public static LVector2 LeftVec(this LVector2 vec)
        {
            return LVector2.CreateFromRaw(-vec._y, vec._x);
        }

        public static LVector2 BackVec(this LVector2 vec)
        {
            return LVector2.CreateFromRaw(-vec._x, -vec._y);
        }


        public static LFloat Abs(this LFloat val)
        {
            return LMath.Abs(val);
        }
    }



}
