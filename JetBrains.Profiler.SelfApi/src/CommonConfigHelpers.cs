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

    /// <summary>
    /// Appends an arbitrary argument to the command line as is (without any quoting, and so on).
    /// </summary>
    public static T WithCommandLineArgument<T>(this T config, string argument)
      where T : CommonConfig
    {
      if (argument == null) throw new ArgumentNullException(nameof(argument));

      if (config.OtherArguments != null)
        config.OtherArguments += " " + argument;
      else
        config.OtherArguments = argument;

      return config;
    }

    /// <summary>
    /// Closes the session if the profiler doesn't respond in the specified timeout, for example, when attaching to or detaching from the process
    /// </summary>
    public static T UseCustomResponseTimeout<T>(this T config, int milliseconds)
      where T : CommonConfig
    {
      config.Timeout = milliseconds;
      return config;
    }
  }
}