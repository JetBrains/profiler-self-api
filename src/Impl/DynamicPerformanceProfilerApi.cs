using System;

namespace JetBrains.Profiler.SelfApi.Impl
{
    internal sealed class DynamicPerformanceProfilerApi
    {
        [Flags]
        private enum MeasureFeatures : uint
        {
            Ready = 0x1,
            Detach = 0x2
        }

        private delegate MeasureFeatures GetFeaturesDelegate();
        private delegate void StartCollectingDataDelegate();
        private delegate void StopCollectingDataDelegate();
        private delegate void SaveDataDelegate();
        private delegate void DropDataDelegate();
        private delegate void DetachDelegate();
        
        private readonly GetFeaturesDelegate _getFeatures;
        private readonly StartCollectingDataDelegate _start;
        private readonly StopCollectingDataDelegate _stop;
        private readonly SaveDataDelegate _saveData;
        private readonly DropDataDelegate _dropData;
        private readonly DetachDelegate _detach;

        private DynamicPerformanceProfilerApi(GetFeaturesDelegate getFeatures, StartCollectingDataDelegate start, StopCollectingDataDelegate stop, SaveDataDelegate saveData, DropDataDelegate dropData, DetachDelegate detach)
        {
            _getFeatures = getFeatures;
            _start = start;
            _stop = stop;
            _saveData = saveData;
            _dropData = dropData;
            _detach = detach;
        }

        public void Start() => _start();
        public void Stop() => _stop();
        public void SaveData() => _saveData();
        public void DropData() => _dropData();
        public void Detach() => _detach();
        public bool IsReady() => (_getFeatures() & MeasureFeatures.Ready) == MeasureFeatures.Ready;
        

        public static DynamicPerformanceProfilerApi TryCreate()
        {
            var profiler = DynamicProfiler.TryCreate("JetBrains.Profiler.Api.MeasureProfiler");

            var saveData = profiler?.TryGetMethod<SaveDataDelegate>("SaveData");
            if (saveData == null)
                return null;
            
            var dropData = profiler.TryGetMethod<DropDataDelegate>("DropData");
            if (dropData == null)
                return null;

            var detach = profiler.TryGetMethod<DetachDelegate>("Detach");
            if (detach == null)
                return null;
            
            var getFeatures = profiler.TryGetMethod<GetFeaturesDelegate>("GetFeatures");
            if (getFeatures == null)
                return null;

            var start = profiler.TryGetMethod<StartCollectingDataDelegate>("StartCollectingData");
            if (start == null)
                return null;

            var stop = profiler.TryGetMethod<StopCollectingDataDelegate>("StopCollectingData");
            if (stop == null)
                return null;

            return new DynamicPerformanceProfilerApi(getFeatures, start, stop, saveData, dropData, detach);
        }
    }
}