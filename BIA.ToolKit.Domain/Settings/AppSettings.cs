namespace BIA.ToolKit.Domain.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public static class AppSettings
    {
        // The root folder of the application
        public static string AppFolderPath { get; set; }

        public static string TmpFolderPath { get; set; }
    }
}
