using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.IO.Compression;
using System.Threading.Tasks;
using JetBrains.Profiler.Api;
using JetBrains.Profiler.SelfApi.Impl;

namespace JetBrains.Profiler.SelfApi
{
  /// <summary>
  /// The API lets you initiate and control profiling sessions right from the code of your application.
  /// For example, you can use it to profile the application on end-user desktops or production servers.
  /// The API uses the dotTrace command-line profiler (the tool is downloaded automatically)
  /// </summary>
  /// <remarks>
  /// Use case: ad-hoc profiling <br/>
  /// * Install the JetBrains.Profiler.SelfApi package to your project<br/>
  /// * To initialize the API, call DotTrace.EnsurePrerequisite()<br/>
  /// * To start collecting data, call DotTrace.StartCollectingData()
  /// * To save collected data into snapshot, call DotTrace.SaveData<br/>
  /// * In case you need several snapshots, call DotTrace.StartCollectingData and DotTrace.SaveData once more<br/>
  /// * To pack collected data into a single zip file call DotTrace.GetCollectedSnapshotFilesArchive <br/>
  /// * Investigate the generated snapshots using JetBrains dotTrace<br/>
  ///<br/>
  /// </remarks>
  [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Local")]
  [SuppressMessage("ReSharper", "UnusedMember.Global")]
  [SuppressMessage("ReSharper", "UnusedType.Global")]
  public static class DotTrace
  {
    private const string MessageServicePrefix = "##dotTrace";
    private const string CltPresentableName = "dotTrace console profiler";

    /// <summary>
    /// The version of JetBrains.dotTrace.Console NuGet-package that must be downloaded.
    /// </summary>
    private static readonly NuGet.SemanticVersion NupkgVersion = new NuGet.SemanticVersion(2020, 2);

    /// <summary>
    /// Self-profiling configuration
    /// </summary>
    public sealed class Config : CommonConfig
    {
      internal string SnapshotFile;
      internal string SnapshotDir;
      internal bool IsOverwriteSnapshot;

      /// <summary>
      /// Specifies the path to the snapshot index file.
      /// If you take more than one snapshot, the first snapshot will get the specified file name.
      /// Others will additionally get the suffix "-[snapshot-number]". 
      /// The directory specified in the path must already exist.
      /// <param name="snapshotFile">Path to the file</param>
      /// <param name="overwrite">Overwrite the file if it exists</param>
      /// </summary>
      public Config SaveToFile(string snapshotFile, bool overwrite = false)
      {
        if (SnapshotDir != null)
          throw new InvalidOperationException("SaveToFile and SaveToDir are mutually exclusive.");
        if (snapshotFile == null) throw new ArgumentNullException(nameof(snapshotFile));

        if (Directory.Exists(snapshotFile))
          throw new InvalidOperationException($"The directory {snapshotFile} was specified. If you want to save files to this directory, use {nameof(SaveToDir)} instead");
        SnapshotFile = snapshotFile;
        IsOverwriteSnapshot = overwrite;

        return this;
      }

      /// <summary>
      /// Specifies the path to the directory where snapshots files must be saved (names will be auto-generated).
      /// The directory must already exist.
      /// <param name="dirPath">Path to the directory</param>
      /// </summary>
      public Config SaveToDir(string dirPath)
      {
        if (SnapshotFile != null)
          throw new InvalidOperationException("SaveToDir and SaveToFile are mutually exclusive.");
        if (dirPath == null) throw new ArgumentNullException(nameof(dirPath));

        if (!Directory.Exists(dirPath))
          throw new InvalidOperationException("The specified directory does not exist.");

        SnapshotDir = dirPath;

        return this;
      }
    }

    private const int Timeout = 30000;
    private static readonly Prerequisite ConsoleRunnerPackage = new Prerequisite();
    private static readonly object Mutex = new object();

    private static Session _session;
    private static string[] _collectedSnapshots;
    private static HashSet<string> _deletedIndexFiles;
    private static int _packedInZipCount;

    /// <summary>
    /// Makes sure that the dotTrace command-line profiler is downloaded and is ready to use.
    /// </summary>
    /// <remarks>
    /// 1. Looks for dotTrace executable in the same directory with the running assembly. Uses it if it's found.<br/>
    /// 2. Downloads `JetBrains.dotTrace.CommandLineTools` NuGet package into the <paramref name="downloadTo"/>
    /// directory and uses the dotTrace command-line profiler from this package. The package version is defined by <see cref="NupkgVersion"/>.
    /// The command-line profiler is saved to `{downloadTo}/dotTrace.{NupkgVersion}`
    /// If the executable file exists, a new one is not downloaded.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="progress">Download progress callback from 0.0 to 100.0. If null, progress is not reported.</param>
    /// <param name="nugetUrl">URL of NuGet mirror. If null, www.nuget.org is used.</param>
    /// <param name="nugetApi">NuGet API version.</param>
    /// <param name="downloadTo">NuGet download destination directory. If null, %LocalAppData% is used.</param>
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
    /// Attaches dotTrace to the current process using the default API configuration.
    /// Note: After attaching, the profiler goes to "Stopped" state. To start collecting data,
    /// call <see cref="StartCollectingData"/>
    /// </summary>
    public static void Attach()
    {
      Attach(new Config());
    }

    /// <summary>
    /// Attaches dotTrace to the current process using the specified API configuration.
    /// Note: After attaching, the profiler goes to "Stopped" state. To start collecting data,
    /// call <see cref="StartCollectingData"/>
    /// <param name="config">Profiler configuration</param>
    /// </summary>
    public static void Attach(Config config)
    {
      if (config == null)
        throw new ArgumentNullException(nameof(config));

      Helper.CheckAttachCompatibility();
      Helper.CheckSamplingCompatibility();

      lock (Mutex)
      {
        ConsoleRunnerPackage.VerifyReady();

        if (_session != null)
          throw new InvalidOperationException("The profiling session is not active: Did you call Attach()?");

        _collectedSnapshots = null;
        _deletedIndexFiles = new HashSet<string>();
        _packedInZipCount = 0;
        _session = RunProfiler(config).AwaitConnected(Timeout);
      }
    }

    /// <summary>
    /// Detaches dotTrace from the current process
    /// Call it after you finish profiling
    /// </summary>
    public static void Detach()
    {
      lock (Mutex)
      {
        if (_session == null)
          throw new InvalidOperationException("The profiling session is not active: Did you call Attach()?");

        try
        {
          _session.Detach().AwaitFinished(Timeout);
          _collectedSnapshots = _session.GetCollectedSnapshotsIndexFiles();
        }
        finally
        {
          _session = null;
        }
      }
    }

    /// <summary>
    /// Starts collecting performance data
    /// Profiler changes state from "Stopped" to "Started"
    /// If profiler is already in "Started" state, the command is ignored
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public static void StartCollectingData()
    {
      lock (Mutex)
      {
        if (_session == null)
          throw new InvalidOperationException("The profiling session is not active: Did you call Attach()?");

        _session.StartCollectingData();
      }
    }

    /// <summary>
    /// Takes a snapshot of the current process
    /// All collected data is saved to files specified in the configuration
    /// Note: After saving the data, profiler goes to "Stopped" state
    /// To start collecting data again, call <see cref="StartCollectingData"/> 
    /// </summary>
    public static void SaveData()
    {
      lock (Mutex)
      {
        if (_session == null)
          throw new InvalidOperationException("The profiling session is not active: Did you call Attach()?");

        _session.SaveData();
      }
    }

    /// <summary>
    /// Drops collected data
    /// Run this method to discard collected data. 
    /// Note: After discarding the data, profiler goes to "Stopped" state
    /// To start collecting data again, call <see cref="StartCollectingData"/> 
    /// </summary>
    public static void DropData()
    {
      lock (Mutex)
      {
        if (_session == null)
          throw new InvalidOperationException("The profiling session is not active: Did you call Attach()?");

        _session.DropData();
      }
    }

    /// <summary>
    /// Stops collecting performance data
    /// Profiler changes state from "Started" to "Stopped".
    /// If profiler is already in "Stopped" state, the command is ignored.
    /// It is not necessary to call this method before taking snapshot.
    /// Note: The command is supported only if profiler API is used to control session.
    /// Otherwise, the command is ignored.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public static void StopCollectingData()
    {
      lock (Mutex)
      {
        if (_session == null)
          throw new InvalidOperationException("The profiling session is not active: Did you call Attach()?");

        _session.StopCollectingData();
      }
    }

    private static string[] GetCollectedSnapshotIndexFilesCore()
    {
      if (_session != null)
        return _session.GetCollectedSnapshotsIndexFiles();
      if (_collectedSnapshots != null)
        return _collectedSnapshots;

      throw new InvalidOperationException("The profiling session was never initiated: Did you call Attach()?");
    }

    /// <summary>
    /// Returns collected snapshot index files
    /// For each collected snapshot, a separate index file is returned.
    /// Note: To share a snapshot, you must copy not only the index file but all files.
    /// In such cases, it's more convenient to use
    /// <see cref="GetCollectedSnapshotFilesArchive"/> or <see cref="GetCollectedSnapshotFiles"/>
    /// </summary>
    /// <returns>Paths to index files</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static string[] GetCollectedSnapshotIndexFiles()
    {
      lock (Mutex)
        return GetCollectedSnapshotIndexFilesCore().Where(f => !_deletedIndexFiles.Contains(f)).ToArray();
    }

    /// <summary>
    /// Returns all collected snapshot files (including index files)
    /// For each collected snapshot, more than one file is returned
    /// </summary>
    /// <returns>Paths to snapshot files</returns>
    public static string[] GetCollectedSnapshotFiles()
    {
      lock (Mutex)
        return GetCollectedSnapshotIndexFilesCore().Where(f => !_deletedIndexFiles.Contains(f)).SelectMany(GetSnapshotFiles).ToArray();
    }

    /// <summary>
    /// Returns all collected snapshot files (including index files)
    /// packed into a single zip file
    /// </summary>
    /// <returns>Path to the zip file. Returns null if data is not yet collected.</returns>
    public static string GetCollectedSnapshotFilesArchive(bool deleteUnpackedFiles)
    {
      lock (Mutex)
      {
        var indexFiles = GetCollectedSnapshotIndexFilesCore();
        if (_packedInZipCount >= indexFiles.Length)
          return null;

        var firstIndexFile = indexFiles[_packedInZipCount];
        var directory = Path.GetDirectoryName(firstIndexFile);
        var name = Path.GetFileNameWithoutExtension(firstIndexFile);
        var zipFilePatch = CreateUniqFileName(directory, name, "zip");
        var packedIndexFiles = new List<string>();
        var packedFiles = new List<string>();

        using (var zipInput = File.OpenWrite(zipFilePatch))
        using (var zip = new ZipArchive(zipInput, ZipArchiveMode.Create))
          for (var index = _packedInZipCount; index < indexFiles.Length; index++)
          {
            var indexFile = indexFiles[index];
            packedIndexFiles.Add(indexFile);
            foreach (var file in GetSnapshotFiles(indexFile))
            {
              zip.CreateEntryFromFile(file, Path.GetFileName(file));
              packedFiles.Add(file);
            }
          }

        if (deleteUnpackedFiles)
        {
          foreach (var packedFile in packedFiles)
          {
            try
            {
              File.Delete(packedFile);
            }
            catch
            {
            }
          }

          foreach (var packedIndexFile in packedIndexFiles)
            _deletedIndexFiles.Add(packedIndexFile);
        }

        _packedInZipCount = indexFiles.Length;
        return zipFilePatch;
      }
    }

    private static string[] GetSnapshotFiles(string indexFile)
    {
      var snapshotFilePattern = Path.GetFileName(indexFile) + ".*";
      var directory = Path.GetDirectoryName(indexFile);
      return Directory.GetFiles(directory ?? ".", snapshotFilePattern, SearchOption.TopDirectoryOnly);
    }

    private static string CreateUniqFileName(string directory, string name, string extension)
    {
      for (var i = 0; i < 10; i++)
      {
        var path = Path.Combine(directory, $"{name}-{DateTime.UtcNow:yyyy-MM-ddTHH-mm-ss.fffffffK}.{extension}");
        if (!File.Exists(path) && !Directory.Exists(path))
          return path;
      }

      throw new IOException("Unable to create the archive file");
    }

    private static Session RunProfiler(Config config)
    {
      Trace.Verbose("DotTrace.RunConsole: Looking for runner...");

      var runnerPath = ConsoleRunnerPackage.GetRunnerPath();

      var commandLine = new StringBuilder();

      commandLine.Append($"attach {Process.GetCurrentProcess().Id}");
      commandLine.Append(" --service-input=stdin --service-output=On");
      commandLine.Append(" --collect-data-from-start=Off");
      commandLine.Append(" --use-api");

      if (config.LogFile != null)
        commandLine.Append($" --log-file=\"{config.LogFile}\" --debug-logging");

      if (config.IsOverwriteSnapshot)
        commandLine.Append(" --overwrite");

      if (config.SnapshotDir != null)
        commandLine.Append($" --save-to={config.SnapshotDir}");

      if (config.SnapshotFile != null)
        commandLine.Append($" --save-to={config.SnapshotFile}");

      Trace.Info("DotTrace.RunConsole:\n  runner = `{0}`\n  arguments = `{1}`", runnerPath, commandLine);

      var collectedSnapshots = new CollectedSnapshots();
      var consoleProfiler = new ConsoleProfiler(
        runnerPath,
        commandLine.ToString(),
        MessageServicePrefix,
        CltPresentableName,
        () => (MeasureProfiler.GetFeatures() & MeasureFeatures.Ready) == MeasureFeatures.Ready,
        collectedSnapshots);

      Trace.Verbose("DotTrace.RunConsole: Runner started.");

      return new Session(consoleProfiler, collectedSnapshots);
    }

    private sealed class Prerequisite : PrerequisiteBase
    {
      public Prerequisite() : base("dotTrace", NupkgVersion)
      {
      }

      protected override string GetRunnerName()
      {
        switch (Helper.Platform)
        {
        case PlatformId.Linux:
        case PlatformId.MacOs: return "dotTrace.sh";
        case PlatformId.Windows: return "ConsoleProfiler.exe";
        default: throw new ArgumentOutOfRangeException();
        }
      }

      protected override string GetPackageName()
      {
        return "JetBrains.dotTrace.CommandLineTools";
      }

      protected override long GetEstimatedSize()
      {
        return 30 * 1024 * 1024;
      }
    }

    private sealed class Session
    {
      private readonly CollectedSnapshots _snapshots;
      private readonly ConsoleProfiler _consoleProfiler;

      public Session(ConsoleProfiler consoleProfiler, CollectedSnapshots snapshots)
      {
        _consoleProfiler = consoleProfiler;
        _snapshots = snapshots;
      }

      [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
      public Session Detach()
      {
        MeasureProfiler.Detach();
        return this;
      }

      [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
      public Session SaveData()
      {
        MeasureProfiler.SaveData();
        _consoleProfiler.AwaitResponse("(?:snapshot-saved|get-snapshot-error)", -1);
        return this;
      }

      [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
      public Session DropData()
      {
        MeasureProfiler.DropData();
        return this;
      }

      [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
      public Session StartCollectingData()
      {
        MeasureProfiler.StartCollectingData();
        return this;
      }

      [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
      public Session StopCollectingData()
      {
        MeasureProfiler.StopCollectingData();
        return this;
      }

      public Session AwaitConnected(int milliseconds)
      {
        _consoleProfiler.AwaitResponse("ready", -1);
        _consoleProfiler.AwaitConnected(milliseconds);
        return this;
      }

      public Session AwaitFinished(int milliseconds)
      {
        _consoleProfiler.AwaitFinished(milliseconds);
        return this;
      }

      public string[] GetCollectedSnapshotsIndexFiles() => _snapshots.GetCollectedIndexFiles();
    }

    private sealed class CollectedSnapshots : IResponseCommandProcessor
    {
      private readonly Regex _savedCommandRegex = new Regex("\x0022filename\x22\\s*:\\s*\x22(.*)\x22");

      private readonly IList<string> _snapshotIndexFiles = new List<string>();

      public void ProcessCommand(string command, string args)
      {
        switch (command)
        {
        case "snapshot-saved":
          var m = _savedCommandRegex.Match(args);
          var rawResult = m.Groups[1].Value;
          var unescapedResult = Regex.Unescape(rawResult);
          if (m.Success)
            lock (_snapshotIndexFiles)
              _snapshotIndexFiles.Add(unescapedResult);
          break;
        }
      }

      public string[] GetCollectedIndexFiles()
      {
        lock (_snapshotIndexFiles)
          return _snapshotIndexFiles.ToArray();
      }
    }
  }
}