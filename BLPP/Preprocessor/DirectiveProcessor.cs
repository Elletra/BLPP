/**
 * DirectiveProcessor.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the BLPP source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using BLPP.Util;
using Shared;

namespace BLPP.Preprocessor
{
	public class DirectiveProcessor
	{
		private PreprocessorTokenReader _stream = new([]);
		private Dictionary<string, Macro> _macros = [];

		public List<Token> Process(List<Token> tokens, Dictionary<string, Macro> macros)
		{
			_stream = new([..tokens]);
			_macros = macros;

			ValidateMacros();
			ExpandMacros();
			ApplyConcatenation();

			return _stream.Stream;
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

					throw new SyntaxException(check.Line, $"Infinite macro recursion {pathStr}");
				}

				CheckForMacroLoops(_macros[name], visited, path);
			}
		}

		private void ValidateMacroBody(Macro macro)
		{
			foreach (var token in macro.Body)
			{
				if (token.Type == TokenType.Macro)
				{
					if (!_macros.ContainsKey(token.MacroName))
					{
						throw new UndefinedMacroException(token);
					}

					if (token.MacroName == macro.Name)
					{
						throw new SyntaxException(token.Line, $"Macro '{macro.Name}' cannot invoke itself");
					}
				}
				else if (token.Type == TokenType.MacroParameter && !macro.HasArgument(token.Value))
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

			_stream.Remove(startIndex, _stream.Index - startIndex);
			_stream.Insert(startIndex, body);

			// Reset our index back to where we started so we can keep expanding macros as much as we need.
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
					throw new SyntaxException(line, $"Macro '{macro.Name}' requires at least {macro.FixedArgumentCount} argument(s)");
				}

				return [];
			}

			_stream.Consume(TokenType.ParenLeft);

			var args = new List<List<Token>>();
			var argIndex = 0;
			var parentheses = 1;

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
				throw new SyntaxException(line, $"Not enough arguments passed into macro '{macro.Name}'");
			}

			if (args.Count > macro.FixedArgumentCount && !macro.IsVariadic)
			{
				throw new SyntaxException(line, $"Too many arguments passed into macro '{macro.Name}'");
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

				if (!token.IsValidMacroBodyToken)
				{
					throw new UnexpectedTokenException(token.Line, token.Value);
				}

				if (type == TokenType.MacroParameter)
				{
					foreach (var arg in args[macro.Arguments.IndexOf(token.ParameterName)])
					{
						body.Add(new(arg, line));
					}
				}
				else if (type == TokenType.MacroKeyword)
				{
					switch (value)
					{
						case Constants.Tokens.MACRO_KEYWORD_LINE:
							body.Add(new(TokenType.Number, $"{line}", line, whitespace));
							break;

						case Constants.Tokens.MACRO_KEYWORD_VARG_COUNT:
							body.Add(new(TokenType.Number, $"{macro.FixedArgumentCount - args.Count}", line, whitespace));
							break;

						case Constants.Tokens.MACRO_KEYWORD_VARGS or Constants.Tokens.MACRO_KEYWORD_VARGS_PREPEND:
						{
							var fixedArgsCount = macro.FixedArgumentCount;

							if (args.Count > fixedArgsCount)
							{
								var prependComma = value == Constants.Tokens.MACRO_KEYWORD_VARGS_PREPEND;

								if (prependComma)
								{
									body.Add(new(TokenType.Comma, ",", line, whitespace));
								}

								args[fixedArgsCount][0].WhitespaceBefore = prependComma ? " " : whitespace;

								for (var i = fixedArgsCount; i < args.Count; i++)
								{
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
							throw new SyntaxException(token.Line, $"Unknown or unsupported macro keyword `{value}`");
					}
				}
				else
				{
					body.Add(new(token, line));
				}
			}

			return body;
		}

		private void StripDirective(Token token)
		{
			var startIndex = _stream.Index - 1;

			if (token.Value == Constants.Tokens.DIRECTIVE_USE)
			{
				// Advance past file name.
				_stream.Advance();
			}
			else if (token.Value == Constants.Tokens.DIRECTIVE_DEFINE)
			{
				var name = _stream.Consume(TokenType.Identifier);
				var macro = _macros[name.Value];

				/* Advance past parameter list (if any). */

				if (macro.Arguments.Count > 0)
				{
					while (!_stream.IsAtEnd && !_stream.AdvanceIfMatch(TokenType.ParenRight))
					{
						_stream.Advance();
					}
				}

				/* Advance past macro body. */

				var brackets = _stream.AdvanceIfMatch(TokenType.DirectiveCurlyLeft);

				while (!_stream.IsAtEnd)
				{
					if ((brackets && _stream.AdvanceIfMatch(TokenType.DirectiveCurlyRight)) || (!brackets && !_stream.MatchLine(token)))
					{
						break;
					}

					_stream.Advance();
				}
			}
			else if (token.Value != Constants.Tokens.DIRECTIVE_BLCS)
			{
				throw new SyntaxException(token.Line, $"Unknown or unsupported preprocessor directive `{token.Value}`");
			}

			// Strip out the directive.
			_stream.Remove(startIndex, count: _stream.Index - startIndex);
			_stream.Seek(startIndex);
		}

		private void ApplyConcatenation()
		{
			_stream.Rewind();

			while (!_stream.IsAtEnd)
			{
				if (_stream.Read().Type == TokenType.MacroConcat)
				{
					var left = _stream.Peek(-2);
					var right = _stream.Peek();

					if (left.Type == TokenType.String && right.Type == TokenType.String && left.Value[0] == right.Value[0])
					{
						var quote = left.Value[0];

						left.Value = quote + left.Value[1..^1] + right.Value[1..^1] + quote;
						_stream.Remove(_stream.Index - 1, count: 2);
					}
					else
					{
						_stream.Peek().WhitespaceBefore = "";
						_stream.Remove(_stream.Index - 1, count: 1);
					}
				}
			}
		}
	}
}
