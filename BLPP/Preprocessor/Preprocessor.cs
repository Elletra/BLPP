/**
 * Preprocessor.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the BLPP source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using BLPP.Util;
using Shared;
using static BLPP.Preprocessor.Constants.Preprocessor;

namespace BLPP.Preprocessor
{
	public class Preprocessor
	{
		private const long WATCHER_CACHE_TIMEOUT = 100;
		private const int WATCHER_BUFFER_SIZE = 2 << 14;

		private readonly Lexer _lexer = new();
		private readonly DirectiveParser _parser = new();
		private readonly DirectiveProcessor _processor = new();

		private CommandLineOptions _options = new();

		private FileSystemWatcher _watcher = new();
		private readonly Dictionary<string, long> _watcherCache = [];

		public void Preprocess(CommandLineOptions options)
		{
			_options = options;

			if (options.IsDirectory)
			{
				if (options.Watch)
				{
					WatchDirectory(options.Path);
				}
				else if (PreprocessDirectory(options.Path) <= 0)
				{
					Logger.LogError($"Directory contains no files with the '{FILE_EXTENSION}' extension");
				}
			}
			else
			{
				PreprocessFile(options.Path);
			}
		}

		private bool PreprocessFile(string filePath)
		{
			var success = true;

			try
			{
				PreprocessFile(filePath, []);
			}
			catch (Exception exception)
			{
				PreprocessorErrorHandler(exception);
				success = false;
			}

			return success;
		}

		private int PreprocessDirectory(string directoryPath)
		{
			var files = Directory.GetFiles(directoryPath, $"*{FILE_EXTENSION}");
			var visited = new HashSet<string>();

			foreach (var file in files)
			{
				try
				{
					PreprocessFile(file, visited);
				}
				catch (Exception exception)
				{
					PreprocessorErrorHandler(exception);
					break;
				}
			}

			return files.Length;
		}

		private void PreprocessFile(string filePath, HashSet<string> visited)
		{
			var startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
			var queue = new Queue<string>();
			var macros = new Dictionary<string, Macro>();
			var parsed = new Dictionary<string, List<Token>>();

			filePath = Path.GetFullPath(filePath);
			queue.Enqueue(filePath);

			var basePath = Path.GetDirectoryName(filePath);

			while (queue.Count > 0)
			{
				var nextPath = queue.Dequeue();

				if (visited.Contains(nextPath))
				{
					continue;
				}

				Logger.LogMessage($"Parsing: \"{nextPath}\"");

				if (Path.GetExtension(nextPath) != FILE_EXTENSION)
				{
					throw new FileExtensionException(nextPath, FILE_EXTENSION);
				}

				if (!File.Exists(nextPath))
				{
					throw new FileNotFoundException($"File not found: \"{nextPath}\"", nextPath);
				}

				visited.Add(nextPath);

				using var stream = new FileStream(nextPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				using var reader = new StreamReader(stream);

				var code = reader.ReadToEnd();
				var tokens = _lexer.Scan(code);

				if (tokens.Count <= 0)
				{
					Logger.LogMessage($"\tFile is empty! Skipping...");
				}
				else
				{
					var data = _parser.Parse(tokens);

					parsed[nextPath] = tokens;

					/* Queue up more files to process. */

					foreach (var file in data.Files)
					{
						queue.Enqueue(Path.GetFullPath(file, basePath));
					}

					/* Combine macros. */

					foreach (var (name, macro) in data.Macros)
					{
						if (macros.ContainsKey(name))
						{
							throw new MultipleDefinitionsException(macro);
						}

						macros[name] = macro;
					}
				}
			}

			/* Write output file. */

			foreach (var (name, tokens) in parsed)
			{
				Logger.LogMessage($"Processing: \"{name}\"");

				var processedTokens = _processor.Process(tokens, macros);
				var code = FILE_TOP_COMMENT;
				var line = 1;

				foreach (var token in processedTokens)
				{
					for (var i = 0; i < token.Line - line; i++)
					{
						code += "\n";
					}

					code += $"{token.WhitespaceBefore}{token.Value}";

					line = token.Line;
				}

				var path = Path.GetDirectoryName(name);
				var newFile = Path.GetFullPath($"{Path.GetFileNameWithoutExtension(name)}.cs", path);
				var empty = processedTokens.Count <= 0;

				if (!empty || _options.OutputEmptyFiles)
				{
					using var stream = new FileStream(newFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
					using var writer = new StreamWriter(stream);

					writer.Write($"{code}\n\n{FILE_BOTTOM_COMMENT}");

					if (empty)
					{
						Logger.LogMessage($"\tOutput empty processed file: \"{newFile}\"", ConsoleColor.DarkGray);
					}
					else
					{
						Logger.LogMessage($"\tOutput processed file: \"{newFile}\"", ConsoleColor.DarkGray);
					}
				}
				else
				{
					Logger.LogMessage($"\tProcessed file would be empty, skipping...", ConsoleColor.DarkYellow);
				}
			}

			if (parsed.Count > 0)
			{
				Logger.LogSuccess($"BLPP finished successfully in {DateTimeOffset.Now.ToUnixTimeMilliseconds() - startTime} ms\n");
			}
		}

		private void WatchDirectory(string directoryPath)
		{
			_watcher = new(directoryPath)
			{
				NotifyFilter = NotifyFilters.FileName
					| NotifyFilters.LastWrite
					| NotifyFilters.Security
					| NotifyFilters.Size,

				Filter = $"*{FILE_EXTENSION}",
				InternalBufferSize = WATCHER_BUFFER_SIZE,
				IncludeSubdirectories = true,
				EnableRaisingEvents = true,
			};

			_watcher.Created += OnWatchedFileCreated;
			_watcher.Changed += OnWatchedFileChanged;
			_watcher.Deleted += OnWatchedFileDeleted;
			_watcher.Error += OnWatchedFileError;

			Logger.LogMessage($"Watching directory \"{directoryPath}\" for changes...\n", ConsoleColor.Cyan);

			PreprocessDirectory(directoryPath);

			while (true)
			{
				_watcher.WaitForChanged(WatcherChangeTypes.Changed);
			}
		}

		private void OnWatchedFileCreated(object sender, FileSystemEventArgs args)
		{
			if (args.ChangeType == WatcherChangeTypes.Created)
			{
				Logger.LogMessage($"Detected file creation: \"${args.FullPath}\"");

				try
				{
					PreprocessFile(args.FullPath, []);
				}
				catch (Exception exception)
				{
					PreprocessorErrorHandler(exception);
				}
			}
		}

		private void OnWatchedFileChanged(object sender, FileSystemEventArgs args)
		{
			if (args.ChangeType == WatcherChangeTypes.Changed)
			{
				/**
				 * There is a quirk with `SystemFileWatcher` (and file systems in general) that makes
				 * multiple events fire at once when a file is changed. The (somewhat hacky) solution
				 * is to keep track of when each file was last processed.
				 */
				if (UpdateWatcherEntry(args.FullPath, args.ChangeType))
				{
					Logger.LogMessage($"Detected file change: \"${args.FullPath}\"");

					try
					{
						PreprocessFile(args.FullPath, []);
					}
					catch (Exception exception)
					{
						PreprocessorErrorHandler(exception);
					}
				}
			}
		}

		private void OnWatchedFileDeleted(object sender, FileSystemEventArgs args)
		{
			if (args.ChangeType == WatcherChangeTypes.Deleted)
			{
				Logger.LogWarning($"Detected file deletion: \"{args.FullPath}\"");
				_watcherCache.Remove(args.FullPath);
			}
		}

		private void OnWatchedFileError(object sender, ErrorEventArgs args) => Logger.LogMessage(args.GetException().Message);

		private bool UpdateWatcherEntry(string fileName, WatcherChangeTypes changeType)
		{
			var updated = false;
			var key = $"{fileName}{changeType}";

			if (_watcherCache.TryGetValue(key, out long updateTime))
			{
				var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();

				if (now - updateTime >= WATCHER_CACHE_TIMEOUT)
				{
					_watcherCache[key] = now;
					updated = true;
				}
			}
			else
			{
				_watcherCache[key] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
				updated = true;
			}

			return updated;
		}

		private void PreprocessorErrorHandler(Exception exception)
		{
			switch (exception)
			{
				case SyntaxException:
					Logger.LogError(exception.Message, indented: true);
					break;

				case UnexpectedTokenException
					or UnexpectedEndOfLineException
					or UnexpectedEndOfCodeException
					or UnterminatedStringException
					or UnterminatedCommentException:
					Logger.LogError($"Syntax error: {exception.Message}", indented: true);
					break;

				case UndefinedMacroException
					or UndefinedMacroParameterException
					or MultipleDefinitionsException:
					Logger.LogError($"Preprocessing error: {exception.Message}", indented: true);
					break;

				default:
					Logger.LogError($"{exception.Message}");
					break;
			}

			Logger.LogWarning($"BLPP failed to process one or more file(s)!\n");
		}
	}
}
