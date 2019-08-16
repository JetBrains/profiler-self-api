using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JetBrains.Profiler.SelfApi
{
  /// <summary>
  /// The dotMemory self profiling API. Based on single-exe console runner
  /// which is automatically downloaded as NuGet-package. 
  /// </summary>
  /// <remarks>
  /// Target use case:
  /// * install NuGet (just NuGet, no any other actions required); or simple copy this file into your project
  /// * insert into code DotMemory.EnsurePrerequisite()
  /// * then call DotMemory.GetSnapshotOnce (or Attach/GetSnapshot*/Detach)
  /// * deploy to staging
  /// * reproduce issue
  /// * take over generated workspace for investigation 
  /// </remarks>
  public static class DotMemory
  {
    [Serializable]
    public sealed class DotMemoryException : Exception
    {
      public DotMemoryException(string message) : base(message)
      {
      }
      
      public DotMemoryException(SerializationInfo info, StreamingContext context) : base(info, context)
      {
      }
    }

    public sealed class Config
    {
      internal string PrerequisitePath;
      internal string WorkspaceFile;
      internal bool IsOpenDotMemory;

      public Config UsePrerequisite(string prerequisitePath)
      {
        PrerequisitePath = prerequisitePath ?? throw new ArgumentNullException(nameof(prerequisitePath));
        return this;
      }
      
      public Config SaveToFile(string filePath)
      {
        WorkspaceFile = filePath ?? throw new ArgumentNullException(nameof(filePath));
        return this;
      }
      
      public Config OpenDotMemory()
      {
        IsOpenDotMemory = true;
        return this;
      }
    }

    public interface IProgress
    {
      void Notify(int percents);
    }


    private const string SemanticVersion = "192.0.20190807.154300";
    private static readonly Uri NugetOrgUrl = new Uri("https://www.nuget.org/api/v2/package");
    private const int Timeout = 30000;
    private static readonly object Mutex = new object();
    private static Task _prerequisiteTask;
    private static Session _session;

    /// <summary>
    /// Ensures prerequisite (dotMemory console runner) for current OS and process bitness is downloaded and ready to use.
    /// </summary>
    /// <param name="progress">The progress callback. If null progress will not be reported.</param>
    /// <param name="nugetUrl">The URL of NuGet mirror. If null the default www.nuget.org will be used.</param>
    /// <param name="prerequisitePath">The path to download prerequisite to. If null %LocalAppData% is used.</param>
    public static Task EnsurePrerequisiteAsync(IProgress progress = null, Uri nugetUrl = null, string prerequisitePath = null)
    {
      lock (Mutex)
      {
        if (_prerequisiteTask != null)
          return _prerequisiteTask;

        if (Prerequisite.TryGetRunner(prerequisitePath, out _))
          return _prerequisiteTask = Task.CompletedTask;
        
        if (nugetUrl == null)
          nugetUrl = NugetOrgUrl;
        
        return _prerequisiteTask = Prerequisite.EnsureAsync(progress, nugetUrl, prerequisitePath);
      }
    }

    /// <summary>
    /// The shortcut for <see cref="EnsurePrerequisiteAsync"/><c>.Wait()</c>
    /// </summary>
    public static void EnsurePrerequisite(Uri nugetUrl = null, string prerequisitePath = null)
    {
      EnsurePrerequisiteAsync(null, nugetUrl, prerequisitePath).Wait();
    }
    
    /// <summary>
    /// The shortcut for <see cref="Attach()"/>, <see cref="GetSnapshot"/>, <see cref="Detach"/>.
    /// </summary>
    /// <returns>Saved workspace file path.</returns>
    public static string GetSnapshotOnce()
    {
      return GetSnapshotOnce(new Config());
    }

    /// <summary>
    /// The shortcut for <see cref="Attach(Config)"/>, <see cref="GetSnapshot"/>, <see cref="Detach"/>.
    /// </summary>
    /// <returns>Saved workspace file path.</returns>
    public static string GetSnapshotOnce(Config config)
    {
      if (config == null) throw new ArgumentNullException(nameof(config));

      lock (Mutex)
      {
        if (_prerequisiteTask == null || !_prerequisiteTask.Wait(40))
          throw new DotMemoryException("The prerequisite isn't ready: forgot to call EnsurePrerequisiteAsync?");
        
        if (_session != null)
          throw new DotMemoryException("The profiling session is active already: Attach was called early.");

        return RunConsole("get-snapshot", config).AwaitFinished(-1).WorkspaceFile;
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

      lock (Mutex)
      {
        if (_prerequisiteTask == null || !_prerequisiteTask.Wait(40))
          throw new DotMemoryException("The prerequisite isn't ready: forgot to call EnsurePrerequisiteAsync?");
        
        if (_session != null)
          throw new DotMemoryException("The profiling session is active still: forgot to call Detach?");

        _session = RunConsole("attach -s", config).AwaitConnected(Timeout);
      }
    }

    /// <summary>
    /// Detaches dotMemory from current process.
    /// </summary>
    /// <returns>Saved workspace file path.</returns>
    public static string Detach()
    {
      lock (Mutex)
      {
        if (_session == null)
          throw new DotMemoryException("The profiling session isn't active: forgot to call Attach?");

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
      lock (Mutex)
      {
        if (_session == null)
          throw new DotMemoryException("The profiling session isn't active: forgot to call Attach?");

        _session.GetSnapshot(name);
      }
    }

    private static string GetSaveToFilePath()
    {
      return Path.Combine(Path.GetTempPath(), $"{Process.GetCurrentProcess().ProcessName}.{DateTime.Now:yyyy-MM-ddTHH-mm-ss.fff}.dmw");
    }

    private static Session RunConsole(string command, Config config)
    {
      if (!Prerequisite.TryGetRunner(config.PrerequisitePath, out var runnerPath))
        throw new DotMemoryException("The dotMemory console profiler not found.");
      
      if (config.WorkspaceFile == null)
        config.WorkspaceFile = GetSaveToFilePath();
      
      var commandLine = new StringBuilder();
      commandLine.Append($"{command} {Process.GetCurrentProcess().Id} \"-f={config.WorkspaceFile}\"");

      if (config.IsOpenDotMemory)
        commandLine.Append(" --open-dotmemory");
    
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
        throw new DotMemoryException("Something went wrong: unable to start dotMemory console profiler.");

      return new Session(console, config.WorkspaceFile);
    }

    private static class Prerequisite
    {
      public static async Task EnsureAsync(IProgress progress, Uri nugetUrl, string downloadTo)
      {
        if (string.IsNullOrEmpty(downloadTo))
          downloadTo = GetAppLocalPath();

        Directory.CreateDirectory(downloadTo);

        var nupkgPath = Path.GetTempFileName();

        using (var httpClient = new HttpClient())
        {
          var packageUrl = new UriBuilder(nugetUrl);
          packageUrl.Path += $"/{GetPackageName()}/{SemanticVersion}";

          using (var input = await httpClient.GetStreamAsync(packageUrl.Uri).ConfigureAwait(false))
          using (var output = File.Create(nupkgPath))
          {
            input.CopyTo(output);
          }
        }

        using (var zipInput = File.OpenRead(nupkgPath))
        using (var nupkg = new ZipArchive(zipInput))
        {
          var runnerName = GetRunnerName();
          var entry = nupkg.Entries
            .FirstOrDefault(x => string.Equals(x.Name, runnerName, StringComparison.OrdinalIgnoreCase));
          if (entry == null)
            throw new DotMemoryException("Something went wrong: unable to find dotMemory console profiler inside NuGet package.");

          using (var input = entry.Open())
          using (var output = File.Create(Path.Combine(downloadTo, runnerName)))
          {
            input.CopyTo(output);
          }
        }
        
        File.Delete(nupkgPath);
      }

      public static bool TryGetRunner(string prerequisitePath, out string runnerPath)
      {
        var runnerName = GetRunnerName();

        if (!string.IsNullOrEmpty(prerequisitePath))
        {
          runnerPath = Path.Combine(prerequisitePath, runnerName);
          return File.Exists(runnerPath);
        }

        runnerPath = Path.Combine(GetNearbyPath(), runnerName);
        if (File.Exists(runnerPath))
          return true;

        runnerPath = Path.Combine(GetAppLocalPath(), runnerName);
        if (File.Exists(runnerPath))
          return true;

        return false;
      }

      private static string GetRunnerName()
      {
        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
          throw new DotMemoryException("Platforms other than Windows not yet supported.");

        return "dotMemory.exe";

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

      private static string GetPackageName()
      {
        return "JetBrains.dotMemory.Console";
        
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

      private static string GetNearbyPath()
      {
        var assembly = Assembly.GetExecutingAssembly();
        return string.IsNullOrEmpty(assembly.Location) ? string.Empty : Path.GetDirectoryName(assembly.Location);
      }

      private static string GetAppLocalPath()
      {
        return Path.Combine(
          Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
          $"JetBrains/ProfilerSelfApi/{SemanticVersion}"
        );
      }
    }

    private sealed class Session
    {
      [SuppressMessage("ReSharper", "InconsistentNaming")]
      private const string __dotMemory = "##dotMemory";
      
      private readonly List<string> _outputLines = new List<string>();
      private readonly List<string> _errorLines = new List<string>();
      private readonly Process _console;

      public Session(Process console, string workspaceFile)
      {
        _console = console;
        
        console.OutputDataReceived +=
          (sender, args) =>
            {
              if (args.Data != null)
              {
                lock (_outputLines)
                  _outputLines.Add(args.Data);
              }
            };

        console.ErrorDataReceived +=
          (sender, args) =>
            {
              if (args.Data != null)
              {
                lock (_errorLines)
                  _errorLines.Add(args.Data);
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
        Send("disconnect");
        return this;
      }
      
      [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
      public Session GetSnapshot(string name)
      {
        Send("get-snapshot", "name", name);
        return this;
      }
      
      public Session AwaitConnected(int milliseconds)
      {
        var regex = new Regex(__dotMemory + @"\[\x22connected\x22,\s*\{.*\}\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        if (TryAwaitFor(regex, milliseconds) == null)
          throw DotMemoryConsoleException("The dotMemory console profiler was not connected. See details below.");

        return this;
      }
      
      public Session AwaitFinished(int milliseconds)
      {
        if (!_console.WaitForExit(milliseconds))
          throw DotMemoryConsoleException("The dotMemory console profiler has not finished in given time. See details below.");

        if (_console.ExitCode != 0)
          throw DotMemoryConsoleException("The dotMemory console profiler has failed. See details below.");

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

        _console.StandardInput.WriteLine(messageBuilder.ToString());
      }

      private DotMemoryException DotMemoryConsoleException(string caption)
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
        
        throw new DotMemoryException(message.ToString());
      }
    }
  }
}