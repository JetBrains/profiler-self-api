using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.HabitatDetector;
using JetBrains.Profiler.Api;
using JetBrains.Profiler.SelfApi.Impl;

namespace JetBrains.Profiler.SelfApi
{
  /// <summary>
  /// The API lets you initiate and control profiling sessions right from the code of your application.
  /// For example, this can be helpful for profiling the application on end-user desktops or production servers.
  /// The API uses the dotMemory.exe command-line profiler (the tool is downloaded automatically)
  /// </summary>
  /// <remarks>
  /// Use case 1: ad-hoc profiling <br/>
  /// * install the JetBrains.Profiler.SelfApi package to your project<br/>
  /// * to initialize the API, call DotMemory.EnsurePrerequisite()<br/>
  /// * to get just one memory snapshot, call DotMemory.GetSnapshotOnce<br/>
  /// * or in case you need several snapshots, call Attach/GetSnapshot*/Detach<br/>
  /// * deploy your application<br/>
  /// * reproduce the issue<br/>
  /// * investigate the generated workspace with snapshots using JetBrains dotMemory<br/>
  ///<br/>
  /// Use case 2: self-profiling as a part of troubleshooting on a production server<br/>
  /// * install the JetBrains.Profiler.SelfApi package to your project<br/>
  /// * in handler of awesome `Gather trouble report` action call DotMemory.EnsurePrerequisite()<br/>
  /// * to get a memory snapshot, call DotMemory.GetSnapshotOnce<br/>
  /// * include the generated workspace with snapshots into the report<br/>
  /// </remarks>
  [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Local")]
  [SuppressMessage("ReSharper", "UnusedMember.Global")]
  [SuppressMessage("ReSharper", "UnusedType.Global")]
  public static class DotMemory
  {
    private const string MessageServicePrefix = "##dotMemory";
    private const string CltPresentableName = "dotMemory console profiler";

    /// <summary>
    /// Self-profiling configuration
    /// </summary>
    public sealed class Config : CommonConfig
    {
      internal string LogLevel;
      internal string WorkspaceFile;
      internal string WorkspaceDir;
      internal bool IsOpenDotMemory;
      internal bool IsOverwriteWorkspace;

      /// <summary>
      /// Specifies the path to the workspace file (snapshots storage).
      /// </summary>
      public Config SaveToFile(string filePath, bool overwrite = false)
      {
        if (WorkspaceDir != null)
          throw new InvalidOperationException("The SaveToFile and SaveToDir are mutually exclusive.");

        WorkspaceFile = filePath ?? throw new ArgumentNullException(nameof(filePath));
        IsOverwriteWorkspace = overwrite;
        return this;
      }

      /// <summary>
      /// Specifies the path to the workspace directory (filename will be auto-generated).
      /// </summary>
      public Config SaveToDir(string dirPath)
      {
        if (WorkspaceDir != null)
          throw new InvalidOperationException("The SaveToDir and SaveToFile are mutually exclusive.");

        WorkspaceDir = dirPath ?? throw new ArgumentNullException(nameof(dirPath));
        return this;
      }

      /// <summary>
      /// Specifies whether to open the generated workspace in JetBrains dotMemory.
      /// </summary>
      public Config OpenDotMemory()
      {
        IsOpenDotMemory = true;
        return this;
      }

      /// <summary>
      /// Sets the TRACE logging level.
      /// </summary>
      public Config UseLogLevelTrace()
      {
        LogLevel = "Trace";
        return this;
      }

      /// <summary>
      /// Sets the VERBOSE logging level.
      /// </summary>
      public Config UseLogLevelVerbose()
      {
        LogLevel = "Verbose";
        return this;
      }
    }

    private static readonly Prerequisite ConsoleRunnerPackage = new Prerequisite();
    private static readonly object Mutex = new object();

    private static Session _session;

    /// <summary>
    /// Makes sure that the dotMemory.exe command-line profiler is downloaded and is ready to use.
    /// </summary>
    /// <remarks>
    /// 1. Looks for command-line profiler in the <paramref name="downloadTo"/> folder (if specified). Uses it if it's found.<br/>
    /// 2. Looks for command-line profiler in the same folder with the running assembly. Uses it if it's found.<br/>
    /// 3. Downloads the latest `JetBrains.dotMemory.Console` NuGet package into the <paramref name="downloadTo"/>
    /// folder and uses the command-line profiler from this package. The basic package version is defined by <see cref="CommandLineToolsConfig.NupkgVersion"/>.
    /// The command-line profiler is saved to `{downloadTo}/dotMemory/{Version}/dotMemory.exe`
    /// If the file exists, a new one is not downloaded.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="progress">Download progress callback from 0.0 to 100.0. If null, progress is not reported.</param>
    /// <param name="nugetUrl">URL of NuGet mirror. If null, www.nuget.org is used.</param>
    /// <param name="nugetApi">NuGet API version.</param>
    /// <param name="downloadTo">NuGet download destination folder. If null, %LocalAppData% is used.</param>
    public static Task EnsurePrerequisiteAsync(
      CancellationToken cancellationToken,
      IProgress<double> progress = null,
      Uri nugetUrl = null,
      NuGetApi nugetApi = NuGetApi.V3,
      string downloadTo = null)
    {
      lock (Mutex)
        return ConsoleRunnerPackage.DownloadAsync(nugetUrl, nugetApi, downloadTo, progress, cancellationToken);
    }

    /// <summary>
    /// The shortcut for <c>EnsurePrerequisiteAsync(CancellationToken.None, progress, nugetUrl, prerequisitePath)</c>
    /// </summary>
    public static Task EnsurePrerequisiteAsync(
      IProgress<double> progress = null,
      Uri nugetUrl = null,
      NuGetApi nugetApi = NuGetApi.V3,
      string downloadTo = null)
    {
      return EnsurePrerequisiteAsync(CancellationToken.None, progress, nugetUrl, nugetApi, downloadTo);
    }

    /// <summary>
    /// The shortcut for <c>EnsurePrerequisiteAsync(CancellationToken.None, progress: null, nugetUrl, prerequisitePath).Wait()</c>
    /// </summary>
    public static void EnsurePrerequisite(
      Uri nugetUrl = null,
      NuGetApi nugetApi = NuGetApi.V3,
      string downloadTo = null)
    {
      EnsurePrerequisiteAsync(null, nugetUrl, nugetApi, downloadTo).Wait();
    }

    /// <summary>
    /// The shortcut for <see cref="Attach()"/>; <see cref="GetSnapshot"/>; <see cref="Detach"/>;
    /// </summary>
    /// <returns>Saved workspace file path.</returns>
    public static string GetSnapshotOnce()
    {
      return GetSnapshotOnce(new Config());
    }

    /// <summary>
    /// The shortcut for <see cref="Attach(Config)"/>; <see cref="GetSnapshot"/>; <see cref="Detach"/>;
    /// </summary>
    /// <returns>Saved workspace file path.</returns>
    public static string GetSnapshotOnce(Config config)
    {
      if (config == null) throw new ArgumentNullException(nameof(config));

      lock (Mutex)
      {
        ConsoleRunnerPackage.VerifyReady();

        if (_session != null)
          throw new InvalidOperationException("The profiling session is active already: Attach() was called early.");

        return RunConsole("get-snapshot", config).AwaitFinished().WorkspaceFile;
      }
    }

    /// <summary>
    /// Attaches dotMemory to the current process using the default API configuration.
    /// </summary>
    public static void Attach()
    {
      Attach(new Config());
    }

    /// <summary>
    /// Attaches dotMemory to the current process using the specified API configuration.
    /// </summary>
    public static void Attach(Config config)
    {
      if (config == null)
        throw new ArgumentNullException(nameof(config));

      Helper.CheckAttachCompatibility();

      lock (Mutex)
      {
        ConsoleRunnerPackage.VerifyReady();

        if (_session != null)
          throw new InvalidOperationException("The profiling session is active still: forgot to call Detach()?");

        _session = RunConsole("attach", config).AwaitConnected();
      }
    }

    /// <summary>
    /// Detaches dotMemory from the current process.
    /// </summary>
    /// <returns>Saved workspace file path.</returns>
    public static string Detach()
    {
      lock (Mutex)
      {
        if (_session == null)
          throw new InvalidOperationException("The profiling session isn't active: forgot to call Attach()?");

        try
        {
          return _session.Detach().AwaitFinished().WorkspaceFile;
        }
        finally
        {
          _session = null;
        }
      }
    }

    /// <summary>
    ///   Collect memory snapshot of the current process and save it to the disk. This method forces full GC.
    /// </summary>
    /// <param name="name">The name of the memory snapshot. This is not a file name. Currently not used.</param>
    public static void GetSnapshot(string name = null)
    {
      lock (Mutex)
      {
        if (_session == null)
          throw new InvalidOperationException("The profiling session isn't active: forgot to call Attach()?");

        _session.GetSnapshot(name);
      }
    }

    private static string GetSaveToFilePath(Config config)
    {
      if (config.WorkspaceFile != null)
        return config.WorkspaceFile;

      var workspaceName = $"{Process.GetCurrentProcess().ProcessName}.{DateTime.Now:yyyy-MM-ddTHH-mm-ss.fff}.dmw";
      if (config.WorkspaceDir != null)
        return Path.Combine(config.WorkspaceDir, workspaceName);

      return Path.Combine(Path.GetTempPath(), workspaceName);
    }

    private static Session RunConsole(string command, Config config)
    {
      Trace.Verbose("DotMemory.RunConsole: Looking for runner...");

      var runnerPath = ConsoleRunnerPackage.GetRunnerPath();

      var workspaceFile = GetSaveToFilePath(config);

      var commandLine = new StringBuilder();

      if (config.LogLevel != null)
        commandLine.Append($"--log-level={config.LogLevel} ");

      if (config.LogFile != null)
        commandLine.Append($"\"--log-file={config.LogFile}\" ");

      commandLine.Append($"{command} {Process.GetCurrentProcess().Id} \"-f={workspaceFile}\"");

      if (config.IsOverwriteWorkspace)
        commandLine.Append(" --overwrite");

      if (config.IsOpenDotMemory)
        commandLine.Append(" --open-dotmemory");

      if (command != "get-snapshot")
        commandLine.Append(" --use-api");

      if (config.OtherArguments != null)
        commandLine.Append(' ').Append(config.OtherArguments);

      Trace.Info("DotMemory.RunConsole:\n  runner = `{0}`\n  arguments = `{1}`", runnerPath, commandLine);

      var consoleProfiler = new ConsoleProfiler(
        runnerPath,
        commandLine.ToString(),
        MessageServicePrefix,
        CltPresentableName,
        () => (MemoryProfiler.GetFeatures() & MemoryFeatures.Ready) == MemoryFeatures.Ready
        );

      Trace.Verbose("DotMemory.RunConsole: Runner started.");

      return new Session(consoleProfiler, workspaceFile, config.Timeout);
    }

    private sealed class Prerequisite : PrerequisiteBase
    {
      public Prerequisite() : base("dotMemory", CommandLineToolsConfig.NupkgVersion)
      {
      }

      protected override string GetRunnerName()
      {
        switch (HabitatInfo.Platform)
        {
        case JetPlatform.Linux:
        case JetPlatform.MacOsX: return "dotmemory";
        case JetPlatform.Windows: return "dotMemory.exe";
        default: throw new ArgumentOutOfRangeException();
        }
      }

      protected override string GetPackageName()
      {
        return "JetBrains.dotMemory.Console";
      }

      protected override long GetEstimatedSize()
      {
        return 20 * 1024 * 1024;
      }
    }

    private sealed class Session
    {
      private readonly ConsoleProfiler _consoleProfiler;
      private readonly int _timeout;

      public Session(ConsoleProfiler consoleProfiler, string workspaceFile, int timeout)
      {
        _consoleProfiler = consoleProfiler;
        _timeout = timeout;

        WorkspaceFile = workspaceFile;
      }

      public string WorkspaceFile { get; }

      [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
      public Session Detach()
      {
        MemoryProfiler.Detach();
        return this;
      }

      [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
      public Session GetSnapshot(string name)
      {
        MemoryProfiler.GetSnapshot(name);
        return this;
      }

      public Session AwaitConnected()
      {
        _consoleProfiler.AwaitConnected(_timeout);
        return this;
      }

      public Session AwaitFinished()
      {
        _consoleProfiler.AwaitFinished(_timeout);
        return this;
      }
    }
  }
}