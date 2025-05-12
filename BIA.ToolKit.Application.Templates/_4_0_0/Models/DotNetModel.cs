namespace BIA.ToolKit.Application.Templates._4_0_0.Models
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class DotNetModel
    {
        public DotNetModel() { }

        private List<string> usingList = new List<string>();

        public void ClearUse()
        {
            usingList.Clear();
        }

        public void Use(string usingItem)
        {
            usingList.Add(usingItem);
        }

        public string WritteUsing()
        {
            StringBuilder sb = new StringBuilder();
            if (usingList.Count() > 0)
            {
                sb.Append("\r\n");
            }
            if (usingList.Where(s => s.StartsWith("System.") || s == "System").Count() > 0)
            {
                sb.Append(string.Join("\r\n", usingList.Where(s => s.StartsWith("System.") || s == "System").OrderBy(s => s).Select(s => "    using " + s + ";")));
                sb.Append("\r\n");
            }
            if (usingList.Where(s => !s.StartsWith("System.") && s != "System").Count() > 0)
            {
                sb.Append(string.Join("\r\n", usingList.Where(s => !s.StartsWith("System.") && s != "System").OrderBy(s => s).Select(s => "    using " + s + ";")));
                sb.Append("\r\n");
            }

            return sb.ToString();
        }
    }
}
