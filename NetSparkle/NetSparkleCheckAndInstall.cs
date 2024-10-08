﻿using System;
using System.Collections.Generic;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using Ionic.Zip;
using Shell32;

namespace AppLimit.NetSparkle
{
    public static class NetSparkleCheckAndInstall
    {

        public static void Install(Sparkle sparkle, String tempName, bool restartApp, string installCommandOptions, Action shutdownCallback)
        {
            try
            {


                // get the commandline 
                String cmdLine = Environment.CommandLine;
                String workingDir = Environment.CurrentDirectory;

                // generate the batch file path
                String cmd = Environment.ExpandEnvironmentVariables("%temp%\\" + Guid.NewGuid() + ".cmd");
                String installerCMD;

                // get the file type
                if (Path.GetExtension(tempName).ToLower().Equals(".exe"))
                {
                    // build the command line 
                    installerCMD = tempName;
                }
                else if (tempName.ToLower().EndsWith(".msi.zip"))
                {
                    string unpacked = UnpackZip(tempName).First();
                    Install(sparkle, unpacked, restartApp, installCommandOptions, shutdownCallback);
                    return;
                }
                else if (tempName.ToLower().EndsWith(".exe.zip"))
                {
                    string unpacked = UnpackZip(tempName).First();
                    Install(sparkle, unpacked, restartApp, installCommandOptions, shutdownCallback);
                    return;
                }
                else if (Path.GetExtension(tempName).ToLower() == ".zip")
                {
                    installerCMD = tempName;
                }
                else if (Path.GetExtension(tempName).ToLower().Equals(".msi"))
                {
                    // build the command line
                    installerCMD = "msiexec /i \"" + tempName + "\"";

                    if (sparkle.EnableServiceMode)
                    {
                        installerCMD += " /qn";
                    }
                }
                else
                {
                    sparkle.ReportDiagnosticMessage("Updater not supported, please execute " + tempName + " manually");
                    //MessageBox.Show("Updater not supported, please execute " + tempName + " manually", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(-1);
                    return;
                }

                if (!string.IsNullOrEmpty(installCommandOptions))
                    installerCMD += " " + installCommandOptions;

                // generate the batch file                
                sparkle.ReportDiagnosticMessage("Generating MSI batch in " + Path.GetFullPath(cmd));

                StreamWriter write = new StreamWriter(cmd);

                if (sparkle.EnableServiceMode)
                    write.WriteLine("net stop \"" + sparkle.ServiceName + "\"");

                write.WriteLine(installerCMD);
                write.WriteLine("cd " + workingDir);

                if (sparkle.EnableServiceMode)
                    write.WriteLine("net start \"" + sparkle.ServiceName + "\"");
                else if (restartApp)
                    write.WriteLine(cmdLine);

                write.Close();

                // report
                sparkle.ReportDiagnosticMessage("Going to execute batch: " + cmd);

                // start the installer helper
                Process process = new Process();
                process.StartInfo.FileName = cmd;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.Start();

                if (shutdownCallback != null)
                {
                    shutdownCallback();
                }
                else
                {
                    // quit the app
                    Environment.Exit(0);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Sorry, but the update could not be installed due to\r\n\r\n" + e.Message + "\r\n\r\nPlease try again, or update manually.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Trace.WriteLine(e);
            }
        }

        private static List<string> UnpackZip(string tempName)
        {
            var zf = new ZipFile(tempName);
            string path = Path.GetDirectoryName(tempName);
            zf.ExtractAll(path);
            DirectoryInfo di = new DirectoryInfo(path);

            return di.GetFiles().Select(x => x.FullName).Where(x => x != tempName).ToList();
        }

        public static bool CheckDSA(Sparkle sparkle, NetSparkleAppCastItem item, String tempName)
        {
            Boolean bDSAOk = false;

            // check if we have a dsa signature in appcast            
            if (item.DSASignature == null || item.DSASignature.Length == 0)
            {
                sparkle.ReportDiagnosticMessage("No DSA check needed");
                bDSAOk = true;
            }
            else
            {
                // report
                sparkle.ReportDiagnosticMessage("Performing DSA check");

                // get the assembly
                if (File.Exists(tempName))
                {
                    // check if the file was downloaded successfully
                    String absolutePath = Path.GetFullPath(tempName);
                    if (!File.Exists(absolutePath))
                        throw new FileNotFoundException();

                    // get the assembly reference from which we start the update progress
                    // only from this trusted assembly the public key can be used
                    Assembly refassembly = System.Reflection.Assembly.GetEntryAssembly();
                    if (refassembly != null)
                    {
                        // Check if we found the public key in our entry assembly
                        if (NetSparkleDSAVerificator.ExistsPublicKey("NetSparkle_DSA.pub"))
                        {
                            // check the DSA Code and modify the back color            
                            NetSparkleDSAVerificator dsaVerifier = new NetSparkleDSAVerificator("NetSparkle_DSA.pub");
                            bDSAOk = dsaVerifier.VerifyDSASignature(item.DSASignature, tempName);
                        }
                    }
                }
            }

            return bDSAOk;
        }
    }
}
