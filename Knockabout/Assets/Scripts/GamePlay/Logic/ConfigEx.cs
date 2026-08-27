using Lockstep;

namespace GamePlay
{
    public static class ConfigEx
    {
        public static LVector3 ToLVector3(this Luban.v3 v3)
        {
            return new LVector3(v3.X.ToLFloat(), v3.Y.ToLFloat(), v3.Z.ToLFloat());
        }
    }
}


