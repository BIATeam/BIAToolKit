namespace BIA.ToolKit.Application.Helper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;


    public delegate void CallBack(string value);

    internal static class SynchronizeSettings
    {
        public static Dictionary<string, List<CallBack>> dictCallBacks = [];

        public static void AddCallBack(string settingsName, CallBack myCallBack)
        {
            _ = new List<CallBack>();
            dictCallBacks.TryGetValue(settingsName, out List<CallBack> callBacks);
            if (callBacks == null)
            {
                callBacks = [];
                dictCallBacks.Add(settingsName, callBacks);
            }

            callBacks.Add(myCallBack);
        }

        public static void SettingChange(string settingsName, string value)
        {
            _ = new List<CallBack>();
            dictCallBacks.TryGetValue(settingsName, out List<CallBack> callBacks);
            if (callBacks != null)
            {
                foreach (CallBack callBack in callBacks)
                {
                    callBack(value);
                }
            }
        }

    }
}
