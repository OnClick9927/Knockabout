using System;
using System.Collections.Generic;
namespace Lockstep
{
    /// <summary>供不同泛型对象池统一按 object 归还的内部接口。</summary>
    interface IPool
    {

        void SetObject(object obj);
    }

    /// <summary>
    /// 按泛型实参维护全局静态池的门面。
    /// 每个引用类型和数组元素类型都拥有独立池；SetByRealType 用注册表找到真实类型池。
    /// Disposable 包装器适合 using 作用域，可保证正常退出和异常路径都归还一次资源。
    /// </summary>
    public class StaticPool
    {
        private static readonly Dictionary<Type, IPool> map = new Dictionary<Type, IPool>();
        class Pool<T> where T : class, new()
        {
            internal static readonly SimpleObjectPool<T> s_Pool = new SimpleObjectPool<T>();
            static Pool()
            {
                map[typeof(T)] = s_Pool;
            }
            public static T Get() => s_Pool.Get();

            public static void Set(T toRelease) => s_Pool.SetObject(toRelease);

        }
        class ArrPool<T>
        {
            internal static readonly ArrayPool<T> s_array_Pool = new ArrayPool<T>();
            public static T[] Get(int length) => s_array_Pool.Get(length);
            public static void Set(T[] toRelease) => s_array_Pool.Set(toRelease);

        }

        /// <summary>暴露池中值并允许通过 Dispose 自动归还的轻量包装契约。</summary>
        public interface IDisposableValue<T> : IDisposable
        {
            T value { get; }

        }

        /// <summary>引用对象的作用域归还包装器；Dispose 可重复调用而不会重复入池。</summary>
        public struct StaticPoolValue<T> : IDisposableValue<T> where T : class, new()
        {
            private T pooledValue;
            public T value => pooledValue;

            internal StaticPoolValue(bool ignore = true) => pooledValue = StaticPool.Get<T>();

            public void Dispose()
            {
                T toRelease = pooledValue;
                if (ReferenceEquals(toRelease, null)) return;

                pooledValue = null;
                StaticPool.Set(toRelease);
            }
        }
        /// <summary>数组的作用域归还包装器；归还时数组池会清空全部元素。</summary>
        public struct StaticPoolArray<T> : IDisposableValue<T[]>
        {
            private T[] pooledValue;
            public T[] value => pooledValue;
            internal StaticPoolArray(int length) => pooledValue = StaticPool.GetArray<T>(length);

            public void Dispose()
            {
                T[] toRelease = pooledValue;
                if (toRelease == null) return;

                pooledValue = null;
                StaticPool.Set(toRelease);
            }
        }
        public static T[] GetArray<T>(int length) => ArrPool<T>.Get(length);

        public static void Set<T>(T[] toRelease) => ArrPool<T>.Set(toRelease);

        public static T Get<T>() where T : class, new() => Pool<T>.Get();

        public static void Set<T>(T toRelease) where T : class, new() => Pool<T>.Set(toRelease);

        /// <summary>
        /// 根据对象的实际运行时类型归还，而不是按变量声明类型归还。
        /// 对象对应的泛型池必须至少初始化过一次，才能出现在类型注册表中。
        /// </summary>
        public static void SetByRealType<T>(T toRelease)
        {
            if (ReferenceEquals(toRelease, null))
            {
                Debug.LogError("Cannot return null to StaticPool.");
                return;
            }

            var type = toRelease.GetType();
            IPool result;
            map.TryGetValue(type, out result);

            if (result != null)
            {
                result.SetObject(toRelease);
            }
            else
            {
                Debug.LogError($"No pool is registered for {type}.");
            }
        }

        public static StaticPoolValue<T> CreateDisposable<T>() where T : class, new() => new StaticPoolValue<T>(true);
        public static StaticPoolArray<T> CreateDisposableArray<T>(int length) => new StaticPoolArray<T>(length);



    }






}



