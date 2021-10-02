using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

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

            rootCommand.Handler = CommandHandler.Create<DownloadArgs>(DownloadAsync);

            return await rootCommand.InvokeAsync(args);
        }

        private static async Task<int> DownloadAsync(DownloadArgs args)
        {
            Console.WriteLine($"Destination = {args.Destination}");
            Console.WriteLine($"Prerelease = {args.PreRelease}");
            Console.WriteLine($"Url = {args.Url} ({args.Url?.Length})");

            return 0;
        }
    }
}
