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
    }
}