using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using Microsoft.Win32;

namespace CopyOTP
{
    class Program
    {
        private const string bSpoofPath = "SpoofInfo.txt";
        private const string bOtpPath = "OtpInfo.reg";
        private const string bOtpRegPath = "HKEY_CURRENT_USER\\Software\\AnyOTP";
        private static void exportRegistry(string strKey, string filepath)
        {
            try
            {
                using (Process proc = new Process())
                {
                    proc.StartInfo.FileName = "reg.exe";
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.RedirectStandardError = true;
                    proc.StartInfo.CreateNoWindow = true;
                    proc.StartInfo.Arguments = "export \"" + strKey + "\" \"" + filepath + "\" /y";
                    proc.Start();
                    string stdout = proc.StandardOutput.ReadToEnd();
                    string stderr = proc.StandardError.ReadToEnd();
                    proc.WaitForExit();
                }
            }
            catch (Exception ex)
            {
               Console.WriteLine(ex);
            }
        }

        private static void exportSpoof(string filepath)
        {
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");

                foreach (ManagementObject wmi_HD in searcher.Get())
                {
                    var bIndex = Convert.ToInt32(wmi_HD["Index"]);

                    if (bIndex == 0)
                    {
                        var bPNP = wmi_HD["PNPDeviceID"].ToString();
                        var bSIGTemp = wmi_HD["SIgnature"];
                        var bSIG = bSIGTemp is null ? 0 : Convert.ToUInt32(bSIGTemp);

                        Console.WriteLine(bPNP);
                        Console.WriteLine(bSIG);

                        File.WriteAllText(filepath, $"{bPNP}{Environment.NewLine}{bSIG}{Environment.NewLine}0");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        static void Main(string[] args)
        {
            exportSpoof(bSpoofPath);
            exportRegistry(bOtpRegPath, bOtpPath);

            if(File.Exists(bSpoofPath))
                Console.WriteLine($"{bSpoofPath} Dosyasını StartOTP.exe yanına koyun.");
            else
                Console.WriteLine($"{bSpoofPath} Dosya export edilemedi.");


            if (File.Exists(bOtpPath))
                Console.WriteLine($"{bOtpPath} Dosyayı çalıştırın.");
            else
                Console.WriteLine($"{bOtpPath} Dosya export edilemedi.");

            Console.ReadLine();
        }
    }
}
