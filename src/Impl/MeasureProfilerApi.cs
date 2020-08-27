using JetBrains.Profiler.Api;

namespace JetBrains.Profiler.SelfApi.Impl
{
  internal sealed class MeasureProfilerApi
  {
    public static readonly MeasureProfilerApi Instance = new MeasureProfilerApi();

    private MeasureProfilerApi()
    {
    }

    public void Start() => MeasureProfiler.StartCollectingData();
    public void Stop() => MeasureProfiler.StopCollectingData();
    public void SaveData() => MeasureProfiler.SaveData();
    public void DropData() => MeasureProfiler.DropData();
    public void Detach() => MeasureProfiler.Detach();
    public bool IsReady() => (MeasureProfiler.GetFeatures() & MeasureFeatures.Ready) == MeasureFeatures.Ready;
  }
}