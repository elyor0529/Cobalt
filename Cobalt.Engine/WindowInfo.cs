using System;
using System.Diagnostics;

namespace Cobalt.Engine
{
    public class WindowInfo
    {
        public string Title { get; set; }
        public string ProcessFilePath { get; set; }
        public string[] ProcessArgs { get; set; }
        public DateTimeOffset ActivatedTimestamp { get; set; }

        public FileVersionInfo ProcessFileInfo => FileVersionInfo.GetVersionInfo(ProcessFilePath);
    }
}
