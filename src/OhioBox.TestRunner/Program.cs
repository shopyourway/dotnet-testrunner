using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using CommandLine;

namespace OhioBox.TestRunner
{
	class Program
	{
		public class Options
		{
			[Option('f',"folder", Required = true, HelpText = "Folder that contains your test projects")]
			public string TestsFolder { get; set; }

			[Option('i', "include", Required = false, HelpText = "Included categories (assigned as traits with the name \"Categgory\"")]
			public IEnumerable<string> IncludedCategories { get; set; }

			[Option('e', "exclude", Required = false, HelpText = "Excluded categories (assigned as traits with the name \"Categgory\"")]
			public IEnumerable<string> ExcludedCategories { get; set; }

		}

		static int Main(string[] args)
		{
			Console.WriteLine($"ARGS: {string.Join(",",args)}");
			var error = true;
			var result = Parser.Default.ParseArguments<Options>(args);
			result.WithParsed(options =>
			{
				Console.WriteLine($"Folder: {options.TestsFolder}");
				if (options.IncludedCategories != null)
					Console.WriteLine($"Include: {string.Join(",", options.IncludedCategories)}");
				if (options.ExcludedCategories != null)
					Console.WriteLine($"Exclude: {string.Join(",", options.ExcludedCategories)}");

				var filter = BuildFilter(options.IncludedCategories, options.ExcludedCategories);

				foreach (var f in DirSearch(options.TestsFolder, x => x.EndsWith("Tests.csproj", StringComparison.OrdinalIgnoreCase)))
				{
					var command = $"test \"{f}\"";
					if (!string.IsNullOrEmpty(filter))
						command += $" --filter \"{filter}\"";

					Console.WriteLine($"Running tests for \"{f}\".\n Command: {command}");
					;
					var processStartInfo = new ProcessStartInfo("dotnet", command);

					var process = new Process { StartInfo = processStartInfo };

					process.Start();

					process.WaitForExit();
				}
				Console.WriteLine("All done");
				error = false;
			});

			return error ? 1 : 0;
		}

		private static string BuildFilter(IEnumerable<string> includedCategories, IEnumerable<string> excludedCategories)
		{
			var filterBuilder = new StringBuilder();

			var included = string.Join("|", includedCategories.Select(x => $"Category={x}"));
			var excluded = string.Join("|", excludedCategories.Select(x => $"Category!={x}"));

			if (!string.IsNullOrEmpty(included))
				filterBuilder.Append($"({included})");

			if (!string.IsNullOrEmpty(included) && !string.IsNullOrEmpty(excluded))
				filterBuilder.Append("&");

			if (!string.IsNullOrEmpty(excluded))
				filterBuilder.Append($"({excluded})");

			return filterBuilder.ToString();
		}

		static IEnumerable<string> DirSearch(string dir, Func<string, bool> predicate)
		{
			foreach (var d in Directory.GetDirectories(dir))
			{
				foreach (var f in Directory.GetFiles(d).Where(predicate))
				{
					yield return f;
				}

				foreach (var f in DirSearch(d, predicate))
				{
					yield return f;
				}
			}
		}
	}
}