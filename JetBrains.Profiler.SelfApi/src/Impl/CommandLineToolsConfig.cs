namespace JetBrains.Profiler.SelfApi.Impl
{
  internal static class CommandLineToolsConfig
  {
    // The version (major and minor only!!!) of JetBrains.dotTrace.CommandLineTools and JetBrains.dotMemory.Console
    // NuGet-packages that must be downloaded.
    // Don't forget to update xmldoc. The xmldoc is used for injecting proper version into Init methods (inheritdoc).
    /// <summary>2023.1</summary>
    internal static readonly NuGet.SemanticVersion NupkgVersion = new(2024, 1);
  }
}