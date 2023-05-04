using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JetBrains.HabitatDetector;

namespace JetBrains.Profiler.SelfApi.Impl
{
  internal abstract class PrerequisiteBase
  {
    public readonly string Name;
    public readonly NuGet.SemanticVersion SemanticVersion;

    private Task _downloadTask;
    private string _downloadTo;

    protected PrerequisiteBase(string name, NuGet.SemanticVersion semanticVersion)
    {
      Name = name;
      SemanticVersion = semanticVersion;
    }

    public Task DownloadAsync(
      Uri nugetUrl,
      NuGetApi nugetApi,
      string downloadTo,
      IProgress<double> progress,
      CancellationToken cancellationToken)
    {
      if (_downloadTask != null && !_downloadTask.IsCompleted)
      {
        Trace.Verbose($"Prerequisite[{Name}].DownloadAsync: Task already running.");
        return _downloadTask;
      }

      _downloadTask = null;
      _downloadTo = downloadTo;

      if (TryGetRunner(downloadTo, out _))
      {
        Trace.Verbose($"Prerequisite[{Name}].DownloadAsync: Runner found, no async task needed.");
        return _downloadTask = Task.FromResult(Missing.Value);
      }

      if (nugetUrl == null)
        nugetUrl = NuGet.GetDefaultUrl(nugetApi);

      Trace.Verbose($"Prerequisite[{Name}].DownloadAsync: Runner not found, starting download...");
      return _downloadTask = DoDownloadAsync(nugetUrl, nugetApi, downloadTo, progress, cancellationToken);
    }

    public void VerifyReady()
    {
      if (_downloadTask == null || !_downloadTask.Wait(40))
        throw new InvalidOperationException("The prerequisite isn't ready, call DownloadAsync() first.");
    }

    public string GetRunnerPath()
    {
      VerifyReady();

      if (!TryGetRunner(_downloadTo, out var runnerPath) && !TryGetRunnerEx(_downloadTo, out runnerPath))
        throw new InvalidOperationException($"Something went wrong: the {Name} console profiler not found.");

      return runnerPath;
    }

    private async Task DoDownloadAsync(
      Uri nugetUrl,
      NuGetApi nugetApi,
      string downloadTo,
      IProgress<double> progress,
      CancellationToken cancellationToken)
    {
      const double downloadWeight = 0.8;
      const double unzipWeight = 1.0 - downloadWeight;

      try
      {
        if (string.IsNullOrEmpty(downloadTo))
          downloadTo = GetDefaultDownloadPath();

        Trace.Info("Prerequisite.Download: targetPath = `{0}`", downloadTo);
        Directory.CreateDirectory(downloadTo);

        var nupkgName = GetPackageName() + "." + HabitatInfo.OSRuntimeIdString;
        string nupkgFolder, nupkgPath, readyMarker;

        var downloadProgress = new SubProgress(progress, 0, downloadWeight);
        var unzipProgress = new SubProgress(progress, downloadWeight * 100.0, unzipWeight);

        using (var http = new HttpClient())
        {
          Trace.Verbose("Prerequisite.Download: Requesting...");
          var content = await http
            .GetNupkgContentAsync(nugetUrl, nugetApi, nupkgName, SemanticVersion, cancellationToken)
            .ConfigureAwait(false);

          var latestVersion = content.Headers.GetValues("Version").Single();
          nupkgFolder = Path.Combine(downloadTo, Name, latestVersion);
          readyMarker = Path.Combine(nupkgFolder, ".ready");

          if (File.Exists(readyMarker))
          {
            Trace.Verbose("Prerequisite.Download: Package version `{0}` already downloaded.", latestVersion);
            return;
          }

          Directory.CreateDirectory(nupkgFolder);
          nupkgPath = Path.Combine(nupkgFolder, $"{nupkgName}.{latestVersion}.nupkg");

          Trace.Verbose("Prerequisite.Download: Saving...");
          using (var input = await content.ReadAsStreamAsync().ConfigureAwait(false))
          using (var output = File.Create(nupkgPath))
          {
            CopyStream(
              input,
              output,
              content.Headers.ContentLength ?? GetEstimatedSize(),
              downloadProgress,
              cancellationToken
              );
          }
        }

        const string toolsPrefix = "tools/";

        Trace.Verbose("Prerequisite.Download: Reading .nupkg ...");
        using (var zipInput = File.OpenRead(nupkgPath))
        using (var nupkg = new ZipArchive(zipInput))
        {
          var toolsEntries = nupkg.Entries
            .Where(x => x.FullName.StartsWith(toolsPrefix, StringComparison.OrdinalIgnoreCase))
            .ToArray();

          if (toolsEntries.Length == 0)
            throw new InvalidOperationException(
              "Something went wrong: unable to find /tools folder inside NuGet package.");

          var totalLength = toolsEntries.Sum(x => x.Length);
          Trace.Verbose(
            "Prerequisite.Download: Found {0} entries of total length {1} bytes.",
            toolsEntries.Length,
            totalLength
            );

          Trace.Verbose("Prerequisite.Download: Unpacking...");
          var totalUnzippedSize = 0L;
          foreach (var entry in toolsEntries)
          {
            var dstPath = Path.Combine(nupkgFolder, entry.FullName.Substring(toolsPrefix.Length));

            Directory.CreateDirectory(Path.GetDirectoryName(dstPath));

            using (var input = entry.Open())
            using (var output = File.Create(dstPath))
            {
              Trace.Verbose("Prerequisite.Download:   `{0}` -> `{1}`", entry.FullName, dstPath);
              CopyStream(
                input,
                output,
                entry.Length,
                new SubProgress(unzipProgress, 100.0 * totalUnzippedSize / totalLength, 1.0 * entry.Length / totalLength),
                cancellationToken
                );
            }

            if (HabitatInfo.Platform != JetPlatform.Windows)
            {
              Trace.Verbose("Setting up executable bit for {0}...", dstPath);
              Helper.ChModExecutable(dstPath);
            }

            totalUnzippedSize += entry.Length;
          }
        }

        Trace.Verbose("Prerequisite.Download: Cleaning up...");
        File.Delete(nupkgPath);

        new FileStream(readyMarker, FileMode.Create).Dispose();
        Trace.Verbose("Prerequisite.Download: Confirmed everything ready to use.");
      }
      catch (HttpRequestException e)
      {
        throw new Exception(
          $"Failed to download prerequisite package. Please, check the NuGet URL and Internet connection.\n[{nugetUrl}]",
          e
          );
      }
      catch (IOException e)
      {
        ThrowOperationCanceledExceptionIfNeeded(e);
        throw new Exception(
          $"Failed to save/unpack prerequisite package. Please, check the path and available disk space.\n[{downloadTo}]",
          e
          );
      }
    }

