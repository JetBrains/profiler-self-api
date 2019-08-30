using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace JetBrains.Profiler.SelfApi.Impl
{
    internal abstract class PrerequisiteBase
    {
        public readonly string Name;
        public readonly string SemanticVersion;

        protected PrerequisiteBase(string name, string semanticVersion)
        {
            Name = name;
            SemanticVersion = semanticVersion;
        }

        public async Task DownloadAsync(
            Uri nugetUrl,
            NuGetApi nugetApi,
            string downloadTo,
            IProgress<double> progress,
            CancellationToken cancellationToken)
        {
            const double downloadWeigth = 0.8;
            const double unzipWeigth = 0.2;

            downloadTo = string.IsNullOrEmpty(downloadTo)
                ? GetAppLocalPath()
                : Path.Combine(downloadTo, $"{Name}.{SemanticVersion}");

            Trace.Info("Prerequisite.Download: targetPath = `{0}`", downloadTo);
            Directory.CreateDirectory(downloadTo);

            var nupkgName = GetPackageName();
            var nupkgPath = Path.Combine(downloadTo, $"{nupkgName}.{SemanticVersion}.nupkg");
            
            using (var http = new HttpClient())
            {
                Trace.Verbose("Prerequisite.Download: Requesting...");
                var content = await http
                    .GetNupkgContentAsync(nugetUrl, nugetApi, nupkgName, SemanticVersion, cancellationToken)
                    .ConfigureAwait(false);
                
                Trace.Verbose("Prerequisite.Download: Saving...");
                using (var input = await content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var output = File.Create(nupkgPath))
                {
                    Copy(
                        input, 
                        output, 
                        content.Headers.ContentLength ?? GetEstimatedSize(), 
                        new SubProgress(progress, 0, downloadWeigth)
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
                var subStart = downloadWeigth * 100;
                foreach (var entry in toolsEntries)
                {
                    var subStep = entry.Length * unzipWeigth / totalLength;
                    var dstPath = Path.Combine(downloadTo, entry.FullName.Substring(toolsPrefix.Length));
                    
                    using (var input = entry.Open())
                    using (var output = File.Create(dstPath))
                    {
                        Trace.Verbose("Prerequisite.Download:   `{0}` -> `{1}`", entry.FullName, dstPath);
                        Copy(
                            input,
                            output, 
                            entry.Length, 
                            new SubProgress(progress, subStart, subStep)
                        );
                    }

                    subStart += subStep;
                }
            }

            Trace.Verbose("Prerequisite.Download: Cleaning up...");
            File.Delete(nupkgPath);
        }

        public bool TryGetRunner(string prerequisitePath, out string runnerPath)
        {
            var runnerName = GetRunnerName();
            Trace.Verbose("Prerequisite.TryGetRunner: `{0}`", runnerName);

            if (!string.IsNullOrEmpty(prerequisitePath))
            {
                runnerPath = Path.Combine(prerequisitePath, $"{Name}.{SemanticVersion}", runnerName);
                Trace.Verbose("Prerequisite.TryGetRunner: External path provided, looking at `{0}`", runnerPath);
                return File.Exists(runnerPath);
            }

            runnerPath = Path.Combine(GetNearbyPath(), runnerName);
            Trace.Verbose("Prerequisite.TryGetRunner: Looking at `{0}`", runnerPath);
            if (File.Exists(runnerPath))
                return true;

            runnerPath = Path.Combine(GetAppLocalPath(), runnerName);
            Trace.Verbose("Prerequisite.TryGetRunner: Looking at `{0}`", runnerPath);
            if (File.Exists(runnerPath))
                return true;

            Trace.Verbose("Prerequisite.TryGetRunner: No runner found.");
            return false;
        }
        
        protected abstract string GetRunnerName();

        protected abstract string GetPackageName();

        protected abstract long GetEstimatedSize();

        private string GetAppLocalPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                $"JetBrains/Profiler/SelfApi/{Name}.{SemanticVersion}"
            );
        }
        
        private static string GetNearbyPath()
        {
            var assembly = Assembly.GetExecutingAssembly();
            return string.IsNullOrEmpty(assembly.Location) ? string.Empty : Path.GetDirectoryName(assembly.Location);
        }
        
        private static void Copy(
            Stream @from,
            Stream to,
            long length,
            IProgress<double> progress)
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
                var delta = newPercents - percents;
                if (delta < 1.0)
                    continue;

                progress.Report(delta);
                percents = newPercents;
            }
        }
    }
}