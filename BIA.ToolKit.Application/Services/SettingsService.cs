namespace BIA.ToolKit.Application.Services
{
    using BIA.ToolKit.Application.Helper;
    using System.Configuration;

    public class SettingsService
    {
        private readonly IConsoleWriter consoleWriter;

        /// <summary>
        /// Constructor.
        /// </summary>
        public SettingsService(IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
        }

        /// <summary>
        /// Read App.config file and get value corresponding to "key".
        /// </summary>
        public string ReadSetting(string key)
        {
            string result = null;
            try
            {
                result = ConfigurationManager.AppSettings[key];
            }
            catch (ConfigurationErrorsException ex)
            {
                consoleWriter.AddMessageLine($"Error reading app settings (key={key}): {ex}", "Red");
            }
            return result;
        }
    }
}