    private static void ThrowOperationCanceledExceptionIfNeeded(Exception e, int maxExceptionDepth = 5)
    {
      for (var i = 0; i < maxExceptionDepth && e != null; i++)
      {
        if(e is WebException webException)
          if (webException.Status == WebExceptionStatus.RequestCanceled)
            throw new OperationCanceledException("Failed to download prerequisite package. Operation was cancelled.", e);
        e = e.InnerException;
      }
    }

    private bool TryGetRunner(string prerequisitePath, out string runnerPath)
    {
      var runnerName = GetRunnerName();
      Trace.Verbose("Prerequisite.TryGetRunner: `{0}`", runnerName);

      if (!string.IsNullOrEmpty(prerequisitePath))
      {
        runnerPath = Path.Combine(prerequisitePath, runnerName);
        Trace.Verbose("Prerequisite.TryGetRunner: External path provided, looking at `{0}`", runnerPath);
        return File.Exists(runnerPath);
      }

      runnerPath = Path.Combine(GetNearbyPath(), runnerName);
      Trace.Verbose("Prerequisite.TryGetRunner: Looking at `{0}`", runnerPath);
      if (File.Exists(runnerPath))
        return true;

      Trace.Verbose("Prerequisite.TryGetRunner: No runner found.");
      return false;
    }

    private bool TryGetRunnerEx(string prerequisitePath, out string runnerPath)
    {
      runnerPath = null;
      var runnerName = GetRunnerName();
      Trace.Verbose("Prerequisite.TryGetRunnerEx: `{0}`", runnerName);

      if (string.IsNullOrEmpty(prerequisitePath))
        prerequisitePath = GetDefaultDownloadPath();

      prerequisitePath = Path.Combine(prerequisitePath, Name);

      Trace.Verbose("Prerequisite.TryGetRunnerEx: Looking for latest version at `{0}`...", prerequisitePath);

      if (!Directory.Exists(prerequisitePath))
      {
        Trace.Verbose("Prerequisite.TryGetRunnerEx: No runner found.");
        return false;
      }

      var packageVersion2 = SemanticVersion.Version2;
      string latestOriginal = null; // the latest found version including build meta-info
      NuGet.SemanticVersion latest = null; // the latest parsed version w/o meta-info
      foreach (var versionFolder in Directory.GetDirectories(prerequisitePath))
      {
        var sVer = Path.GetFileName(versionFolder);
        Trace.Verbose("Prerequisite.TryGetRunnerEx:   {0}", sVer);

        var ver = NuGet.SemanticVersion.TryParse(sVer);
        if (ver == null || ver.Version2 != packageVersion2)
          continue;

        if (latest == null || latest.CompareTo(ver) <= 0)
        {
          latestOriginal = sVer;
          latest = ver;
        }
      }

      if (latestOriginal != null)
      {
        runnerPath = Path.Combine(prerequisitePath, latestOriginal, runnerName);
        Trace.Verbose("Prerequisite.TryGetRunnerEx: Checking `{0}`...", runnerPath);
        return File.Exists(runnerPath);
      }

      Trace.Verbose("Prerequisite.TryGetRunnerEx: No runner found.");
      return false;
    }

    protected abstract string GetRunnerName();

    protected abstract string GetPackageName();

    protected abstract long GetEstimatedSize();

    private static string GetDefaultDownloadPath()
    {
      return Path.Combine(Path.GetTempPath(), "JetBrains", "Profiler", "SelfApi");
    }

    private static string GetNearbyPath()
    {
      var assembly = Assembly.GetExecutingAssembly();
      return string.IsNullOrEmpty(assembly.Location) ? string.Empty : Path.GetDirectoryName(assembly.Location);
    }

    private static void CopyStream(
      [NotNull] Stream from,
      [NotNull] Stream to,
      long estimatedLength,
      [NotNull] IProgress<double> progress,
      CancellationToken cancellationToken)
    {
      var buffer = new byte[64 * 1024];
      var bytesCopied = 0L;

      while (true)
      {
        var bytesRead = from.Read(buffer, 0, buffer.Length);
        if (bytesRead == 0)
          break;

        to.Write(buffer, 0, bytesRead);
        bytesCopied += bytesRead;

        var percents = bytesCopied < estimatedLength ? bytesCopied * 100.0 / estimatedLength : 100;
        progress.Report(percents);
        
        cancellationToken.ThrowIfCancellationRequested();
      }
    }
  }
}