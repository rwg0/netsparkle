using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace AppLimit.NetSparkle
{
    internal class NetSparkleDeviceInventory
    {
        public Boolean x64System { get; set; }
        public uint ProcessorSpeed { get; set; }
        public Int64 MemorySize { get; set; }
        public string OsVersion { get; set; }
        public int CPUCount { get; set; }

        private NetSparkleConfiguration _config;

        public NetSparkleDeviceInventory(NetSparkleConfiguration config)
        {
            _config = config;
        }

        public void CollectInventory()
        {
            // x64
            CollectProcessorBitnes();


            // cpu count
            CollectCPUCount();


            // windows
            CollectWindowsVersion();
        }

        public String BuildRequestUrl(String baseRequestUrl)
        {
            String retValue = baseRequestUrl;
            
            // x64 
            retValue += "cpu64bit=" + (x64System ? "1" : "0") + "&";

            // cpu speed
            retValue += "cpuFreqMHz=" + ProcessorSpeed + "&";

            // ram size
            retValue += "ramMB=" + MemorySize + "&";

            // Application name (as indicated by CFBundleName)
            retValue += "appName=" + _config.ApplicationName + "&";

            // Application version (as indicated by CFBundleVersion)
            retValue += "appVersion=" + _config.InstalledVersion + "&";

            // User’s preferred language
            retValue += "lang=" + Thread.CurrentThread.CurrentUICulture.ToString() + "&";

            // Windows version
            retValue += "osVersion=" + OsVersion + "&";

            // CPU type/subtype (see mach/machine.h for decoder information on this data)
            // ### TODO: cputype, cpusubtype ###

            // Mac model
            // ### TODO: model ###

            // Number of CPUs (or CPU cores, in the case of something like a Core Duo)
            // ### TODO: ncpu ###
            retValue += "ncpu=" + CPUCount + "&";

            // sanitize url
            retValue = retValue.TrimEnd('&');            

            // go ahead
            return retValue;
        }

        private void CollectWindowsVersion()
        {
            OperatingSystem osInfo = Environment.OSVersion;
            OsVersion = string.Format("{0}.{1}.{2}.{3}", osInfo.Version.Major, osInfo.Version.Minor, osInfo.Version.Build, osInfo.Version.Revision);
        }

        private void CollectProcessorBitnes()
        {
             if (Marshal.SizeOf(typeof(IntPtr)) == 8)
                x64System = true;
            else
                x64System = false;
        }

        private void CollectCPUCount()
        {
            CPUCount = Environment.ProcessorCount;
        }

      
    }
}
