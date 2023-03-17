using System;
using System.Diagnostics;
using System.IO;
using System.Management;

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
                var deviceList = new Dictionary<int, Tuple<string, uint>>();

                foreach (ManagementObject wmi_HD in searcher.Get())
                {
                    var bIndex = Convert.ToInt32(wmi_HD["Index"]);
                    var bPNP = wmi_HD["PNPDeviceID"];
                    var bSIG = wmi_HD["SIgnature"];
                    var bModel = wmi_HD["Model"];

                    deviceList.Add(bIndex, Tuple.Create(bPNP.ToString(), bSIG is null ? uint.MinValue : Convert.ToUInt32(bSIG.ToString())));

                    Console.WriteLine($"{bIndex} - {bModel}");
                }

                Tuple<string, uint> selected;

                if (deviceList.Count > 1)
                {
                    Console.Write("Sistemin Kurulu Olduğu Diski seçin: ");
                    selected = deviceList[Convert.ToInt32(Console.ReadLine())];
                }
                else
                    selected = deviceList.First().Value;

                File.WriteAllText(filepath, $"{selected.Item1}{Environment.NewLine}{selected.Item2}{Environment.NewLine}0");
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
