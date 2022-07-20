using System.Diagnostics;
using System.Runtime.InteropServices;

namespace dumpurself
{
    public static class Program
    {
        private static readonly string URLTemplate = "https://aka.ms/dotnet-dump/$RID";

        public static async Task Main(string[] args)
        {
            var bin = Path.GetTempFileName();
            var output = Path.Combine(Path.GetTempPath(), "dump.dmp");

            try
            {
                var url = GetURL();

                Log($"Downloading dotnet-dump binary from {url}");
                Log($"Saving to {bin}");

                await Download(url, bin);

                Log("Done!");

                var pid = Process.GetCurrentProcess().Id;

                Log($"Dumping memory for PID {pid}");
                Log($"Saving dump to {output}");

                Dump(bin, pid, output);
            }
            catch (Exception ex)
            {
                Log($"Failed to dump memory: {ex.Message}");
            }
            finally
            {
                if (TryDelete(bin)) Log($"Deleted dotnet-dump binary {bin}");
            }
        }

        public static void Dump(string bin, int pid, string output)
        {
            var process = new Process();
            process.StartInfo.FileName = bin;
            process.StartInfo.Arguments = $"collect --process-id {pid} --type full --output {output}";
            process.Start();
            process.WaitForExit();
        }

        public static async Task Download(string url, string destination)
        {
            using var http = new HttpClient();

            using var localStream = new FileStream(destination, FileMode.Open);
            using var remoteStream = await http.GetStreamAsync(GetURL());

            await remoteStream.CopyToAsync(localStream);
        }

        public static void Log(string msg) => Console.WriteLine(msg);

        public static string GetRID()
        {
            var arch = RuntimeInformation.ProcessArchitecture.ToString().ToLower();

            static string OS()
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "linux";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "win";
                return "osx";
            }

            return $"{OS()}-{arch}";
        }

        public static string GetURL() => URLTemplate.Replace("$RID", GetRID());

        public static bool TryDelete(string file)
        {
            try
            {
                File.Delete(file);
                return true;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }
    }
}
