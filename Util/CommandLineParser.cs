/**
 * CommandLineParser.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the BLPP source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using BLPP.Preprocessor;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BLPP.Util
{
	public class CommandLineOptions
	{
		private string _path = "";
		public bool PathSpecified { get; private set; } = false;
		public string Path { get => _path; set { _path = value; PathSpecified = true; } }

		public bool IsDirectory { get; set; } = false;
		public bool Watch { get; set; } = false;
		public bool Silent { get; set; } = false;
	}

	static public class CommandLineParser
	{
		static public Tuple<bool, CommandLineOptions> Parse(string[] args)
		{
			var options = new CommandLineOptions();
			var error = false;

			for (var i = 0; i < args.Length; i++)
			{
				var arg = args[i];

				switch (arg)
				{
					case "--silent" or "-q":
						options.Silent = true;
						break;

					case "--directory" or "-d":
						options.IsDirectory = true;
						break;

					case "--watch" or "-w":
						options.Watch = true;
						break;

					default:
					{
						if (arg.StartsWith('-'))
						{
							Logger.LogError($"Unknown or unsupported flag '{arg}'");

							if (arg == "-Q" || arg == "-D" || arg == "-W")
							{
								Logger.LogError($"Did you mean '{arg.ToLower()}'?");
							}

							error = true;

						}
						else if (options.PathSpecified)
						{
							Logger.LogError($"Multiple paths specified");
							error = true;
						}
						else
						{
							options.Path = arg;
						}

						break;
					}
				}
			}

			if (!options.PathSpecified)
			{
				options.Path = Directory.GetCurrentDirectory();
				options.IsDirectory = true;
			}

			return new(error || !ValidateOptions(options), options);
		}

		static private bool ValidateOptions(CommandLineOptions options)
		{
			if (options.IsDirectory)
			{
				if (!Directory.Exists(options.Path))
				{
					Logger.LogError($"Directory does not exist at the path specified");
					return false;
				}

				return true;
			}

			if (!File.Exists(options.Path))
			{
				Logger.LogError($"File does not exist at the path specified");
				Logger.LogError($"If the path is a directory, please specify with --directory' or -d'");

				return false;
			}

			if (Path.GetExtension(options.Path) != Constants.Preprocessor.FILE_EXTENSION)
			{
				Logger.LogError($"File at the path specified does not have a '{Constants.Preprocessor.FILE_EXTENSION}' extension");
				Logger.LogError($"If the path is a directory, please specify with --directory' or -d'");

				return false;
			}

			if (options.Watch)
			{
				Logger.LogError($"'--watch' flag only works for directories");
				Logger.LogError($"Please specify that the path is a directory with '--directory' or '-d'");

				return false;
			}

			return true;
		}
	}
}
