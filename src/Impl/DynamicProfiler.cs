using System;
using System.Reflection;

namespace JetBrains.Profiler.SelfApi.Impl
{
    internal class DynamicProfiler
    {
        private readonly Type _profiler;

        private DynamicProfiler(Type profiler)
        {
            _profiler = profiler;
        }

        public static DynamicProfiler TryCreate(string profilerType)
        {
            Assembly assembly;
            try
            {
                assembly = Assembly.Load("JetBrains.Profiler.Api, Version=1.1.6.0, Culture=neutral, PublicKeyToken=1010a0d8d6380325");
            }
            catch (Exception e)
            {
                Trace.Info("Unable to load `JetBrains.Profiler.Api` assembly: {0}", e.Message);
                return null;
            }

            var profiler = assembly.GetType(profilerType);
            if (profiler == null)
            {
                Trace.Error($"Something went wrong: the `{profilerType}` type not found.");
                return null;
            }
            return new DynamicProfiler(profiler);
        }

        public T TryGetMethod<T>(string name, params Type[] typeOfParams) 
            where T : Delegate
        {
            var method = _profiler.GetMethod(name, typeOfParams);
            if (method == null)
            {
                Trace.Error($"Something went wrong: the `{name}` method not found.");
                return null;
            }

            return (T) method.CreateDelegate(typeof(T));
        }
    }
}