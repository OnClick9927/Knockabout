// Copyright 2019 谭杰鹏. All Rights Reserved //https://github.com/JiepengTan 

using System;
using System.Runtime.CompilerServices;

namespace Lockstep
{
    /// <summary>
    /// 基于 long 的确定性定点数。
    /// 内部值始终放大 <see cref="Precision"/> 倍保存，例如 1.5 对应 1500000。
    /// 锁步逻辑应优先使用本类型参与运算，避免不同平台的浮点舍入差异。
    /// 乘除法会在中间结果中使用 long，业务侧仍需控制数值范围以防溢出。
    /// </summary>
    [Serializable]
    public struct LFloat : IEquatable<LFloat>, IComparable<LFloat>
    {
        /// <summary>定点缩放倍率，同时决定小数精度为 10^-6。</summary>
        public const long Precision = 1000000;
        /// <summary>旧版千分精度数据转换到当前精度时使用的倍率。</summary>
        public const long RateOfOldPrecision = Precision / 1000;
        public const long HalfPrecision = Precision / 2;
        public const float PrecisionFactor = 0.000001f;

        /// <summary>
        /// 放大后的原始整数值。仅序列化、哈希或底层高性能代码应直接访问；
        /// 普通逻辑应通过 LFloat 运算符和转换方法操作。
        /// </summary>
        public long _val;

        public static readonly LFloat two = FromRaw(Precision * 2L);
        public static readonly LFloat four = FromRaw(Precision * 4L);
        //public static readonly LFloat half = new LFloat(true, Precision / 2);
        public static readonly LFloat zero = FromRaw(0L);
        public static readonly LFloat one = FromRaw(LFloat.Precision);
        public static readonly LFloat negOne = FromRaw(-LFloat.Precision);
        public static readonly LFloat half = FromRaw(LFloat.Precision / 2L);
        public static readonly LFloat FLT_MAX = FromRaw(long.MaxValue);
        public static readonly LFloat FLT_MIN = FromRaw(long.MinValue);
        public static readonly LFloat EPSILON = FromRaw(1L);
        public static readonly LFloat INTERVAL_EPSI_LON = FromRaw(1L);

        public static readonly LFloat MaxValue = FromRaw(long.MaxValue);
        public static readonly LFloat MinValue = FromRaw(long.MinValue);

        /// ! 传入的是正常数放大1000 的数值</summary>
        // public LFloat(string isUseRawVal1000, long rawVal1000)
        // {
        //     this._val = rawVal1000 * RateOfOldPrecision;
        // }

        /// <summary>用已经放大过的原始值构造定点数，不会再次乘以 Precision。</summary>
        public static LFloat FromRaw(long rawVal)
        {
            return new LFloat()
            {
                _val = rawVal
            };
        }
        // public LFloat(bool isUseRawVal, long rawVal)
        // {
        //     this._val = rawVal;
        // }

        // public LFloat(int val)
        // {
        //     this._val = val * LFloat.Precision;
        // }
        /// <summary>用普通整数构造定点数，内部会乘以 Precision。</summary>
        public LFloat(long val)
        {
            this._val = val * LFloat.Precision;
        }

        // #if UNITY_EDITOR
        //         /// <summary>
        //         /// 直接使用浮点型 进行构造 警告!!! 仅应该在Editor模式下使用，不应该在正式代码中使用,避免出现引入浮点的不确定性
        //         /// </summary>
        //         public LFloat(bool shouldOnlyUseInEditor, float val)
        //         {
        //             this._val = (long)(val * LFloat.Precision);
        //         }
        // #endif

