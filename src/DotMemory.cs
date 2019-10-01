using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Profiler.SelfApi.Impl;

namespace JetBrains.Profiler.SelfApi
{
  /// <summary>
  /// The dotMemory self profiling API based on single-exe console runner which is automatically downloaded as NuGet-package. 
  /// </summary>
  /// <remarks>
  /// Use case: ad-hoc profiling<br/>
  /// * install NuGet (just NuGet, no any other actions required)<br/>
  /// * on initialization phase call DotMemory.EnsurePrerequisite()<br/>
  /// * in profiled peace of code call DotMemory.GetSnapshotOnce (or Attach/GetSnapshot*/Detach)<br/>
  /// * deploy to staging<br/>
  /// * reproduce issue<br/>
  /// * take over generated workspace for investigation<br/>
  ///<br/>
  /// Use case: self profiling as part of troubleshooting on production  
  /// * install NuGet (just NuGet, no any other actions required)
  /// * in handler of awesome `Gather trouble report` action call DotMemory.EnsurePrerequisite()
  /// * then call DotMemory.GetSnapshotOnce
  /// * include generated workspace into report 
  /// </remarks>
  [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Local")]
  [SuppressMessage("ReSharper", "UnusedMember.Global")]
  public static class DotMemory
  {
    /// <summary>
    /// The version of JetBrains.dotMemory.Console NuGet-package which will be downloaded. 
    /// </summary>
    public const string NupkgVersion = "192.0.20190807.154300";
    
    /// <summary>
    /// Self profiling configuration.
    /// </summary>
    public sealed class Config
    {
      internal string WorkspaceFile;
      internal string WorkspaceDir;
      internal bool IsOpenDotMemory;
      internal bool IsOverwriteWorkspace;
      internal bool? IsUseApi;

      /// <summary>
      /// Specifies file to save workspace to. 
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
      /// Specifies directory to save workspace to (file name will be auto generated). 
      /// </summary>
      public Config SaveToDir(string dirPath)
      {
        if (WorkspaceDir != null)
          throw new InvalidOperationException("The SaveToDir and SaveToFile are mutually exclusive.");
        
        WorkspaceDir = dirPath ?? throw new ArgumentNullException(nameof(dirPath));
        return this;
      }
      
      /// <summary>
      /// Specifies to open produced workspace in dotMemory UI.
      /// </summary>
      public Config OpenDotMemory()
      {
        IsOpenDotMemory = true;
        return this;
      }
      
      /// <summary>
      /// Specifies to use `JetBrains.Profiler.Api` to control snapshots.
      /// </summary>
      /// <remarks>
      /// By default, the `JetBrains.Profiler.Api` is used automatically if appropriate assembly successfully loaded.
      /// Otherwise console runner's service messages are used.
      /// </remarks>
      public Config UseApi()
      {
        if (IsUseApi.HasValue)
          throw new InvalidOperationException("The UseApi and DoNotUseApi are mutually exclusive.");
        
        IsUseApi = true;
        return this;
      }
      
      /// <summary>
      /// Specifies to DO NOT use `JetBrains.Profiler.Api` to control snapshots.
      /// </summary>
      /// <remarks>
      /// By default, the `JetBrains.Profiler.Api` is used automatically if appropriate assembly successfully loaded.
      /// Otherwise console runner's service messages are used.
      /// </remarks>
      public Config DoNotUseApi()
      {
        if (IsUseApi.HasValue)
          throw new InvalidOperationException("The DoNotUseApi and UseApi are mutually exclusive.");
        
        IsUseApi = false;
        return this;
      }
    }
    
    private const int Timeout = 30000;
    private static readonly Prerequisite OurPrerequisite = new Prerequisite();
    private static readonly object OurMutex = new object();
    private static Task _prerequisiteTask;
    private static string _prerequisitePath;
    private static Session _session;

    /// <summary>
    /// Ensures prerequisite (dotMemory console runner) for current OS and process bitness is downloaded and ready to use.
    /// </summary>
    /// <remarks>
    /// 1. Looking for `dotMemory.exe` in the same folder as executing assembly. Uses it if found one.<br/>
    /// 2. Otherwise downloads `JetBrains.dotMemory.Console` NuGet-package into <paramref name="downloadTo"/>
    /// folder and uses runner from this package. The package version to be downloaded is defined by <see cref="NupkgVersion"/> constant.
    /// Actually, runner will be located in `{downloadTo}/dotMemory.{NupkgVersion}/dotMemory.exe`
    /// If one already exists then no download performed.
    /// </remarks>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <param name="progress">The progress callback. The progress is reported in range [0.0; 100.0]. If null progress will not be reported.</param>
    /// <param name="nugetUrl">The URL of NuGet mirror. If null the default www.nuget.org will be used.</param>
    /// <param name="nugetApi">The NuGet API version.</param>
    /// <param name="downloadTo">The path to download prerequisite to. If null %LocalAppData% is used.</param>
    public static Task EnsurePrerequisiteAsync(
      CancellationToken cancellationToken, 
      IProgress<double> progress = null, 
      Uri nugetUrl = null, 
      NuGetApi nugetApi = NuGetApi.V3,
      string downloadTo = null)
    {
      lock (OurMutex)
      {
        if (_prerequisiteTask != null && !_prerequisiteTask.IsCompleted)
        {
          Trace.Verbose("DotMemory.EnsurePrerequisite: Task already running.");
          return _prerequisiteTask;
        }

        _prerequisiteTask = null;
        _prerequisitePath = downloadTo;

        if (OurPrerequisite.TryGetRunner(downloadTo, out _))
        {
          Trace.Verbose("DotMemory.EnsurePrerequisite: Runner found, no async task needed.");
          return _prerequisiteTask = Task.FromResult(Missing.Value);
        }
        
        if (nugetUrl == null)
          nugetUrl = NuGet.GetDefaultUrl(nugetApi);
        
        Trace.Verbose("DotMemory.EnsurePrerequisite: Runner not found, starting download...");
        return _prerequisiteTask = OurPrerequisite
          .DownloadAsync(nugetUrl, nugetApi, downloadTo, progress, cancellationToken);
      }
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

      lock (OurMutex)
      {
        if (_prerequisiteTask == null || !_prerequisiteTask.Wait(40))
          throw new InvalidOperationException("The prerequisite isn't ready: forgot to call EnsurePrerequisiteAsync()?");
        
        if (_session != null)
          throw new InvalidOperationException("The profiling session is active already: Attach() was called early.");

        // `get-snapshot` command doesn't support API mode
        config.IsUseApi = false;

        return RunConsole("get-snapshot --raw", config).AwaitFinished(-1).WorkspaceFile;
      }
    }

    /// <summary>
    /// Attaches dotMemory to current process with default configuration.
    /// </summary>
    public static void Attach()
    {
      Attach(new Config());
    }

    /// <summary>
    /// Attaches dotMemory to current process with specified configuration.
    /// </summary>
    public static void Attach(Config config)
    {
      if (config == null) throw new ArgumentNullException(nameof(config));

      lock (OurMutex)
      {
        if (_prerequisiteTask == null || !_prerequisiteTask.Wait(40))
          throw new InvalidOperationException("The prerequisite isn't ready: forgot to call EnsurePrerequisiteAsync()?");
        
        if (_session != null)
          throw new InvalidOperationException("The profiling session is active still: forgot to call Detach()?");

        _session = RunConsole("attach -s", config).AwaitConnected(Timeout);
      }
    }

    /// <summary>
    /// Detaches dotMemory from current process.
    /// </summary>
    /// <returns>Saved workspace file path.</returns>
    public static string Detach()
    {
      lock (OurMutex)
      {
        if (_session == null)
          throw new InvalidOperationException("The profiling session isn't active: forgot to call Attach()?");

        try
        {
          return _session.Detach().AwaitFinished(Timeout).WorkspaceFile;
        }
        finally
        {
          _session = null;
        }
      }
    }
    
    /// <summary>
    /// Gets memory snapshot of current process.
    /// </summary>
    /// <param name="name">Optional snapshot name.</param>
    public static void GetSnapshot(string name = null)
    {
      lock (OurMutex)
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
      if (!OurPrerequisite.TryGetRunner(_prerequisitePath, out var runnerPath))
        throw new InvalidOperationException("Something went wrong: the dotMemory console profiler not found.");
      
      var workspaceFile = GetSaveToFilePath(config);
      
      var commandLine = new StringBuilder();
      commandLine.Append($"{command} {Process.GetCurrentProcess().Id} \"-f={workspaceFile}\"");

      if (config.IsOverwriteWorkspace)
        commandLine.Append(" --overwrite");
      
      if (config.IsOpenDotMemory)
        commandLine.Append(" --open-dotmemory");

      DynamicMemoryProfilerApi api = null;
      if (config.IsUseApi.HasValue)
      {
        if (config.IsUseApi.Value)
        {
          Trace.Info("DotMemory.RunConsole: force to use API");
          api = DynamicMemoryProfilerApi.TryCreate();
          if (api == null)
            throw new InvalidOperationException("Unable to load `JetBrains.Profiler.Api` assembly.");
        }
        else
        {
          Trace.Info("DotMemory.RunConsole: force to do not use API");
        }
      }
      else // auto mode
      {
        Trace.Info("DotMemory.RunConsole: auto API mode...");
        api = DynamicMemoryProfilerApi.TryCreate();
        Trace.Info(api != null
          ? "DotMemory.RunConsole: API assembly found, will use it"
          : "DotMemory.RunConsole: API assembly not found, will use service messages");
      }
      
      if (api != null)
        commandLine.Append(" --use-api");

      Trace.Info("DotMemory.RunConsole:\n  runner = `{0}`\n  arguments = `{1}`", runnerPath, commandLine);
    
      var console = Process.Start(
        new ProcessStartInfo
        {
          FileName = runnerPath,
          Arguments = commandLine.ToString(),
          CreateNoWindow = true,
          UseShellExecute = false,
          RedirectStandardInput = true,
          RedirectStandardOutput = true,
          RedirectStandardError = true
        }
      );
    
      if (console == null)
        throw new InvalidOperationException("Something went wrong: unable to start dotMemory console profiler.");
      
      Trace.Verbose("DotMemory.RunConsole: Runner started.");

      return new Session(console, api, workspaceFile);
    }

    private sealed class Prerequisite : PrerequisiteBase
    {
      public Prerequisite() : base("dotMemory", NupkgVersion)
      {
      }
      
      protected override string GetRunnerName()
      {
        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
          throw new NotSupportedException("Platforms other than Windows not yet supported.");

        return "dotMemory.exe";

        // The code below is for future use: specific runner name for OS and bitness
        /*string osSuffix, ext;

        switch (Environment.OSVersion.Platform)
        {
          case PlatformID.Win32NT:
            osSuffix = "Win";
            ext = "exe";
            break;

          default:
            throw new NotSupportedException();
        }

        var bitnessSuffix = Environment.Is64BitProcess ? "x64" : "x86";

        return $"dotMemory.{osSuffix}.{bitnessSuffix}.{ext}";*/
      }

      protected override string GetPackageName()
      {
        return "JetBrains.dotMemory.Console";
        
        // The code below is for future use: specific package name for OS and bitness - for prerequisite size optimization
        /*string osSuffix;

        switch (Environment.OSVersion.Platform)
        {
          case PlatformID.Win32NT:
            osSuffix = "Win";
            break;

          default:
            throw new NotSupportedException();
        }

        var bitnessSuffix = Environment.Is64BitProcess ? "x64" : "x86";

        return $"JetBrains.dotMemory.{osSuffix}.{bitnessSuffix}";*/
      }

      protected override long GetEstimatedSize()
      {
        return 20 * 1024 * 1024;
      }
    }

    private sealed class Session
    {
      [SuppressMessage("ReSharper", "InconsistentNaming")]
      private const string __dotMemory = "##dotMemory";
      
      private readonly List<string> _outputLines = new List<string>();
      private readonly List<string> _errorLines = new List<string>();
      private readonly Process _console;
      private readonly DynamicMemoryProfilerApi _profilerApi;

      public Session(Process console, DynamicMemoryProfilerApi profilerApi, string workspaceFile)
      {
        _console = console;
        _profilerApi = profilerApi;
        
        console.OutputDataReceived +=
          (sender, args) =>
            {
              if (args.Data != null)
              {
                lock (_outputLines)
                {
                  _outputLines.Add(args.Data);
                  Trace.Verbose(args.Data);
                }
              }
            };

        console.ErrorDataReceived +=
          (sender, args) =>
            {
              if (args.Data != null)
              {
                lock (_errorLines)
                {
                  _errorLines.Add(args.Data);
                  Trace.Verbose(args.Data);
                }
              }
            };

        console.BeginOutputReadLine();
        console.BeginErrorReadLine();
        
        WorkspaceFile = workspaceFile;
      }

      public string WorkspaceFile { get; }

      [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
      public Session Detach()
      {
        if (_profilerApi != null)
          _profilerApi.Detach();
        else
          Send("disconnect");
        return this;
      }
      
      [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
      public Session GetSnapshot(string name)
      {
        if (_profilerApi != null)
          _profilerApi.GetSnapshot(name);
        else
        {
          Send("get-snapshot", "name", name);
          AwaitSnapshotSaved();
        }
        return this;
      }
      
      public Session AwaitConnected(int milliseconds)
      {
        var regex = new Regex(__dotMemory + @"\[\x22connected\x22,\s*\{.*\}\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        if (TryAwaitFor(regex, milliseconds) == null)
          throw ConsoleException("The dotMemory console profiler was not connected. See details below.");
        
        if (_profilerApi != null)
        {
          var startTime = DateTime.UtcNow;
          while ((_profilerApi.GetFeatures() & 0x1) != 0x1)
          {
            if (_console.HasExited)
              throw ConsoleException("The dotMemory console profiler has exited unexpectedly. See details below.");

            if (milliseconds >= 0 && (DateTime.UtcNow - startTime).TotalMilliseconds > milliseconds)
              throw ConsoleException("The Profiler.Api was not became ready in given time. See details below.");

            Thread.Sleep(40);
          }
        }

        return this;
      }
      
      private Session AwaitSnapshotSaved()
      {
        var regex = new Regex(__dotMemory + @"\[\x22snapshot-saved\x22,\s*\{.*\}\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        TryAwaitFor(regex, -1);
        return this;
      }
      
      public Session AwaitFinished(int milliseconds)
      {
        if (!_console.WaitForExit(milliseconds))
          throw ConsoleException("The dotMemory console profiler has not finished in given time. See details below.");

        if (_console.ExitCode != 0)
          throw ConsoleException("The dotMemory console profiler has failed. See details below.");

        return this;
      }
      
      private Match TryAwaitFor(Regex regex, int milliseconds)
      {
        var startTime = DateTime.UtcNow;
        var lineNum = 0;
        while (true)
        {
          lock (_outputLines)
          {
            while (lineNum < _outputLines.Count)
            {
              var line = _outputLines[lineNum++];
              var match = regex.Match(line);
              if (match.Success)
                return match;
            }
          }

          if (_console.HasExited)
            return null;

          if (milliseconds >= 0 && (DateTime.UtcNow - startTime).TotalMilliseconds > milliseconds)
            return null;
          
          Thread.Sleep(40);
        }
      }
      
      private void Send(string command, params string[] args)
      {
        var messageBuilder = new StringBuilder();
        messageBuilder.Append(__dotMemory).Append("[\"").Append(command).Append("\"");

        if (args != null && args.Length > 0)
        {
          messageBuilder.Append(",{");
          for (var i = 0; i < args.Length; i += 2)
          {
            messageBuilder.Append(args[i]).Append(":");
            
            if (args[i + 1] != null)
              messageBuilder.Append("\"").Append(args[i + 1].Replace('"', '`')).Append("\"");
            else
              messageBuilder.Append("null");
            
            if (i > 0)
              messageBuilder.Append(",");
          }
          messageBuilder.Append("}");
        }
        
        messageBuilder.Append("]");

        var message = messageBuilder.ToString();
        Trace.Verbose(message);
        _console.StandardInput.WriteLine(message);
      }

      private InvalidOperationException ConsoleException(string caption)
      {
        var message = new StringBuilder();
        message.AppendLine(caption);
        
        message.AppendLine("*** Standard Error ***");
        lock (_errorLines)
          message.AppendLine(string.Join(Environment.NewLine, _errorLines));

        message.AppendLine();
        message.AppendLine("*** Standard Output ***");
        lock (_outputLines)
          message.AppendLine(string.Join(Environment.NewLine, _outputLines));
        
        throw new InvalidOperationException(message.ToString());
      }
    }
  }
}