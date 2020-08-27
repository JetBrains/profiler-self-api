using JetBrains.Profiler.Api;

namespace JetBrains.Profiler.SelfApi.Impl
{
  internal sealed class MemoryProfilerApi
  {
    public static readonly MemoryProfilerApi Instance = new MemoryProfilerApi();

    private MemoryProfilerApi()
    {
    }

    public void GetSnapshot(string name) => MemoryProfiler.GetSnapshot(name);
    public void Detach() => MemoryProfiler.Detach();
    public bool IsReady() => (MemoryProfiler.GetFeatures() & MemoryFeatures.Ready) == MemoryFeatures.Ready;

  }
}