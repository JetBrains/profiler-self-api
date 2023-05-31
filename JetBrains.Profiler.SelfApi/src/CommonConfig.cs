namespace JetBrains.Profiler.SelfApi
{
  /// <summary>
  /// Self-profiling configuration
  /// </summary>
  public abstract class CommonConfig
  {
    internal int? Pid;
    internal bool DoNotUseApi;
    internal string LogFile;
    internal string OtherArguments;
    internal int Timeout = 30000;
  }
}