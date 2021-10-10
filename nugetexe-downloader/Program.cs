using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using zivkan.nugetexe.downloader.Model;

namespace zivkan.nugetexe.downloader
{
    internal class Program
    {
        internal static readonly string DefaultUrl = "https://dist.nuget.org/tools.json";

        public static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("Download and manage versions of NuGet.exe");

            var destinationOption = new Option<DirectoryInfo>(new[] { "--destination", "-d" })
            {
                Description = "Directory NuGet.exe should be written to",
                Arity = ArgumentArity.ExactlyOne,IsRequired = true
            };
            rootCommand.AddOption(destinationOption);

            var prereleaseOption = new Option<bool>("--prerelease")
            {
                Description = "Include prerelease versions"
            };
            rootCommand.AddOption(prereleaseOption);

            var urlOption = new Option<string>("--url")
            {
                Description = "URL for tools.json. Defaults to " + DefaultUrl
            };
            rootCommand.AddOption(urlOption);

            var dryRunOption = new Option<bool>("--dry-run")
            {
                Description = "Do not download NuGet.exe, only output what would be performed"
            };
            rootCommand.Add(dryRunOption);

            rootCommand.Handler = CommandHandler.Create<DownloadArgs>(RunAsync);

            return await rootCommand.InvokeAsync(args);
        }

        private static async Task<int> RunAsync(DownloadArgs args)
        {
            using (HttpClient httpClient = new())
            {
                httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("zivkan.nugetexe-downloader", GetProductVersion()));
                httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(+https://github.com/zivkan/nugetexe-downloader)"));
                Console.WriteLine(httpClient.DefaultRequestHeaders.UserAgent.ToString());

                HttpResponseMessage response;
                string json;
                using (response = await httpClient.GetAsync(args.Url, HttpCompletionOption.ResponseContentRead))
                {
                    response.EnsureSuccessStatusCode();

                    json = await response.Content.ReadAsStringAsync();
                }
                var tools = JsonSerializer.Deserialize<Dictionary<string, List<Tool>>>(json);

                if (!tools.TryGetValue("nuget.exe", out List<Tool>? versions))
                {
                    Console.WriteLine("Did not find nuget.exe in " + args.Url);

                    return -1;
                }

                var workingVersions =
                    versions.Select(tool => new
                    {
                        Version = NuGetVersion.Parse(tool.version),
                        Url = tool.url
                    });

                if (!args.PreRelease)
                {
                    workingVersions = workingVersions.Where(tool => !tool.Version.IsPrerelease);
                }

                var sortedVersions = 
                    workingVersions
                    .OrderBy(tool => tool.Version)
                    .ToList();

                if (sortedVersions.Count == 0)
                {
                    Console.WriteLine("tools.json did not contain any versions of nuget.exe");
                    return 0;
                }

                var destinationFullPath = args.Destination.FullName;
                Console.WriteLine($"Directory: " + destinationFullPath);

                if (!args.Destination.Exists)
                {
                    try
                    {
                        args.Destination.Create();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Unable to create directory '{args.Destination}': {ex.Message}");
                        return -2;
                    }
                }

                var downloadPath = Path.Combine(destinationFullPath, "download.tmp");

                foreach (var version in sortedVersions)
                {
                    var versionString = version.Version.ToNormalizedString().ToLowerInvariant();
                    var localFullPath = Path.Combine(destinationFullPath, $"NuGet.{versionString}.exe");
                    if (!File.Exists(localFullPath))
                    {
                        Console.WriteLine($"Downloading NuGet.exe {versionString}");
                        using (var downloadFile = File.Open(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        using (response = await httpClient.GetAsync(version.Url, HttpCompletionOption.ResponseHeadersRead))
                        {
                            response.EnsureSuccessStatusCode();

                            using (var downloadStream = await response.Content.ReadAsStreamAsync())
                            {
                                await downloadStream.CopyToAsync(downloadFile);
                            }
                        }

                        File.Move(downloadPath, localFullPath);
                    }
                }
            }

            return 0;
        }

        public static string GetProductVersion()
        {
            var informationalVersion =
                typeof(Program)
                .Assembly
                .GetCustomAttributes(true)
                .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
                .Single();

            return informationalVersion.InformationalVersion;
        }
    }
}
