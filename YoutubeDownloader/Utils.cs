using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoutubeDownloader
{
    public class Utils
    {
        public static string RemoveInvalidChars(string filename)
        {
            return string.Concat(filename.Split(System.IO.Path.GetInvalidFileNameChars()));
        }
    }
}