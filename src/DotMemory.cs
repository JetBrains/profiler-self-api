using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace JetBrains.Profiler.SelfApi
{
  /// <summary>
  /// The dotMemory self profiling API based on single-exe console runner which is automatically downloaded as NuGet-package. 
  /// </summary>
  /// <remarks>
  /// Use case: ad-hoc profiling<br/>
  /// * install NuGet (just NuGet, no any other actions required); or simple copy this file into your project<br/>
  /// * on initialization phase call DotMemory.EnsurePrerequisite()<br/>
  /// * in profiled peace of code call DotMemory.GetSnapshotOnce (or Attach/GetSnapshot*/Detach)<br/>
  /// * deploy to staging<br/>
  /// * reproduce issue<br/>
  /// * take over generated workspace for investigation<br/>
  ///<br/>
  /// Use case: self profiling as part of troubleshooting on production  
  /// * install NuGet (just NuGet, no any other actions required); or simple copy this file into your project
  /// * in handler of awesome `Gather trouble report` action call DotMemory.EnsurePrerequisite()
  /// * then call DotMemory.GetSnapshotOnce
  /// * include generated workspace into report 
  /// </remarks>
  [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Local")]
  [SuppressMessage("ReSharper", "UnusedMember.Global")]
  public static class DotMemory
  {
    /// <summary>
    /// Self profiling configuration.
    /// </summary>
    public sealed class Config
    {
      internal string PrerequisitePath;
      internal string WorkspaceFile;
      internal string WorkspaceDir;
      internal bool IsOpenDotMemory;
      internal bool IsOverwriteWorkspace;

      /// <summary>
      /// Specifies path to install prerequisite to.
      /// </summary>
      public Config UsePrerequisitePath(string prerequisitePath)
      {
        PrerequisitePath = prerequisitePath ?? throw new ArgumentNullException(nameof(prerequisitePath));
        return this;
      }
      
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
    }

    /// <summary>
    /// Operation progress callback. 
    /// </summary>
    public interface IProgress
    {
      /// <summary>
      /// Advances progress to given number of percents.
      /// The sum of all <paramref name="percentsDelta"/>-s is less or equal to 100.
      /// </summary>
      void Advance(double percentsDelta);
    }

    private const string SemanticVersion = "192.0.20190807.154300";
    private static readonly Uri NugetOrgUrl = new Uri("https://www.nuget.org/api/v2/package");
    private const int Timeout = 30000;
    private static readonly object Mutex = new object();
    private static Task _prerequisiteTask;
    private static string _prerequisitePath;
    private static Session _session;

    /// <summary>
    /// Ensures prerequisite (dotMemory console runner) for current OS and process bitness is downloaded and ready to use.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <param name="progress">The progress callback. If null progress will not be reported.</param>
    /// <param name="nugetUrl">The URL of NuGet mirror. If null the default www.nuget.org will be used.</param>
    /// <param name="prerequisitePath">The path to download prerequisite to. If null %LocalAppData% is used.</param>
    public static Task EnsurePrerequisiteAsync(
      CancellationToken cancellationToken, 
      IProgress progress = null, 
      Uri nugetUrl = null, 
      string prerequisitePath = null)
    {
      lock (Mutex)
      {
        if (_prerequisiteTask != null)
        {
          var status = _prerequisiteTask.Status;
          if (status != TaskStatus.Faulted && status != TaskStatus.Canceled)
            return _prerequisiteTask;
        }

        _prerequisiteTask = null;
        _prerequisitePath = prerequisitePath;
        
        if (Prerequisite.TryGetRunner(prerequisitePath, out _))
          return _prerequisiteTask = Task.FromResult(Missing.Value);
        
        if (nugetUrl == null)
          nugetUrl = NugetOrgUrl;
        
        return _prerequisiteTask = Prerequisite.EnsureAsync(cancellationToken, progress, nugetUrl, prerequisitePath);
      }
    }

    /// <summary>
    /// The shortcut for <c>EnsurePrerequisiteAsync(CancellationToken.None, progress, nugetUrl, prerequisitePath)</c>
    /// </summary>
    public static Task EnsurePrerequisiteAsync(
      IProgress progress = null,
      Uri nugetUrl = null, 
      string prerequisitePath = null)
    {
      return EnsurePrerequisiteAsync(CancellationToken.None, progress, nugetUrl, prerequisitePath);
    }

    /// <summary>
    /// The shortcut for <c>EnsurePrerequisiteAsync(CancellationToken.None, progress: null, nugetUrl, prerequisitePath).Wait()</c>
    /// </summary>
    public static void EnsurePrerequisite(Uri nugetUrl = null, string prerequisitePath = null)
    {
      EnsurePrerequisiteAsync(null, nugetUrl, prerequisitePath).Wait();
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
        if (_prerequisiteTask == null || !_prerequisiteTask.Wait(40))
          throw new InvalidOperationException("The prerequisite isn't ready: forgot to call EnsurePrerequisiteAsync()?");
        
        if (_session != null)
          throw new InvalidOperationException("The profiling session is active already: Attach() was called early.");

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
      lock (Mutex)
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
      if (!Prerequisite.TryGetRunner(config.PrerequisitePath ?? _prerequisitePath, out var runnerPath))
        throw new InvalidOperationException("Something went wrong: the dotMemory console profiler not found.");
      
      var workspaceFile = GetSaveToFilePath(config);
      
      var commandLine = new StringBuilder();
      commandLine.Append($"{command} {Process.GetCurrentProcess().Id} \"-f={workspaceFile}\"");

      if (config.IsOverwriteWorkspace)
        commandLine.Append(" --overwrite");
      
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
        throw new InvalidOperationException("Something went wrong: unable to start dotMemory console profiler.");

      return new Session(console, workspaceFile);
    }

    private static class Prerequisite
    {
      public static async Task EnsureAsync(
        CancellationToken cancellationToken, 
        IProgress progress, 
        Uri nugetUrl,
        string downloadTo)
      {
        const double downloadWeigth = 0.8;
        const double unzipWeigth = 0.2;
        const long estimatedLength = 20 * 1024 * 1024;

        downloadTo = string.IsNullOrEmpty(downloadTo) 
          ? GetAppLocalPath() 
          : Path.Combine(downloadTo, SemanticVersion);

        Directory.CreateDirectory(downloadTo);

        var nupkgName = GetPackageName();
        var nupkgPath = Path.Combine(downloadTo, $"{nupkgName}.{SemanticVersion}.nupkg");

        using (var httpClient = new HttpClient())
        {
          var packageUrl = new UriBuilder(nugetUrl);
          packageUrl.Path += $"/{nupkgName}/{SemanticVersion}";

          using (var response = await httpClient
            .GetAsync(packageUrl.Uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false))
          {
            response.EnsureSuccessStatusCode();
            using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            using (var output = File.Create(nupkgPath))
            {
              var length = response.Content.Headers.ContentLength ?? estimatedLength;
              Copy(input, output, length, progress, downloadWeigth);
            }
          }
        }

        using (var zipInput = File.OpenRead(nupkgPath))
        using (var nupkg = new ZipArchive(zipInput))
        {
          var runnerName = GetRunnerName();
          var entry = nupkg.Entries
            .FirstOrDefault(x => string.Equals(x.Name, runnerName, StringComparison.OrdinalIgnoreCase));
          if (entry == null)
            throw new InvalidOperationException("Something went wrong: unable to find dotMemory console profiler inside NuGet package.");

          using (var input = entry.Open())
          using (var output = File.Create(Path.Combine(downloadTo, runnerName)))
          {
            Copy(input, output, entry.Length, progress, unzipWeigth);
          }
        }
        
        File.Delete(nupkgPath);
      }

      public static bool TryGetRunner(string prerequisitePath, out string runnerPath)
      {
        var runnerName = GetRunnerName();

        if (!string.IsNullOrEmpty(prerequisitePath))
        {
          runnerPath = Path.Combine(prerequisitePath, SemanticVersion, runnerName);
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

      private static void Copy(
        Stream @from, 
        Stream to, 
        long length, 
        IProgress progress, 
        double progressWeight)
      {
        var buffer = new byte[65535];
        var percents = 0L;
        var bytesCopied = 0L;
        
        while (true)
        {
          var bytesRead = from.Read(buffer, 0, buffer.Length);
          if (bytesRead <= 0)
            break;
          
          to.Write(buffer, 0, bytesRead);
          bytesCopied += bytesRead;

          if (progress == null) 
            continue;
          
          var newPercents = bytesCopied < length ? bytesCopied * 100 / length : 100;
          var delta = (newPercents - percents) * progressWeight;
          if (delta < 1.0) 
            continue;
          
          progress.Advance(delta);
          percents = newPercents;
        }
      }

      private static string GetRunnerName()
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

      private static string GetPackageName()
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

      private static string GetNearbyPath()
      {
        var assembly = Assembly.GetExecutingAssembly();
        return string.IsNullOrEmpty(assembly.Location) ? string.Empty : Path.GetDirectoryName(assembly.Location);
      }

      private static string GetAppLocalPath()
      {
        return Path.Combine(
          Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
          $"JetBrains/Profiler/SelfApi/{SemanticVersion}"
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
          throw ConsoleException("The dotMemory console profiler was not connected. See details below.");

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

        _console.StandardInput.WriteLine(messageBuilder.ToString());
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