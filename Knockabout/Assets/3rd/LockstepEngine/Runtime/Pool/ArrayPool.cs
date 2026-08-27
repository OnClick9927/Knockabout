using System;
using System.Collections.Generic;
namespace Lockstep
{
    /// <summary>
    /// 按数组长度分桶的简单数组池。
    /// 归还时会清空全部元素，避免引用泄漏和旧数据污染；HashSet 用于阻止重复归还。
    /// 本实现没有线程同步，只能在锁步主线程或由调用方保证互斥的环境中使用。
    /// </summary>
    class ArrayPool<T>
    {
        private readonly Dictionary<int, Stack<T[]>> pools = new Dictionary<int, Stack<T[]>>();
        private readonly HashSet<T[]> pooled = new HashSet<T[]>();

        /// <summary>取得指定长度数组；没有缓存时创建新实例。</summary>
        public T[] Get(int length)
        {
            if (length < 0)
            {
                Debug.LogError($"Cannot get a negative-length array from {typeof(T[])} pool: {length}.");
                return Array.Empty<T>();
            }

            if (pools.TryGetValue(length, out Stack<T[]> pool) && pool.Count > 0)
            {
                T[] value = pool.Pop();
                pooled.Remove(value);
                return value;
            }

            return new T[length];
        }

        /// <summary>清空并归还数组；null 或重复归还返回 false。</summary>
        public bool Set(T[] value)
        {
            if (value == null)
            {
                Debug.LogError($"Cannot return null to {typeof(T[])} pool.");
                return false;
            }

            if (!pooled.Add(value))
            {
                Debug.LogError("Set Err: Exist " + typeof(T[]));
                return false;
            }

            Array.Clear(value, 0, value.Length);

            if (!pools.TryGetValue(value.Length, out Stack<T[]> pool))
            {
                pool = new Stack<T[]>();
                pools.Add(value.Length, pool);
            }

            pool.Push(value);
            return true;
        }
    }






}



