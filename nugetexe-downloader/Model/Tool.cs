using System;

namespace zivkan.nugetexe.downloader.Model
{
    internal class Tool
    {
        public string? version { get; set; }

        public string? url { get; set; }

        public string? stage { get; set; }

        public DateTimeOffset? uploaded { get; set; }
    }
}
