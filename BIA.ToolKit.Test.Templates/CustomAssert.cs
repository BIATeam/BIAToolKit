namespace BIA.ToolKit.Test.Templates
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal static class CustomAssert
    {
        public static void FilesEquals(string expectedFilePath, string actualFilePath)
        {
            var expectedLines = File.ReadAllLines(expectedFilePath);
            var actualLines = File.ReadAllLines(actualFilePath);

            var differences = new List<string>();

            int maxLines = Math.Max(expectedLines.Length, actualLines.Length);
            for (int i = 0; i < maxLines; i++)
            {
                string expectedLine = i < expectedLines.Length ? expectedLines[i] : "<no line>";
                string actualLine = i < actualLines.Length ? actualLines[i] : "<no line>";

                if (!string.Equals(expectedLine, actualLine, StringComparison.Ordinal))
                {
                    differences.Add($"Line {i + 1}\n-> Expected: {expectedLine}\n-> Actual  : {actualLine}");
                }
            }

            if (differences.Count != 0)
            {
                var builder = new StringBuilder();
                builder.AppendLine($"File : {Path.GetFileName(expectedFilePath)}");
                builder.AppendLine($"Compare : kdiff3.exe {expectedFilePath} {actualFilePath}");
                builder.AppendLine();
                builder.AppendLine(string.Join("\n\n", differences));
                throw new FilesEqualsException(builder.ToString());
            }
        }
    }
}
