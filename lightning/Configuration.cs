using System;
using System.IO;
using System.Text.Json;

namespace lightning
{
	class ConfigRead
	{
		public int CallStackSize { get; set; }
		public string VMLogFile { get; set; }
		public string TryLogFile { get; set; }
		public string CompilerLogFile { get; set; }
		public string ParserLogFile { get; set; }
		public string ScannerLogFile { get; set; }
		public string AssembliesPath { get; set; }

		public ConfigRead()
		{
			CallStackSize = 30;
			VMLogFile = "_vm.log";
			TryLogFile = "_try.log";
			CompilerLogFile = "_compiler.log";
			ParserLogFile = "_parser.log";
			ScannerLogFile = "_scanner.log";
			AssembliesPath = "refs";
		}

		public Config ToConfig()
		{
			return new Config(
				CallStackSize,
				VMLogFile,
				TryLogFile,
				CompilerLogFile,
				ParserLogFile,
				ScannerLogFile,
				AssembliesPath
			);
		}
	}

	public struct Config
	{
		public int CallStackSize { get; }
		public string VMLogFile { get; }
		public string TryLogFile { get; }
		public string CompilerLogFile { get; }
		public string ParserLogFile { get; }
		public string ScannerLogFile { get; }
		public string AssembliesPath { get; }
		public string BaseDirectoryPath { get; }

		public Config(
			int p_CallStackSize,
			string p_VMLogFile,
			string p_TryLogFile,
			string p_CompilerLogFile,
			string p_ParserLogFile,
			string p_ScannerLogFile,
			string p_AssembliesPath
		)
		{
			BaseDirectoryPath = AppDomain.CurrentDomain.BaseDirectory;

			CallStackSize = p_CallStackSize;
			VMLogFile = p_VMLogFile;
			TryLogFile = p_TryLogFile;
			CompilerLogFile = p_CompilerLogFile;
			ParserLogFile = p_ParserLogFile;
			ScannerLogFile = p_ScannerLogFile;
			AssembliesPath = System.IO.Path.Combine(BaseDirectoryPath, p_AssembliesPath);
		}
	}

	public class Defaults
	{
		private const string configPath = "Defaults.json";
		public static Config Config { get; private set;}

		static Defaults()
		{
			ConfigRead source = new ConfigRead();

			try
			{
				using (StreamReader r = new StreamReader(Defaults.configPath))
				{
					string read_json = r.ReadToEnd();
					source = JsonSerializer.Deserialize<ConfigRead>(read_json);
				}
			}
			catch (Exception)
			{
				Console.WriteLine("NO 'VM.json' -> Created new 'VM.json', using VM defaults");

				string jsonString = JsonSerializer.Serialize(source, new JsonSerializerOptions() { WriteIndented = true });
				using (StreamWriter outFile = new StreamWriter(Defaults.configPath))
				{
					outFile.WriteLine(jsonString);
				}
			}
			finally
			{
				Config = source.ToConfig();
			}
		}
	}
}