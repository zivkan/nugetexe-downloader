using System;
using System.IO;

namespace zivkan.nugetexe.downloader
{
    internal class DownloadArgs
    {
        public DownloadArgs(DirectoryInfo destination, string url)
        {
            Destination = destination ?? throw new ArgumentNullException(nameof(destination));
            Url = string.IsNullOrEmpty(url) ? Program.DefaultUrl : url;
        }

        public DirectoryInfo Destination { get; set; }

        public bool PreRelease { get; set; }

        public string Url { get; set; }

        public bool DryRun { get; set; }
    }
}
