namespace JetBrains.Profiler.SelfApi
{
  /// <summary>
  /// Self-profiling configuration
  /// </summary>
  public abstract class CommonConfig
  {
    internal string LogFile;
    internal string OtherArguments;
    internal int Timeout = 30000;
  }
}