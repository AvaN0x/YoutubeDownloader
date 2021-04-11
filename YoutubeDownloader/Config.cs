using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Diagnostics;
using System.Reflection;

namespace YoutubeDownloader
{
    public enum Extension
    {
        mp3,
        mp4
    }

    public class Config
    {
        public static bool Portable
        {
            get
            {
                var assembly = Assembly.GetEntryAssembly();
                string? assemblyPath = assembly is not null ? Path.GetDirectoryName(assembly.Location) : null;
                return assemblyPath is not null && Directory.Exists(Path.Combine(assemblyPath, "config"));
            }
        }

        public static string ConfigDir
        {
            get
            {
                var assembly = Assembly.GetEntryAssembly();
                string? assemblyPath = assembly is not null ? Path.GetDirectoryName(assembly.Location) : null;
                return Portable && assemblyPath is not null ? Path.Combine(assemblyPath, "config") : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "YoutubeDownloader");
            }
        }

        private static readonly string DefaultDownloadPath = (string?)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", "{374DE290-123F-4565-9164-39C4925E467B}", null) ?? Assembly.GetEntryAssembly()?.Location ?? "";

        [JsonProperty("downloadPath")]
        public string DownloadPath { get; set; }

        [JsonProperty("topMost")]
        public bool TopMost { get; set; }

        [JsonProperty("extension")]
        [JsonConverter(typeof(StringEnumConverter))]
        public Extension Extension { get; set; }

        public Config()
        {
            DownloadPath = DefaultDownloadPath;
            TopMost = true;
            Extension = Extension.mp3;
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            if (!Directory.Exists(DownloadPath))
                DownloadPath = DefaultDownloadPath;
        }
    }
}