using System;
using System.Reflection;

namespace JetBrains.Profiler.SelfApi.Impl
{
    internal sealed class DynamicMemoryProfilerApi
    {
        private delegate void GetSnapshotDelegate(string name);
        private delegate void DetachDelegate();
        private delegate uint GetFeaturesDelegate();
        
        private readonly GetSnapshotDelegate _getSnapshot;
        private readonly DetachDelegate _detach;
        private readonly GetFeaturesDelegate _getFeatures;

        private DynamicMemoryProfilerApi(GetSnapshotDelegate snapshot, DetachDelegate detach, GetFeaturesDelegate getFeatures)
        {
            _getSnapshot = snapshot;
            _detach = detach;
            _getFeatures = getFeatures;
        }

        public uint GetFeatures()
        {
            return _getFeatures();
        }

        public void GetSnapshot(string name)
        {
            _getSnapshot(name);
        }
        
        public void Detach()
        {
            _detach();
        }
        
        public static DynamicMemoryProfilerApi TryCreate()
        {
            Assembly assembly;
            try
            {
                assembly = Assembly.Load("JetBrains.Profiler.Api, Version=1.1.3.0, Culture=neutral, PublicKeyToken=1010a0d8d6380325");
            }
            catch (Exception e)
            {
                Trace.Info("Unable to load `JetBrains.Profiler.Api` assembly: {0}", e.Message);
                return null;
            }

            var profiler = assembly.GetType("JetBrains.Profiler.Api.MemoryProfiler");
            if (profiler == null)
            {
                Trace.Error("Something went wrong: the `MemoryProfiler` type not found.");
                return null;
            }
            
            var getSnapshot = TryGetMethod<GetSnapshotDelegate>(profiler, "GetSnapshot", typeof(string));
            if (getSnapshot == null)
                return null;
            
            var detach = TryGetMethod<DetachDelegate>(profiler, "Detach");
            if (detach == null)
                return null;
            
            var getFeatures = TryGetMethod<GetFeaturesDelegate>(profiler, "GetFeatures");
            if (getFeatures == null)
                return null;
            
            return new DynamicMemoryProfilerApi(getSnapshot, detach, getFeatures);
        }

        private static T TryGetMethod<T>(Type profiler, string name, params Type[] typeOfParams) 
            where T : Delegate
        {
            var method = profiler.GetMethod(name, typeOfParams);
            if (method == null)
            {
                Trace.Error($"Something went wrong: the `{name}` method not found.");
                return null;
            }

            return (T) method.CreateDelegate(typeof(T));
        }
    }
}