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
    /// By default self-api profiles the same process it was run in.
    /// With this option it is possible to profile another process by its pid
    /// </summary>
    public static T ProfileExternalProcess<T>(this T config, int pid)
      where T : CommonConfig
    {
      config.Pid = pid;
      return config.DoNotUseApi();
    }

    /// <summary>
    /// [Advanced use only] Prohibits using `JetBrains.Profiler.Api` to control the profiling session in case <see cref="ProfileExternalProcess{T}"/> was used.>
    /// </summary>
    /// <remarks>
    /// By default, `JetBrains.Profiler.Api` is used to control the session.
    /// Otherwise, the self-profiling API uses command-line profiler service messages to control the session.
    /// </remarks>
    private static T DoNotUseApi<T>(this T config)
      where T : CommonConfig
    {
      if (config.Pid == null)
        throw new InvalidOperationException("DoNotUseApi is available only when ProfileCustomProcess was specified.");

      config.DoNotUseApi = true;
      return config;
    }

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