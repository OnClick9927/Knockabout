// Copyright 2019 谭杰鹏. All Rights Reserved //https://github.com/JiepengTan 

using System;
using System.Runtime.CompilerServices;


namespace Lockstep
{
    /// <summary>
    /// 三维定点向量，是 3D 碰撞、导航网格和空间变换的基础数据结构。
    /// 内部直接保存三个 LFloat 原始值，向量长度、点积和叉积均使用确定性整数运算。
    /// </summary>
    [Serializable]
    public struct LVector3 : IEquatable<LVector3>
    {
        public LFloat x
        {
            get { return  LFloat.FromRaw(  _x); }
            set { _x = value._val ; }
        }

        public LFloat y
        {
            get { return  LFloat.FromRaw(  _y); }
            set { _y = value._val ; }
        }

        public LFloat z
        {
            get { return  LFloat.FromRaw(  _z); }
            set { _z = value._val ; }
        }

        /// <summary>三个分量的原始定点值，普通业务代码应优先访问 x/y/z 属性。</summary>
        public long _x;
        public long _y;
        public long _z;


        public static readonly LVector3 zero = CreateFromRaw(0, 0, 0);
        public static readonly LVector3 one = CreateFromRaw(LFloat.Precision, LFloat.Precision, LFloat.Precision);
        public static readonly LVector3 half = CreateFromRaw(LFloat.Precision / 2, LFloat.Precision / 2,LFloat.Precision / 2);
        
        public static readonly LVector3 forward = CreateFromRaw(0, 0, LFloat.Precision);
        public static readonly LVector3 up = CreateFromRaw(0, LFloat.Precision, 0);
        public static readonly LVector3 right = CreateFromRaw(LFloat.Precision, 0, 0);
        public static readonly LVector3 back = CreateFromRaw(0, 0, -LFloat.Precision);
        public static readonly LVector3 down = CreateFromRaw(0, -LFloat.Precision, 0);
        public static readonly LVector3 left = CreateFromRaw(-LFloat.Precision, 0, 0);
        
       
        /// <summary>
        /// 将这些值作为内部值 直接构造(高效) （仅用于内部实现，外部不建议使用）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <summary>根据已经乘过 Precision 的原始分量创建向量，不进行二次缩放。</summary>
        public static LVector3 CreateFromRaw(long x, long y, long z)
        {
            return new LVector3 { _x = x, _y = y, _z = z };
        }

        public LVector3(long _x, long _y, long _z)
        {
            this._x = _x * LFloat.Precision;
            this._y = _y * LFloat.Precision;
            this._z = _z * LFloat.Precision;
        }
        public LVector3(LFloat x, LFloat y, LFloat z)
        {
            this._x = x._val;
            this._y = y._val;
            this._z = z._val;
        }
        #if UNITY_EDITOR
        /// <summary>
        /// 直接使用浮点型 进行构造 警告!!! 仅应该在Editor模式下使用，不应该在正式代码中使用,避免出现引入浮点的不确定性
        /// </summary>
        public static LVector3 CreateFromFloat(float x, float y, float z)
        {
            return CreateFromRaw(
                (long)(x * LFloat.Precision),
                (long)(y * LFloat.Precision),
                (long)(z * LFloat.Precision));
        }
        #endif

        public LFloat magnitude
        {
            get
            {
                return  LFloat.FromRaw(  LMath.Sqrt(_x * _x + _y * _y + _z * _z));
            }
        }


