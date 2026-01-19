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
        public static Dictionary<string,List<CallBack>> dictCallBacks = new Dictionary<string, List<CallBack>> ();

        public static void AddCallBack(string settingsName, CallBack myCallBack)
        {
            List<CallBack> callBacks = new List<CallBack>();
            dictCallBacks.TryGetValue(settingsName, out callBacks);
            if (callBacks == null)
            {
                callBacks = new List<CallBack>();
                dictCallBacks.Add(settingsName, callBacks);
            }

            callBacks.Add(myCallBack);
        }

        public static void SettingChange(string settingsName, string value)
        {
            List<CallBack> callBacks = new List<CallBack>();
            dictCallBacks.TryGetValue(settingsName, out callBacks);
            if (callBacks != null)
            { 
                foreach (var callBack in callBacks)
                {
                    callBack(value);
                }
            }
        }

    }
}
