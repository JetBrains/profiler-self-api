using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace JetBrains.Profiler.SelfApi.Impl
{
  /// <summary>
  /// The minimal stuff to support NuGet package downloading for API v2/v3
  /// </summary>
  internal static class NuGet
  {
    /// <summary>
    /// Simplified support for semantic version.
    /// </summary>
    public sealed class SemanticVersion : IComparable<SemanticVersion>
    {
      private readonly Version _version;

      private SemanticVersion(Version version, string prerelease, string build)
      {
        _version = version;
        Prerelease = prerelease;
        Build = build;
      }

      public SemanticVersion(int major, int minor)
      {
        _version = new Version(major, minor);
      }

      public Version Version2 => new Version(_version.Major, _version.Minor);

      public string Prerelease { get; }

      public string Build { get; }

      public int CompareTo(SemanticVersion other)
      {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;

        var cmp = _version.CompareTo(other._version);
        if (cmp != 0)
          return cmp;

        if (string.IsNullOrEmpty(Prerelease))
        {
          return string.IsNullOrEmpty(other.Prerelease)
            ? string.Compare(Build, other.Build, StringComparison.Ordinal)
            : 1;
        }

        if (string.IsNullOrEmpty(other.Prerelease))
          return -1;

        cmp = string.Compare(Prerelease, other.Prerelease, StringComparison.Ordinal);
        if (cmp != 0)
          return cmp;

        return string.Compare(Build, other.Build, StringComparison.Ordinal);
      }

      public override string ToString()
      {
        var sb = new StringBuilder(32);
        sb.Append(_version);
        if (!string.IsNullOrEmpty(Prerelease))
          sb.Append('-').Append(Prerelease);
        if (!string.IsNullOrEmpty(Build))
          sb.Append('+').Append(Build);
        return sb.ToString();
      }

      public static SemanticVersion TryParse(string value)
      {
        if (string.IsNullOrEmpty(value))
          return null;

        var prerelease = (string) null;
        var build = (string) null;

        var idx = value.LastIndexOf('+');
        if (idx >= 0)
        {
          build = value.Substring(idx + 1);
          value = value.Substring(0, idx);
        }

        idx = value.LastIndexOf('-');
        if (idx >= 0)
        {
          prerelease = value.Substring(idx + 1);
          value = value.Substring(0, idx);
        }

        if (!Version.TryParse(value, out var version))
          return null;

        return new SemanticVersion(version, prerelease, build);
      }
    }

    public static Uri GetDefaultUrl(NuGetApi nugetApi)
    {
      switch (nugetApi)
      {
      case NuGetApi.V2:
        return V2.NugetOrgUrl;

      case NuGetApi.V3:
        return V3.NugetOrgUrl;

      default:
        throw new NotSupportedException($"The NuGet API {nugetApi} is not supported.");
      }
    }

    public static Task<HttpContent> GetNupkgContentAsync(
      this HttpClient http,
      Uri nugetUrl,
      NuGetApi nugetApi,
      string packageId,
      SemanticVersion packageVersion,
      CancellationToken cancellationToken)
    {
      if (http == null) throw new ArgumentNullException(nameof(http));
      if (nugetUrl == null) throw new ArgumentNullException(nameof(nugetUrl));
      if (packageId == null) throw new ArgumentNullException(nameof(packageId));
      if (packageVersion == null) throw new ArgumentNullException(nameof(packageVersion));

      switch (nugetApi)
      {
      case NuGetApi.V2:
        return V2.GetNupkgContentAsync(http, nugetUrl, packageId, packageVersion, cancellationToken);

      case NuGetApi.V3:
        return V3.GetNupkgContentAsync(http, nugetUrl, packageId, packageVersion, cancellationToken);

      default:
        throw new NotSupportedException($"The NuGet API {nugetApi} is not supported.");
      }
    }

    public static Uri Combine(this Uri baseUrl, string subPath)
    {
      const char slash = '/';

      var builder = new UriBuilder(baseUrl);
      builder.Path = builder.Path.TrimEnd(slash) + slash + subPath.TrimStart(slash);

      return builder.Uri;
    }

    public static Uri Query(this Uri baseUrl, string query)
    {
      return new UriBuilder(baseUrl) {Query = query}.Uri;
    }

    public static class V2
    {
      public static readonly Uri NugetOrgUrl = new Uri("https://www.nuget.org/api/v2");

      public static async Task<HttpContent> GetNupkgContentAsync(
        HttpClient http,
        Uri nugetUrl,
        string packageId,
        SemanticVersion packageVersion,
        CancellationToken cancellationToken)
      {
        var indexUrl = nugetUrl.Combine("FindPackagesById()").Query($"id='{packageId}'");
        var feed = await GetFeedAsync(http, indexUrl, cancellationToken).ConfigureAwait(false);

        var latestEntry = GetLatestEntry(feed, packageVersion);
        var packageUrl = latestEntry.ContentSrc;

        Trace.Info("NuGet.V2.GetNupkgContent: {0}", packageUrl);
        var response = await http
          .GetAsync(packageUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
          .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        response.Content.Headers.Add("Version", latestEntry.Version.ToString());

        return response.Content;
      }

      private static async Task<Feed> GetFeedAsync(
        HttpClient http,
        Uri url,
        CancellationToken cancellationToken)
      {
        Trace.Info("NuGet.V2.GetFeed: {0}", url);
        using (var response = await http
          .GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
          .ConfigureAwait(false))
        {
          response.EnsureSuccessStatusCode();
          using (var xml = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
          {
            return Feed.FromStream(xml);
          }
        }
      }

      private static Entry GetLatestEntry(Feed feed, SemanticVersion packageVersion)
      {
        Entry latest = null;
        foreach (var entry in feed.Entries)
        {
          if (entry.Version == null || entry.Version.Version2 != packageVersion.Version2)
            continue;

          if (latest == null || latest.Version.CompareTo(entry.Version) <= 0)
            latest = entry;
        }

        if (latest == null)
          throw new InvalidOperationException(
            $"Something went wrong: unable to find the latest package of v{packageVersion}");

        return latest;
      }

      private sealed class Feed
      {
        private readonly XResponseNode _root;

        private Feed(XResponseNode root)
        {
          _root = root;
        }

        public IEnumerable<Entry> Entries => _root.Select("//a:feed/a:entry").Select(x => new Entry(x));

        public static Feed FromStream(Stream stream)
        {
          using (var reader = XmlReader.Create(new StreamReader(stream)))
          {
            var xdoc = XDocument.Load(reader);
            // ReSharper disable once AssignNullToNotNullAttribute
            return new Feed(new XResponseNode(xdoc.Root, new XmlNamespaceManager(reader.NameTable)));
          }
        }
      }

      private sealed class Entry
      {
        public Entry(XResponseNode node)
        {
          ContentSrc =
            node.ValueOf("a:content/@src")
            ?? throw new InvalidOperationException("Something went wrong: unable to find content/@src");

          Version = SemanticVersion.TryParse(node.ValueOf("m:properties/d:Version"));
        }

        public string ContentSrc { get; }
        public SemanticVersion Version { get; }
      }

      private sealed class XResponseNode
      {
        private readonly XElement _element;
        private readonly XmlNamespaceManager _ns;

        public XResponseNode(XElement element, XmlNamespaceManager ns)
        {
          _element = element;
          _ns = ns;

          _ns.AddNamespace("a", "http://www.w3.org/2005/Atom");
          _ns.AddNamespace("d", "http://schemas.microsoft.com/ado/2007/08/dataservices");
          _ns.AddNamespace("m", "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata");
        }

        public IEnumerable<XResponseNode> Select(string xpath)
        {
          return _element.XPathSelectElements(xpath, _ns).Select(x => new XResponseNode(x, _ns));
        }

        public string ValueOf(string xpath, string defaultValue = null)
        {
          var nav = _element.CreateNavigator(_ns.NameTable);
          var subNode = nav.SelectSingleNode(xpath, _ns);
          return subNode == null
            ? defaultValue
            : subNode.Value;
        }
      }
    }

    public static class V3
    {
      public static readonly Uri NugetOrgUrl = new Uri("https://api.nuget.org/v3/index.json");

      public static async Task<HttpContent> GetNupkgContentAsync(
        HttpClient http,
        Uri indexUrl,
        string packageId,
        SemanticVersion packageVersion,
        CancellationToken cancellationToken)
      {
        packageId = packageId.ToLowerInvariant();

        var serviceIndex = await GetIndexAsync(http, indexUrl, cancellationToken).ConfigureAwait(false);
        var packageBaseUrl = serviceIndex.GetResourceUrl("PackageBaseAddress/3.0.0");

        var packageIndexUrl = packageBaseUrl.Combine($"{packageId}/index.json");
        var packageIndex = await GetVersionsAsync(http, packageIndexUrl, cancellationToken).ConfigureAwait(false);
        var latestVersion = packageIndex.GetLatestVersion(packageVersion);

        var packageContentUrl = packageBaseUrl
          .Combine($"{packageId}/{latestVersion}/{packageId}.{latestVersion}.nupkg");

        Trace.Info("NuGet.V3.GetNupkgContent: {0}", packageContentUrl);
        var response = await http
          .GetAsync(packageContentUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
          .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        response.Content.Headers.Add("Version", latestVersion);

        return response.Content;
      }

      private static async Task<ServiceIndex> GetIndexAsync(
        HttpClient http,
        Uri indexUrl,
        CancellationToken cancellationToken)
      {
        Trace.Info("NuGet.V3.GetIndex: {0}", indexUrl);
        using (var response = await http
          .GetAsync(indexUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
          .ConfigureAwait(false))
        {
          response.EnsureSuccessStatusCode();
          using (var indexJson = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
          {
            return (ServiceIndex) new DataContractJsonSerializer(typeof(ServiceIndex))
              .ReadObject(indexJson);
          }
        }
      }

      private static async Task<PackageIndex> GetVersionsAsync(
        HttpClient http,
        Uri indexUrl,
        CancellationToken cancellationToken)
      {
        Trace.Info("NuGet.V3.GetVersions: {0}", indexUrl);
        using (var response = await http
          .GetAsync(indexUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
          .ConfigureAwait(false))
        {
          response.EnsureSuccessStatusCode();
          using (var indexJson = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
          {
            return (PackageIndex) new DataContractJsonSerializer(typeof(PackageIndex))
              .ReadObject(indexJson);
          }
        }
      }

#pragma warning disable 649
      [DataContract]
      private sealed class ServiceIndex
      {
        [DataMember(Name = "version")] public string Version;

        [DataMember(Name = "resources")] public Resource[] Resources;

        public Uri GetResourceUrl(string resourceType)
        {
          var resource = Resources.FirstOrDefault(x => x.Type == resourceType);
          if (resource == null || string.IsNullOrEmpty(resource.Id))
            throw new InvalidOperationException($"Something went wrong: unable to find `{resourceType}`");

          return new Uri(resource.Id);
        }
      }

      [DataContract]
      private sealed class Resource
      {
        [DataMember(Name = "@id")] public string Id;

        [DataMember(Name = "@type")] public string Type;
      }

      [DataContract]
      private sealed class PackageIndex
      {
        [DataMember(Name = "versions")] public string[] Versions;

        public string GetLatestVersion(SemanticVersion packageVersion)
        {
          string latestOriginal = null; // the latest found version including build meta-info
          SemanticVersion latest = null; // the latest parsed version w/o meta-info 
          foreach (var sVer in Versions)
          {
            var ver = SemanticVersion.TryParse(sVer);
            if (ver == null || ver.Version2 != packageVersion.Version2)
              continue;

            if (latest == null || latest.CompareTo(ver) <= 0)
            {
              latestOriginal = sVer;
              latest = ver;
            }
          }

          if (latestOriginal == null)
            throw new InvalidOperationException(
              $"Something went wrong: unable to find the latest package of v{packageVersion}");

          return latestOriginal;
        }
      }
#pragma warning restore 649
    }
  }
}