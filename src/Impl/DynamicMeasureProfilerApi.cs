using JetBrains.Profiler.Api;

namespace JetBrains.Profiler.SelfApi.Impl
{
  internal sealed class DynamicMeasureProfilerApi
  {
    private DynamicMeasureProfilerApi()
    {
    }

    public void Start() => MeasureProfiler.StartCollectingData();
    public void Stop() => MeasureProfiler.StopCollectingData();
    public void SaveData() => MeasureProfiler.SaveData();
    public void DropData() => MeasureProfiler.DropData();
    public void Detach() => MeasureProfiler.Detach();
    public bool IsReady() => (MeasureProfiler.GetFeatures() & MeasureFeatures.Ready) == MeasureFeatures.Ready;

    public static DynamicMeasureProfilerApi TryCreate()
    {
      return new DynamicMeasureProfilerApi();
    }
  }
}