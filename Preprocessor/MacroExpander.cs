using BLPP.Util;

namespace BLPP.Preprocessor
{
	public class MacroExpander
	{
		private PreprocessorTokenReader _stream = new([]);
		private Dictionary<string, Macro> _macros = [];

		public void Expand(List<Token> tokens, Dictionary<string, Macro> macros)
		{
			_stream = new(tokens);
			_macros = macros;

			ExpandMacros();
			ApplyConcatenation();
		}

		private void ExpandMacros()
		{
			while (!_stream.IsAtEnd)
			{
				var token = _stream.Read();

				if (token.Type == TokenType.Macro)
				{
					ExpandMacro(token);
				}
			}
		}

		private void ExpandMacro(Token token)
		{
			var name = token.MacroName;

			Macro? macro;

			if (!_macros.TryGetValue(name, out macro))
			{
				throw new UndefinedMacroException(token);
			}

			var startIndex = _stream.Index - 1;
			var args = CollectMacroArguments(macro, token.Line);
			var body = CollectMacroBody(macro, args, token.Line);

			_stream.Remove(startIndex, _stream.Index - startIndex);
			_stream.Insert(startIndex, body);

			// Reset our index back to the same index we were at before so we can keep expanding macros as much as we need.
			_stream.Seek(startIndex);
		}

		private List<List<Token>> CollectMacroArguments(Macro macro, int line)
		{
			if (macro.Arguments.Count <= 0)
			{
				return [];
			}

			if (!_stream.Match(TokenType.ParenLeft))
			{
				if (macro.FixedArgumentCount > 0)
				{
					throw new SyntaxException($"Macro '{macro.Name}' requires at least {macro.FixedArgumentCount} argument(s)", line);
				}

				return [];
			}

			var args = new List<List<Token>>();

			_stream.Consume(TokenType.ParenLeft);

			var parentheses = 1;
			var argIndex = 0;

			while (!_stream.IsAtEnd && parentheses > 0)
			{
				var token = _stream.Peek();
				var type = token.Type;

				if (type == TokenType.ParenLeft)
				{
					parentheses++;
				}
				else if (type == TokenType.ParenRight)
				{
					parentheses--;
				}

				if (parentheses > 0)
				{
					_stream.Advance();

					if (parentheses == 1 && type == TokenType.Comma)
					{
						argIndex++;
					}
					else
					{
						if (argIndex >= args.Count)
						{
							args.Add([]);
						}

						args[argIndex].Add(new(token, line));
					}
				}
			}

			_stream.Consume(TokenType.ParenRight);

			if (args.Count < macro.FixedArgumentCount)
			{
				throw new SyntaxException($"Not enough arguments passed into macro '{macro.Name}'", line);
			}

			if (args.Count > macro.FixedArgumentCount && !macro.IsVariadic)
			{
				throw new SyntaxException($"Too many arguments passed into macro '{macro.Name}'", line);
			}

			return args;
		}

		private List<Token> CollectMacroBody(Macro macro, List<List<Token>> args, int line)
		{
			var body = new List<Token>();

			foreach (var token in macro.Body)
			{
				var type = token.Type;
				var value = token.Value;
				var whitespace = token.WhitespaceBefore;

				switch (type)
				{
					case TokenType.MacroParameter:
						foreach (var arg in args[macro.Arguments.IndexOf(token.ParameterName)])
						{
							body.Add(new(arg, line));
						}

						break;

					case TokenType.MacroKeyword:
					{
						if (value == "#!line")
						{
							body.Add(new(TokenType.Number, $"{line}", line, whitespace));
						}
						else if (value == "#!vargc")
						{
							if (!macro.IsVariadic)
							{
								throw new SyntaxException($"Cannot use `{value}` in a non-variadic macro", line);
							}

							body.Add(new(TokenType.Number, $"{macro.FixedArgumentCount - args.Count}", line, whitespace));
						}
						else if (value == "#!vargs" || value == "#!vargsp")
						{
							if (!macro.IsVariadic)
							{
								throw new SyntaxException($"Cannot use `{value}` in a non-variadic macro", line);
							}

							if (value == "#!vargsp")
							{
								body.Add(new(TokenType.Comma, ",", line, whitespace));
							}

							for (var i = macro.FixedArgumentCount; i < args.Count; i++)
							{
								if (i == macro.FixedArgumentCount && args[i].Count > 0)
								{
									args[i][0].WhitespaceBefore = whitespace;
								}

								body.AddRange(args[i]);

								if (i < args.Count - 1)
								{
									body.Add(new(TokenType.Comma, ",", line));
								}
							}
						}

						break;
					}

					default:
						body.Add(new(token, line));
						break;
				}
			}

			return body;
		}

		private void ApplyConcatenation()
		{
			// TODO:
			throw new NotImplementedException();
		}
	}
}
