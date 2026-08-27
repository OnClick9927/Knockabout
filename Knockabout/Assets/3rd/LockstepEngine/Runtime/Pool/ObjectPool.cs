using System;
using System.Collections.Generic;
namespace Lockstep
{
    /// <summary>
    /// 通用 FIFO 对象池基类。Queue 保存可复用对象，HashSet 校验对象是否已在池中。
    /// 子类通过生命周期钩子完成创建、借出重置、归还校验和最终清理。
    /// 该实现不是线程安全的。
    /// </summary>
    abstract class ObjectPool<T>
    {

        protected readonly Queue<T> pool = new Queue<T>();
        protected readonly HashSet<T> pooled = new HashSet<T>();

        public virtual Type type { get { return typeof(T); } }


        public int count { get { return pool.Count; } }




        /// <summary>从池中借出对象；池为空时调用 CreateNew 和 OnCreate。</summary>
        public virtual T Get()
        {
            T t;
            if (pool.Count > 0)
            {
                t = pool.Dequeue();
                pooled.Remove(t);
            }
            else
            {
                t = CreateNew();
                OnCreate(t);
            }
            OnGet(t);

            return t;
        }

        /// <summary>归还对象；null、重复归还或 OnSet 拒绝时返回 false。</summary>
        public virtual bool Set(T t)
        {
            if (ReferenceEquals(t, null))
            {
                Debug.LogError("Cannot return null to " + type + " pool.");
                return false;
            }

            if (!pooled.Add(t))
            {
                Debug.LogError("Set Err: Exist " + type);
                return false;
            }

            if (OnSet(t))
            {
                pool.Enqueue(t);
                return true;
            }

            pooled.Remove(t);
            return false;
        }


        /// <summary>清空当前缓存，并对实现 IDisposable 的对象执行最终释放。</summary>
        public void Clear()
        {
            while (pool.Count > 0)
            {
                var t = pool.Dequeue();
                pooled.Remove(t);
                OnClear(t);
                IDisposable dispose = t as IDisposable;
                if (dispose != null)
                    dispose.Dispose();
            }
        }

        /// <summary>创建一个池从未管理过的新对象。</summary>
        protected abstract T CreateNew();

        protected virtual void OnClear(T t) { }

        /// <summary>归还前清理对象；返回 false 可拒绝对象进入池。</summary>
        protected virtual bool OnSet(T t)
        {
            return true;
        }

        protected virtual void OnGet(T t) { }

        protected virtual void OnCreate(T t) { }
    }
}



