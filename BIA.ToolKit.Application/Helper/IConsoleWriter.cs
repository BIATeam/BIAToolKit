namespace BIA.ToolKit.Application.Helper
{
    public interface IConsoleWriter
    {
        void AddMessageLine(string message, string color = null, bool refreshimediate = true);
        void Clear();
        void CopyToClipboard();
    }
}
