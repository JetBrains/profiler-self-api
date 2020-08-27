using JetBrains.Profiler.Api;

namespace JetBrains.Profiler.SelfApi.Impl
{
  internal sealed class DynamicMemoryProfilerApi
  {
    private DynamicMemoryProfilerApi()
    {
    }

    public void GetSnapshot(string name) => MemoryProfiler.GetSnapshot(name);
    public void Detach() => MemoryProfiler.Detach();
    public bool IsReady() => (MemoryProfiler.GetFeatures() & MemoryFeatures.Ready) == MemoryFeatures.Ready;

    public static DynamicMemoryProfilerApi TryCreate()
    {
      return new DynamicMemoryProfilerApi();
    }
  }
}