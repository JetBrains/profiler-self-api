using System;
using System.Diagnostics.CodeAnalysis;

namespace JetBrains.Profiler.SelfApi
{
  /// <summary>
  /// Helpers for self-profiling configuration
  /// </summary>
  [SuppressMessage("ReSharper", "UnusedMember.Global")]
  [SuppressMessage("ReSharper", "UnusedType.Global")]
  public static class CommonConfigHelpers
  {
    /// <summary>
    /// Specifies path to log file.
    /// </summary>
    public static T UseLogFile<T>(this T config, string filePath)
      where T : CommonConfig
    {
      config.LogFile = filePath ?? throw new ArgumentNullException(nameof(filePath));
      return config;
    }
  }
}