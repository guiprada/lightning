using System;
using System.IO;
using System.Text.Json;

namespace lightning
{
    class VMConfigRead
    {
        public int callStackSize { get; set; }
        public String logFile { get; set; }
    }

    public struct VMConfig
    {
        public int callStackSize { get; }
        public String logFile { get; }

        public VMConfig(
            int p_callStackSize,
            string p_VMLogFile)
        {
            callStackSize = p_callStackSize;
            logFile = p_VMLogFile;
        }
    }

    public class VMDefaults
    {
        private const String configPath = "VM.json";
        private const int callStackSize = 30;
        private const String logFile = "_vm.log";

        public static VMConfig GetConfig()
        {
            VMConfigRead source = new VMConfigRead();
            VMConfig new_config;

            try
            {
                using (StreamReader r = new StreamReader(VMDefaults.configPath))
                {
                    string read_json = r.ReadToEnd();
                    source = JsonSerializer.Deserialize<VMConfigRead>(read_json);
                }
            }
            catch (Exception)
            {
                Console.WriteLine("NO 'VM.json' -> Created new 'VM.json', using VM defaults");
                source.callStackSize = VMDefaults.callStackSize;
                source.logFile = VMDefaults.logFile;

                string jsonString = JsonSerializer.Serialize(source, new JsonSerializerOptions() { WriteIndented = true });
                using (StreamWriter outFile = new StreamWriter(VMDefaults.configPath))
                {
                    outFile.WriteLine(jsonString);
                }
            }
            finally
            {
                new_config = new VMConfig(
                    source.callStackSize,
                    source.logFile
                );
            }

            return new_config;
        }
    }
}