using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace JetBrains.Profiler.SelfApi.Impl
{
  internal sealed class ConsoleToolRunner
  {
    private readonly PrerequisiteBase _prerequisite;
    private Task _prerequisiteDownloadTask;
    private string _prerequisitePath;

    public ConsoleToolRunner(PrerequisiteBase prerequisite)
    {
      _prerequisite = prerequisite;
    }

    public Task EnsurePrerequisiteAsync(
      CancellationToken cancellationToken,
      IProgress<double> progress = null,
      Uri nugetUrl = null,
      NuGetApi nugetApi = NuGetApi.V3,
      string downloadTo = null)
    {
      var name = _prerequisite.Name;
      if (_prerequisiteDownloadTask != null && !_prerequisiteDownloadTask.IsCompleted)
      {
        Trace.Verbose($"EnsurePrerequisite for {name}: Task already running.");
        return _prerequisiteDownloadTask;
      }

      _prerequisiteDownloadTask = null;
      _prerequisitePath = downloadTo;

      if (_prerequisite.TryGetRunner(downloadTo, out _))
      {
        Trace.Verbose($"EnsurePrerequisite for {name}: Runner found, no async task needed.");
        return _prerequisiteDownloadTask = Task.FromResult(Missing.Value);
      }

      if (nugetUrl == null)
        nugetUrl = NuGet.GetDefaultUrl(nugetApi);

      Trace.Verbose($"EnsurePrerequisite for {name}: Runner not found, starting download...");
      return _prerequisiteDownloadTask = _prerequisite
        .DownloadAsync(nugetUrl, nugetApi, downloadTo, progress, cancellationToken);
    }

    public void AssertIfReady()
    {
      if (_prerequisiteDownloadTask == null || !_prerequisiteDownloadTask.Wait(40))
        throw new InvalidOperationException("The prerequisite isn't ready: forgot to call EnsurePrerequisiteAsync()?");
    }

    public string GetRunner()
    {
      if (!_prerequisite.TryGetRunner(_prerequisitePath, out var runnerPath))
        throw new InvalidOperationException($"Something went wrong: the {_prerequisite.Name} console profiler not found.");
      return runnerPath;
    }
  }
}