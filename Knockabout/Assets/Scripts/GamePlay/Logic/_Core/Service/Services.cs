using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
namespace GamePlay
{
    public partial class Services
    {
        private static Dictionary<Type, IService> services = new Dictionary<Type, IService>();
        static Dictionary<Type, PropertyInfo> ps;
        static Services()
        {
            ps = typeof(Services).GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(x => x.PropertyType.GetInterface(nameof(IService)) != null).ToDictionary(x=>x.PropertyType);
        }

        private static Dictionary<Type, bool> interfaceRecord = new();
        private static Dictionary<Type, List<Type>> interfaceRecord1 = new();
        private static List<Type> GetInterfaces(Type type)
        {
            if (!interfaceRecord1.TryGetValue(type, out var record))
            {
                record = new List<Type>();
                var interfaces = type.GetInterfaces();
                for (int i = 0; i < interfaces.Length; i++)
                {
                    if (IsRecordInterface(interfaces[i]))
                    {
                        record.Add(interfaces[i]);
                    }
                }
                interfaceRecord1[type] = record;
            }
            return record;
        }
        private static bool IsRecordInterface(Type type)
        {
            if (!interfaceRecord.TryGetValue(type, out var record))
            {
                record = typeof(IService).IsAssignableFrom(type);
                interfaceRecord[type] = record;
            }
            return record;
        }
        public static void Add<T>(T service) where T : class, IService
        {
            var interfaces = GetInterfaces(service.GetType());

            for (int i = 0; i < interfaces.Count; i++)
            {
                var type = interfaces[i];
                if (!services.TryGetValue(type, out var list))
                {
                    services.Add(type, service);
                    if (ps.TryGetValue(type, out var p))
                    {
                        p.SetValue(null, service);
                    }
                }
            }

        }
        public static void Remove(IService service)
        {
            var interfaces = GetInterfaces(service.GetType());
            bool find = false;
            for (int i = 0; i < interfaces.Count; i++)
            {
                var type = interfaces[i];

                find |= services.Remove(type);
            }
            if (find && service is IDisposable dispose) dispose.Dispose();
        }


        public static T Find<T>() where T : IService
        {
            var type = typeof(T);
            if (!IsRecordInterface(type)) return default;

            services.TryGetValue(type, out var list);
            return (T)list;
        }

        public static void Clear()
        {
            services.Clear();
            foreach (var item in ps.Values)
            {
                item.SetValue(null, null);
            }
        }

    }

    partial class Services
    {

        public static IGameWorld world { get; private set; }
        public static IActorService actor { get; private set; }
        public static IViewService view { get; private set; }
        public static GameHelper helper { get; private set; }
        public static IGameLogic game_logic { get; private set; }
        public static ICollisionService collision { get; private set; }
        public static IRvoService rvo { get; private set; }


    }

}
