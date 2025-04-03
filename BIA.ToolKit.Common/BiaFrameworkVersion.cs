namespace BIA.ToolKit.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class BiaFrameworkVersion
    {
        public int Major { get; }
        public int? Minor { get; }
        public int? Build { get; }

        public BiaFrameworkVersion(string pattern)
        {
            var parts = pattern.Split('.');
            if (parts.Length < 1 || parts.Length > 3)
                throw new ArgumentException("Invalid version format");

            Major = int.Parse(parts[0]);
            Minor = parts.Length > 1 && parts[1] != "*" ? int.Parse(parts[1]) : null;
            Build = parts.Length > 2 && parts[2] != "*" ? int.Parse(parts[2]) : null;
        }

        public bool Matches(Version version)
        {
            if (version.Major != Major)
                return false;
            if (Minor.HasValue && version.Minor != Minor.Value)
                return false;
            if (Build.HasValue && version.Build != Build.Value)
                return false;
            return true;
        }
    }
}
