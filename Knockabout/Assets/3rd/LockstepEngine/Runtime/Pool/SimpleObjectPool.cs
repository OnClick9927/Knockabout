namespace Lockstep
{
    /// <summary>
    /// 使用无参构造函数创建对象的默认对象池，并通过非泛型 IPool 接口支持按运行时类型归还。
    /// </summary>
    sealed class SimpleObjectPool<T> : ObjectPool<T>,IPool where T : class, new()
    {


        /// <summary>校验运行时类型后把对象归还到泛型池。</summary>
        public void SetObject(object context)
        {
            if (!(context is T value))
            {
                Debug.LogError($"{nameof(context)} is not {typeof(T)}; actual type: {context?.GetType().ToString() ?? "null"}.");
                return;
            }
            base.Set(value);
        }

        protected override T CreateNew()
        {
            return new T();
        }
    }






}



