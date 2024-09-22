namespace BLPP.Preprocessor
{
	public class Preprocessor
	{
		private readonly Lexer _lexer = new();
		private readonly DirectiveParser _parser = new();
		private readonly DirectiveProcessor _processor = new();

		public List<string> PreprocessFile(string filePath)
		{
			var queue = new Queue<string>();
			var macros = new Dictionary<string, Macro>();
			var parsed = new Dictionary<string, List<Token>>();
			var processed = new List<string>();

			queue.Enqueue(Path.GetFullPath(filePath, Directory.GetCurrentDirectory()));

			var basePath = Path.GetDirectoryName(Path.GetFullPath(filePath));

			while (queue.Count > 0)
			{
				var nextPath = queue.Dequeue();

				if (parsed.ContainsKey(nextPath))
				{
					continue;
				}

				Console.WriteLine($"Processing: \"{nextPath}\"");

				if (Path.GetExtension(nextPath) != Constants.Preprocessor.EXTENSION)
				{
					throw new FileExtensionException();
				}

				if (!File.Exists(nextPath))
				{
					throw new FileNotFoundException($"File not found: \"{nextPath}\"", nextPath);
				}

				var code = File.ReadAllText(nextPath);
				var tokens = _lexer.Scan(code);

				if (tokens.Count <= 0)
				{
					Console.WriteLine($"\tFile is empty! Skipping...\n");
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

					processed.Add(nextPath);
				}
			}

			/* Write output file. */

			foreach (var (name, tokens) in parsed)
			{
				var processedTokens = _processor.Process(tokens, macros);
				var code = "";
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
				
				if (processedTokens.Count > 0)
				{
					Console.WriteLine($"Writing processed file: \"{newFile}\"");
					File.WriteAllText(newFile, code);
				}
				else
				{
					Console.WriteLine($"Processed file \"{newFile}\" would be empty, skipping...");
				}
			}

			return processed;
		}
	}
}
