using System;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;

namespace JetBrains.Profiler.SelfApi.Impl
{
    /// <summary>
    /// The minimal stuff to support NuGet package downloading for API v2/v3
    /// </summary>
    internal static class NuGet
    {
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
            string packageVersion,
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

        public static class V2
        {
            public static readonly Uri NugetOrgUrl = new Uri("https://www.nuget.org/api/v2/package");
            
            public static async Task<HttpContent> GetNupkgContentAsync(
                HttpClient http,
                Uri nugetUrl,
                string packageId,
                string packageVersion,
                CancellationToken cancellationToken)
            {
                var packageUrl = nugetUrl.Combine($"{packageId}/{packageVersion}");

                var response = await http
                    .GetAsync(packageUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);
                
                response.EnsureSuccessStatusCode();

                return response.Content;
            }
        }

        public static class V3
        {
            public static readonly Uri NugetOrgUrl = new Uri("https://api.nuget.org/v3/index.json");
            
            public static async Task<HttpContent> GetNupkgContentAsync(
                HttpClient http,
                Uri indexUrl, 
                string packageId, 
                string packageVersion, 
                CancellationToken cancellationToken)
            {
                packageId = packageId.ToLowerInvariant();
                packageVersion = packageVersion.ToLowerInvariant();
                
                var serviceIndex = await GetIndexAsync(http, indexUrl, cancellationToken).ConfigureAwait(false);
                var packageBaseUrl = serviceIndex.GetResourceUrl("PackageBaseAddress/3.0.0");

                var packageUrl = packageBaseUrl
                    .Combine($"{packageId}/{packageVersion}/{packageId}.{packageVersion}.nupkg");

                var response = await http
                    .GetAsync(packageUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);
                
                response.EnsureSuccessStatusCode();
                
                return response.Content;
            }

            private static async Task<ServiceIndex> GetIndexAsync(
                HttpClient http, 
                Uri indexUrl,
                CancellationToken cancellationToken)
            {
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

#pragma warning disable 649
            [DataContract]
            private sealed class ServiceIndex
            {
                [DataMember(Name = "version")]
                public string Version;

                [DataMember(Name = "resources")]
                public Resource[] Resources;
                
                public Uri GetResourceUrl(string resourceType)
                {
                    var resource = Resources.FirstOrDefault(x => x.Type == resourceType);
                    if (resource == null || string.IsNullOrEmpty(resource.Id))
                        throw new InvalidOperationException(
                            $"Something went wrong: unable to find `{resourceType}`");

                    return new Uri(resource.Id);
                }
            }
            
            [DataContract]
            private sealed class Resource
            {
                [DataMember(Name = "@id")]
                public string Id;

                [DataMember(Name = "@type")]
                public string Type;
            }
#pragma warning restore 649
        }
    }
}