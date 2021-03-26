using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.Serialization;

namespace YoutubeDownloader
{
    [Serializable]
    public class Config
    {
        private static readonly string DefaultDownloadPath = (string?)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", "{374DE290-123F-4565-9164-39C4925E467B}", null) ?? Directory.GetCurrentDirectory();

        public string DownloadPath { get; set; }
        public bool TopMost { get; set; }

        public Config()
        {
            DownloadPath = DefaultDownloadPath;
            TopMost = true;
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            if (!Directory.Exists(DownloadPath))
                DownloadPath = DefaultDownloadPath;
        }
    }
}