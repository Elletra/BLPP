/**
 * DirectiveProcessor.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the BLPP source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using BLPP.Util;

namespace BLPP.Preprocessor
{
	public class DirectiveProcessor
	{
		private PreprocessorTokenReader _stream = new([]);
		private Dictionary<string, Macro> _macros = [];

		public void Process(List<Token> tokens, Dictionary<string, Macro> macros)
		{
			_stream = new(tokens);
			_macros = macros;

			ValidateMacros();
			ExpandMacros();
			ApplyConcatenation();
		}

		private void ValidateMacros()
		{
			foreach (var (_, macro) in _macros)
			{
				ValidateMacroBody(macro);
				CheckForMacroLoops(macro, [], []);
			}
		}

		private void CheckForMacroLoops(Macro check, HashSet<string> visited, List<string> path)
		{
			visited.Add(check.Name);
			path.Add(check.Name);

			foreach (var name in check.Macros)
			{
				if (visited.Contains(name))
				{
					var pathStr = $"'{string.Join("' -> '", path)}'";

					throw new SyntaxException($"Infinite macro recursion {pathStr}", check.Line);
				}

				CheckForMacroLoops(_macros[name], visited, path);
			}
		}

		private void ValidateMacroBody(Macro macro)
		{
			foreach (var token in macro.Body)
			{
				if (token.Type == TokenType.Macro && !_macros.ContainsKey(token.MacroName))
				{
					throw new UndefinedMacroException(token);
				}

				if (token.Type == TokenType.MacroParameter && !macro.HasArgument(token.Value))
				{
					throw new UndefinedMacroParameterException(token);
				}
			}
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
				else if (token.Type == TokenType.Directive)
				{
					StripDirective(token);
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

			if (body.Count > 0)
			{
				if (body[0].Type == TokenType.MacroConcat)
				{
					throw new SyntaxException($"Macro concatenation operator `{body[0].Value}` missing left side operand", body[0]);
				}

				if (body[^1].Type == TokenType.MacroConcat)
				{
					throw new SyntaxException($"Macro concatenation operator `{body[^1].Value}` missing right side operand", body[^1]);
				}
			}

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

							var prependComma = value == "#!vargsp";

							if (prependComma)
							{
								body.Add(new(TokenType.Comma, ",", line, whitespace));
							}

							for (var i = macro.FixedArgumentCount; i < args.Count; i++)
							{
								if (i == macro.FixedArgumentCount && args[i].Count > 0)
								{
									args[i][0].WhitespaceBefore = prependComma ? " " : whitespace;
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

		private void StripDirective(Token token)
		{
			var startIndex = _stream.Index - 1;

			if (token.Value == "##use")
			{
				_stream.Advance();
			}
			else if (token.Value == "##define")
			{
				var name = _stream.Consume(TokenType.Identifier);
				var macro = _macros[name.Value];

				if (macro.Arguments.Count > 0)
				{
					/* Skip past parameter list. */

					while (!_stream.IsAtEnd && !_stream.AdvanceIfMatch(TokenType.ParenRight))
					{
						_stream.Advance();
					}
				}

				var brackets = _stream.Match(TokenType.DirectiveCurlyLeft);

				while (!_stream.IsAtEnd)
				{
					/* Skip past macro body. */

					if ((brackets && _stream.AdvanceIfMatch(TokenType.DirectiveCurlyRight)) || (!brackets && !_stream.MatchLine(token)))
					{
						break;
					}

					_stream.Advance();
				}
			}
			else
			{
				throw new SyntaxException($"Unknown or unsupported preprocessor directive '{token.Value}'", token);
			}

			// Strip out the directive.
			_stream.Remove(startIndex, _stream.Index - startIndex);
			_stream.Seek(startIndex);
		}

		private void ApplyConcatenation()
		{
			_stream.Rewind();

			while (!_stream.IsAtEnd)
			{
				if (_stream.Read().Type == TokenType.MacroConcat)
				{
					_stream.Peek().WhitespaceBefore = "";
					_stream.Remove(_stream.Index - 1, 1);
				}
			}
		}
	}
}
