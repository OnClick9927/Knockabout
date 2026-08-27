using IFramework;

public abstract class SceneInstance<T> : MonoSingleton<T>, IInjectAble where T : SceneInstance<T>
{

}