        public LFloat sqrMagnitude
        {
            get
            {
                return  LFloat.FromRaw(  (_x * _x + _y * _y + _z * _z) / LFloat.Precision);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long RawSqrMagnitude() => (_x * _x + _y * _y + _z * _z);

        public LVector3 abs
        {
            get { return CreateFromRaw(LMath.Abs(this._x), LMath.Abs(this._y), LMath.Abs(this._z)); }
        }

        public LVector3 Normalize()
        {
            return Normalize((LFloat) 1);
        }

        public LVector3 Normalize(LFloat newMagn)
        {
            long sqr = _x * _x + _y * _y + _z * _z;
            if (sqr == 0L)
            {
                return LVector3.zero;
            }
            long b = LMath.Sqrt(sqr);
            _x = (_x * newMagn._val / b);
            _y = (_y * newMagn._val / b);
            _z = (_z * newMagn._val / b);
            return this;
        }

        public LVector3 normalized
        {
            get
            {
                long sqr = _x * _x + _y * _y + _z * _z;
                if (sqr == 0L)
                {
                    return LVector3.zero;
                }

                var ret = new LVector3();
                long b = LMath.Sqrt(sqr);
                ret._x = (_x * LFloat.Precision / b);
                ret._y = (_y * LFloat.Precision / b);
                ret._z = (_z * LFloat.Precision / b);
                return ret;
            }
        }

        public static bool operator ==(LVector3 lhs, LVector3 rhs)
        {
            return lhs._x == rhs._x && lhs._y == rhs._y && lhs._z == rhs._z;
        }

        public static bool operator !=(LVector3 lhs, LVector3 rhs)
        {
            return lhs._x != rhs._x || lhs._y != rhs._y || lhs._z != rhs._z;
        }

        public static LVector3 operator -(LVector3 lhs, LVector3 rhs)
        {
            lhs._x -= rhs._x;
            lhs._y -= rhs._y;
            lhs._z -= rhs._z;
            return lhs;
        }

        public static LVector3 operator -(LVector3 lhs)
        {
            lhs._x = -lhs._x;
            lhs._y = -lhs._y;
            lhs._z = -lhs._z;
            return lhs;
        }

        public static LVector3 operator +(LVector3 lhs, LVector3 rhs)
        {
            lhs._x += rhs._x;
            lhs._y += rhs._y;
            lhs._z += rhs._z;
            return lhs;
        }

        public static LVector3 operator *(LVector3 lhs, LVector3 rhs)
        {
            lhs._x = ((long) (lhs._x * rhs._x)) / LFloat.Precision;
            lhs._y = ((long) (lhs._y * rhs._y)) / LFloat.Precision;
            lhs._z = ((long) (lhs._z * rhs._z)) / LFloat.Precision;
            return lhs;
        }

        public static LVector3 operator *(LVector3 lhs, LFloat rhs)
        {
            lhs._x = ((long) (lhs._x * rhs._val)) / LFloat.Precision;
            lhs._y = ((long) (lhs._y * rhs._val)) / LFloat.Precision;
            lhs._z = ((long) (lhs._z * rhs._val)) / LFloat.Precision;
            return lhs;
        }

        public static LVector3 operator /(LVector3 lhs, LFloat rhs)
        {
            lhs._x = ((long) lhs._x * LFloat.Precision) / rhs._val;
            lhs._y = ((long) lhs._y * LFloat.Precision) / rhs._val;
            lhs._z = ((long) lhs._z * LFloat.Precision) / rhs._val;
            return lhs;
        }
        
        public static LVector3 operator *(LFloat rhs,LVector3 lhs)
        {
            lhs._x = ((long) (lhs._x * rhs._val)) / LFloat.Precision;
            lhs._y = ((long) (lhs._y * rhs._val)) / LFloat.Precision;
            lhs._z = ((long) (lhs._z * rhs._val)) / LFloat.Precision;
            return lhs;
        }

        public override string ToString()
        {
            return string.Format("({0},{1},{2})", _x * LFloat.PrecisionFactor, _y * LFloat.PrecisionFactor,
                _z * LFloat.PrecisionFactor);
        }

        public override bool Equals(object o)
        {
            return o is LVector3 other && Equals(other);
        }


        public bool Equals(LVector3 other)
        {
            return this._x == other._x && this._y == other._y && this._z == other._z;
        }


        public override int GetHashCode()
        {
            return (int)(this._x * 73856093 ^ this._y * 19349663 ^ this._z * 83492791);
        }

        
        public LFloat this[int index]

        {

            get
            {
                switch (index)
                {
                    case 0: return x;
                    case 1: return y;
                    case 2: return z;
                    default: throw new IndexOutOfRangeException("vector idx invalid" + index);
                }
            }

            set
            {
                switch (index)
                {
                    case 0: _x = value._val; break;
                    case 1: _y = value._val;break;
                    case 2: _z = value._val;break;
                    default: throw new IndexOutOfRangeException("vector idx invalid" + index);
                }
            }

        }

        public static LFloat Dot(ref LVector3 lhs, ref LVector3 rhs)
        {
            var val = ((long) lhs._x) * rhs._x + ((long) lhs._y) * rhs._y + ((long) lhs._z) * rhs._z;
            return  LFloat.FromRaw(  val / LFloat.Precision);
        }

        public static LFloat Dot(LVector3 lhs, LVector3 rhs)
        {
            var val = ((long) lhs._x) * rhs._x + ((long) lhs._y) * rhs._y + ((long) lhs._z) * rhs._z;
            return  LFloat.FromRaw(  val / LFloat.Precision);
            ;
        }
        
        public static LVector3 Cross(ref LVector3 lhs, ref LVector3 rhs)
        {
            return CreateFromRaw(
                ((long) lhs._y * rhs._z - (long) lhs._z * rhs._y) / LFloat.Precision,
                ((long) lhs._z * rhs._x - (long) lhs._x * rhs._z) / LFloat.Precision,
                ((long) lhs._x * rhs._y - (long) lhs._y * rhs._x) / LFloat.Precision
            );
        }

        public static LVector3 Cross(LVector3 lhs, LVector3 rhs)
        {
            return CreateFromRaw(
                ((long) lhs._y * rhs._z - (long) lhs._z * rhs._y) / LFloat.Precision,
                ((long) lhs._z * rhs._x - (long) lhs._x * rhs._z) / LFloat.Precision,
                ((long) lhs._x * rhs._y - (long) lhs._y * rhs._x) / LFloat.Precision
            );
        }
        
        
        public static LVector3 Lerp(LVector3 a, LVector3 b, LFloat f)
        {
            return CreateFromRaw(
                (((long) (b._x - a._x) * f._val) / LFloat.Precision) + a._x,
                (((long) (b._y - a._y) * f._val) / LFloat.Precision) + a._y,
                (((long) (b._z - a._z) * f._val) / LFloat.Precision) + a._z);
        }
        public static LVector3 Average(LVector3 a, LVector3 b)
        {
            return CreateFromRaw(
                (a._x + b._x) / 2,
                (a._y + b._y) / 2,
                (a._z + b._z) / 2);
        }
        public static LVector3 Average(LVector3 a, LVector3 b, LVector3 c)
        {
            return CreateFromRaw(
                (a._x + b._x + c._x) / 3,
                (a._y + b._y + c._y) / 3,
                (a._z + b._z + c._z) / 3);
        }
    }
}
