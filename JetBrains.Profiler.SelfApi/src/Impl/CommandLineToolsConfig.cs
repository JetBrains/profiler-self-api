namespace JetBrains.Profiler.SelfApi.Impl
{
  internal static class CommandLineToolsConfig
  {
    /// <summary>
    ///   The version (major and minor only!!!) of JetBrains.dotTrace.CommandLineTools and JetBrains.dotMemory.Console
    ///   NuGet-packages that must be downloaded.
    /// </summary>
    internal static readonly NuGet.SemanticVersion NupkgVersion = new(2023, 1);
  }
}