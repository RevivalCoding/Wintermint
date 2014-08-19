using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using System;
using WintermintClient.Data;

namespace WintermintClient.Util
{
    internal static class AppUserModelId
    {
        public static void SetWintermintProperties(IntPtr windowHandle)
        {
            LaunchData.Initialize();
            WindowPropertyValue[] windowPropertyValueArray = new WindowPropertyValue[3];
            WindowPropertyValue windowPropertyValue = new WindowPropertyValue()
            {
                Key = SystemProperties.System.AppUserModel.ID,
                Value = "astralfoxy.wintermint.client"
            };
            windowPropertyValueArray[0] = windowPropertyValue;
            WindowPropertyValue windowPropertyValue1 = new WindowPropertyValue()
            {
                Key = SystemProperties.System.AppUserModel.RelaunchCommand,
                Value = LaunchData.WintermintRootExecutable
            };
            windowPropertyValueArray[1] = windowPropertyValue1;
            WindowPropertyValue windowPropertyValue2 = new WindowPropertyValue()
            {
                Key = SystemProperties.System.AppUserModel.RelaunchDisplayNameResource,
                Value = "Wintermint"
            };
            windowPropertyValueArray[2] = windowPropertyValue2;
            WindowProperties.SetWindowProperties(windowHandle, windowPropertyValueArray);
        }
    }
}