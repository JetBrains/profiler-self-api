using System;

namespace JetBrains.Profiler.SelfApi.Impl
{
    internal sealed class DynamicMemoryProfilerApi
    {
        [Flags]
        private enum MemoryFeatures : uint
        {
            Ready = 0x1,
            Detach = 0x2,
            CollectAllocations = 0x4
        }

        private delegate void GetSnapshotDelegate(string name);
        private delegate void DetachDelegate();
        private delegate MemoryFeatures GetFeaturesDelegate();
        
        private readonly GetSnapshotDelegate _getSnapshot;
        private readonly DetachDelegate _detach;
        private readonly GetFeaturesDelegate _getFeatures;

        private DynamicMemoryProfilerApi(GetSnapshotDelegate snapshot, DetachDelegate detach, GetFeaturesDelegate getFeatures)
        {
            _getSnapshot = snapshot;
            _detach = detach;
            _getFeatures = getFeatures;
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
            var profiler = DynamicProfiler.TryCreate("JetBrains.Profiler.Api.MemoryProfiler");

            var getSnapshot = profiler?.TryGetMethod<GetSnapshotDelegate>("GetSnapshot", typeof(string));
            if (getSnapshot == null)
                return null;
            
            var detach = profiler.TryGetMethod<DetachDelegate>("Detach");
            if (detach == null)
                return null;
            
            var getFeatures = profiler.TryGetMethod<GetFeaturesDelegate>("GetFeatures");
            if (getFeatures == null)
                return null;
            
            return new DynamicMemoryProfilerApi(getSnapshot, detach, getFeatures);
        }

        public bool IsReady() => (_getFeatures() & MemoryFeatures.Ready) == MemoryFeatures.Ready;
    }
}