        #region override operator 

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(LFloat a, LFloat b)
        {
            return a._val < b._val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(LFloat a, LFloat b)
        {
            return a._val > b._val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(LFloat a, LFloat b)
        {
            return a._val <= b._val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(LFloat a, LFloat b)
        {
            return a._val >= b._val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(LFloat a, LFloat b)
        {
            return a._val == b._val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(LFloat a, LFloat b)
        {
            return a._val != b._val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LFloat operator +(LFloat a, LFloat b)
        {
            return FromRaw(a._val + b._val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LFloat operator -(LFloat a, LFloat b)
        {
            return FromRaw(a._val - b._val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LFloat operator *(LFloat a, LFloat b)
        {
            long val = (long)(a._val) * b._val;
            return FromRaw(val / LFloat.Precision);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LFloat operator /(LFloat a, LFloat b)
        {
            long val = (long)(a._val * LFloat.Precision) / b._val;
            return FromRaw(val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LFloat operator -(LFloat a)
        {
            return FromRaw(-a._val);
        }

        #region adapt for int

        public static LFloat operator +(LFloat a, int b)
        {
            return FromRaw(a._val + b * Precision);
        }

        public static LFloat operator -(LFloat a, int b)
        {
            return FromRaw(a._val - b * Precision);
        }

        public static LFloat operator *(LFloat a, int b)
        {
            return FromRaw((a._val * b));
        }

        public static LFloat operator /(LFloat a, int b)
        {
            return FromRaw((a._val) / b);
        }


        public static LFloat operator +(int a, LFloat b)
        {
            return FromRaw(b._val + a * Precision);
        }

        public static LFloat operator -(int a, LFloat b)
        {
            return FromRaw(a * Precision - b._val);
        }

        public static LFloat operator *(int a, LFloat b)
        {
            return FromRaw((b._val * a));
        }

        public static LFloat operator /(int a, LFloat b)
        {
            return FromRaw(((long)(a * Precision * Precision) / b._val));
        }


        public static bool operator <(LFloat a, int b)
        {
            return a._val < (b * Precision);
        }

        public static bool operator >(LFloat a, int b)
        {
            return a._val > (b * Precision);
        }

        public static bool operator <=(LFloat a, int b)
        {
            return a._val <= (b * Precision);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(LFloat a, int b)
        {
            return a._val >= (b * Precision);
        }

        public static bool operator ==(LFloat a, int b)
        {
            return a._val == (b * Precision);
        }

        public static bool operator !=(LFloat a, int b)
        {
            return a._val != (b * Precision);
        }


        public static bool operator <(int a, LFloat b)
        {
            return (a * Precision) < (b._val);
        }

        public static bool operator >(int a, LFloat b)
        {
            return (a * Precision) > (b._val);
        }

        public static bool operator <=(int a, LFloat b)
        {
            return (a * Precision) <= (b._val);
        }

        public static bool operator >=(int a, LFloat b)
        {
            return (a * Precision) >= (b._val);
        }

        public static bool operator ==(int a, LFloat b)
        {
            return (a * Precision) == (b._val);
        }

        public static bool operator !=(int a, LFloat b)
        {
            return (a * Precision) != (b._val);
        }

        #endregion
        #region adapt for long

        public static LFloat operator +(LFloat a, long b)
        {
            return FromRaw(a._val + b * Precision);
        }

        public static LFloat operator -(LFloat a, long b)
        {
            return FromRaw(a._val - b * Precision);
        }

        public static LFloat operator *(LFloat a, long b)
        {
            return FromRaw((a._val * b));
        }

        public static LFloat operator /(LFloat a, long b)
        {
            return FromRaw((a._val) / b);
        }


        public static LFloat operator +(long a, LFloat b)
        {
            return FromRaw(b._val + a * Precision);
        }

        public static LFloat operator -(long a, LFloat b)
        {
            return FromRaw(a * Precision - b._val);
        }

        public static LFloat operator *(long a, LFloat b)
        {
            return FromRaw((b._val * a));
        }

        public static LFloat operator /(long a, LFloat b)
        {
            return FromRaw(((long)(a * Precision * Precision) / b._val));
        }


        public static bool operator <(LFloat a, long b)
        {
            return a._val < (b * Precision);
        }

        public static bool operator >(LFloat a, long b)
        {
            return a._val > (b * Precision);
        }

        public static bool operator <=(LFloat a, long b)
        {
            return a._val <= (b * Precision);
        }

        public static bool operator >=(LFloat a, long b)
        {
            return a._val >= (b * Precision);
        }

        public static bool operator ==(LFloat a, long b)
        {
            return a._val == (b * Precision);
        }

        public static bool operator !=(LFloat a, long b)
        {
            return a._val != (b * Precision);
        }


        public static bool operator <(long a, LFloat b)
        {
            return (a * Precision) < (b._val);
        }

        public static bool operator >(long a, LFloat b)
        {
            return (a * Precision) > (b._val);
        }

        public static bool operator <=(long a, LFloat b)
        {
            return (a * Precision) <= (b._val);
        }

        public static bool operator >=(long a, LFloat b)
        {
            return (a * Precision) >= (b._val);
        }

        public static bool operator ==(long a, LFloat b)
        {
            return (a * Precision) == (b._val);
        }

        public static bool operator !=(long a, LFloat b)
        {
            return (a * Precision) != (b._val);
        }

        #endregion

        #endregion

        #region override object func 

        public override bool Equals(object obj)
        {
            return obj is LFloat && ((LFloat)obj)._val == _val;
        }

        public bool Equals(LFloat other)
        {
            return _val == other._val;
        }

        public int CompareTo(LFloat other)
        {
            return _val.CompareTo(other._val);
        }

        public override int GetHashCode()
        {
            return _val.GetHashCode();
        }

        public override string ToString()
        {
            return (_val * LFloat.PrecisionFactor).ToString();
        }

        #endregion

        #region override type convert 
        public static implicit operator LFloat(short value)
        {
            return FromRaw(value * Precision);
        }

        public static explicit operator short(LFloat value)
        {
            return (short)(value._val / Precision);
        }

        public static implicit operator LFloat(int value)
        {
            return FromRaw(value * Precision);
        }

        public static implicit operator int(LFloat value)
        {
            return (int)(value._val / Precision);
        }

        public static explicit operator LFloat(long value)
        {
            return FromRaw(value * Precision);
        }

        public static implicit operator long(LFloat value)
        {
            return value._val / Precision;
        }


        public static explicit operator LFloat(float value)
        {
            return FromRaw((long)(value * Precision));
        }

        public static explicit operator float(LFloat value)
        {
            return (float)value._val * LFloat.PrecisionFactor;
        }

        public static explicit operator LFloat(double value)
        {
            return FromRaw((long)(value * Precision));
        }

        public static explicit operator double(LFloat value)
        {
            return (double)value._val * LFloat.PrecisionFactor;
        }

        #endregion


        public int ToInt()
        {
            return (int)(_val / LFloat.Precision);
        }

        public long ToLong()
        {
            return _val / LFloat.Precision;
        }

        public float ToFloat()
        {
            return _val * LFloat.PrecisionFactor;
        }

        public double ToDouble()
        {
            return _val * LFloat.PrecisionFactor;
        }

        public int Floor()
        {
            var x = this._val;
            if (x > 0)
            {
                x /= LFloat.Precision;
            }
            else
            {
                if (x % LFloat.Precision == 0)
                {
                    x /= LFloat.Precision;
                }
                else
                {
                    x = x / LFloat.Precision - 1;
                }
            }

            return (int)x;
        }

        public int Ceil()
        {
            var x = this._val;
            if (x < 0)
            {
                x /= LFloat.Precision;
            }
            else
            {
                if (x % LFloat.Precision == 0)
                {
                    x /= LFloat.Precision;
                }
                else
                {
                    x = x / LFloat.Precision + 1;
                }
            }

            return (int)x;
        }
    }
}
