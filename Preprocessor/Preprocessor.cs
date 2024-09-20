/**
 * Preprocessor.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the BLPP source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using BLPP.Lexer;

namespace BLPP.Preprocessor
{
	public class MacroDefinition(Token macroToken, Token nameToken)
	{
		public Token NameToken { get; } = nameToken;
		public Token MacroToken { get; } = macroToken;
		public List<string> Arguments { get; set; } = [];
		public List<Token> Body { get; } = [];
		public HashSet<string> Macros { get; } = [];

		public bool IsVariadic => Arguments[^1] == "...";
		public int FixedArgumentCount => IsVariadic ? Arguments.Count - 1 : Arguments.Count;

		public string Name => NameToken.Value;
		public int Line => MacroToken.Line;

		public bool HasArgument(string test) => Arguments.Any(arg => arg == test[2..]);
		public int IndexOfArgument(string arg) => Arguments.IndexOf(arg);
	}

	public class Preprocessor
	{
		private List<Token> _tokens = [];
		private int _index = 0;
		private Dictionary<string, MacroDefinition> _macros = [];

		private bool IsAtEnd => _index >= _tokens.Count;

		public void Process(List<Token> tokens)
		{
			_tokens = tokens;
			_macros = [];

			ProcessDirectives();
			CheckForMacroLoops();
			ExpandMacros();
		}

		private void ProcessDirectives()
		{
			_index = 0;

			while (!IsAtEnd)
			{
				var token = Advance();

				switch (token.Type)
				{
					case TokenType.Directive:
						ProcessDirective(token);
						break;

					case TokenType.DirectiveConcat
					or TokenType.DirectiveCurlyLeft
					or TokenType.DirectiveCurlyRight
					or TokenType.MacroVarArgs
					or TokenType.MacroArgument
					or TokenType.MacroKeyword:
						throw new SyntaxException($"`{token.Value}` can only be used in a macro definition", token);

					default:
						break;
				}
			}
		}

		private void ProcessDirective(Token token)
		{
			switch (token.Value)
			{
				case "##define":
					ProcessDefine(token);
					break;

				case "##include":
					// TODO:
					break;

				default:
					break;
			}
		}

		private void ProcessDefine(Token token)
		{
			var name = Expect(TokenType.Identifier, TokenType.Keyword);
			var macro = new MacroDefinition(token, name);

			ExpectSameLine(token, name);

			if (Match(TokenType.ParenLeft))
			{
				ExpectSameLine(name, Advance());
				ProcessDefineArgs(macro);
			}

			var last = _tokens[_index - 1];
			var brackets = Match(TokenType.DirectiveCurlyLeft);

			if (brackets)
			{
				var bracket = Advance();

				if (bracket.Line > macro.Line + 1)
				{
					throw new SyntaxException($"Opening curly bracket cannot be more than 1 line below macro declaration", bracket);
				}
			}

			ProcessDefineBody(macro, brackets);

			if (!brackets && macro.Body.Count <= 0)
			{
				throw new UnexpectedEndOfLineException(last);
			}

			_macros[macro.Name] = macro;
		}

		private void ProcessDefineArgs(MacroDefinition macro)
		{
			var rightParen = false;

			while (!IsAtEnd && !rightParen)
			{
				var prev = _tokens[_index - 1];
				var arg = Expect(TokenType.Identifier, TokenType.Keyword, TokenType.MacroVarArgs);
				var delimiter = Expect(TokenType.Comma, TokenType.ParenRight);

				ExpectSameLine(prev, arg);
				ExpectSameLine(arg, delimiter);

				if (delimiter.Type == TokenType.Comma && arg.Type == TokenType.MacroVarArgs)
				{
					throw new SyntaxException($"Variadic macro arguments must be at the end of the arguments list", arg);
				}

				macro.Arguments.Add(arg.Value);

				rightParen = delimiter.Type == TokenType.ParenRight;
			}
		}

		private void ProcessDefineBody(MacroDefinition macro, bool leftBracket)
		{
			while (!IsAtEnd)
			{
				if ((leftBracket && Match(TokenType.DirectiveCurlyRight)) || (!leftBracket && Peek().Line > macro.Line))
				{
					break;
				}

				var token = Advance();

				if (!token.IsValidMacroBodyToken)
				{
					throw new SyntaxException($"`{token.Value}` cannot be used in a macro body", token);
				}

				if (token.Type == TokenType.Macro)
				{
					macro.Macros.Add(token.Value[1..]);
				}
				else if (token.Type == TokenType.MacroArgument && !macro.HasArgument(token.Value))
				{
					throw new UndefinedMacroArgumentException(token);
				}

				macro.Body.Add(token);
			}

			if (leftBracket)
			{
				Expect(TokenType.DirectiveCurlyRight);
			}
		}

		private void CheckForMacroLoops()
		{
			foreach (var (_, macro) in _macros)
			{
				CheckForMacroLoops(macro, [], []);
			}
		}

		private void CheckForMacroLoops(MacroDefinition check, HashSet<string> visited, List<string> path)
		{
			visited.Add(check.Name);
			path.Add(check.Name);

			foreach (var name in check.Macros)
			{
				if (visited.Contains(name))
				{
					var pathStr = $"'{string.Join("' -> '", path)}'";

					throw new SyntaxException($"Infinite macro loop {pathStr} detected", check.Line);
				}

				CheckForMacroLoops(_macros[name], visited, path);
			}
		}

		private void ExpandMacros()
		{
			_index = 0;

			while (!IsAtEnd)
			{
				var token = Advance();

				if (token.Type == TokenType.Macro)
				{
					ExpandMacro(token);
				}
			}
		}

		private void ExpandMacro(Token token)
		{
			var name = token.Value[1..];

			MacroDefinition? macroDefinition;

			if (!_macros.TryGetValue(name, out macroDefinition))
			{
				throw new UndefinedMacroException(token);
			}

			var startIndex = _index - 1;
			var body = CollectMacroBody(macroDefinition, CollectMacroArguments(macroDefinition, token.Line), token.Line);

			_tokens.RemoveRange(startIndex, _index - startIndex);
			_tokens.InsertRange(startIndex, body);

			// Reset our index back to the same index we were at before so we can keep expanding macros as much as we need.
			_index = startIndex;
		}

		private List<List<Token>> CollectMacroArguments(MacroDefinition macroDefinition, int line)
		{
			var args = new List<List<Token>>();

			if (macroDefinition.Arguments.Count > 0)
			{
				Expect(TokenType.ParenLeft);

				var parentheses = 1;
				var argIndex = 0;

				/* Collect argument tokens. */

				while (parentheses > 0)
				{
					var token = Peek();
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
						Advance();

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

				if (args.Count < macroDefinition.FixedArgumentCount)
				{
					throw new SyntaxException($"Not enough arguments passed into '{macroDefinition.Name}' macro", line);
				}

				if (args.Count > macroDefinition.FixedArgumentCount && !macroDefinition.IsVariadic)
				{
					throw new SyntaxException($"Too many arguments passed into '{macroDefinition.Name}' macro", line);
				}

				Expect(TokenType.ParenRight);
			}

			return args;
		}

		private List<Token> CollectMacroBody(MacroDefinition macroDefinition, List<List<Token>> arguments, int line)
		{
			var tokens = new List<Token>();

			foreach (var token in macroDefinition.Body)
			{
				var type = token.Type;
				var value = token.Value;

				switch (type)
				{
					case TokenType.MacroArgument:
					{
						foreach (var arg in arguments[macroDefinition.IndexOfArgument(value[2..])])
						{
							tokens.Add(new(arg, line));
						}

						break;
					}

					case TokenType.MacroKeyword:
					{
						if (value == "#!line")
						{
							tokens.Add(new(TokenType.Number, $"{line}", line));
						}
						else if (value == "#!vargc")
						{
							if (!macroDefinition.IsVariadic)
							{
								throw new SyntaxException($"Cannot use `{value}` in a non-variadic macro", line);
							}

							tokens.Add(new(TokenType.Number, $"{macroDefinition.Arguments.Count}", line));
						}
						else if (value == "#!vargs" || value == "#!vargsp")
						{
							if (!macroDefinition.IsVariadic)
							{
								throw new SyntaxException($"Cannot use `{value}` in a non-variadic macro", line);
							}

							if (value == "#!vargsp")
							{
								tokens.Add(new(TokenType.Comma, ",", line));
							}

							for (var i = macroDefinition.FixedArgumentCount; i < arguments.Count; i++)
							{
								tokens.AddRange(arguments[i]);

								if (i < arguments.Count - 1)
								{
									tokens.Add(new(TokenType.Comma, ",", line));
								}
							}
						}

						break;
					}

					default:
						tokens.Add(new(token, line));
						break;
				}
			}

			return tokens;
		}

		private Token Peek() => _tokens[_index];
		private Token Advance() => _tokens[_index++];
		private bool Match(TokenType type) => _index < _tokens.Count && _tokens[_index].Type == type;
		private bool MatchAny(params TokenType[] types) => types.Any(Match);

		/// <exception cref="UnexpectedEndOfCodeException"></exception>
		/// <exception cref="UnexpectedTokenException"></exception>
		private Token Expect(params TokenType[] types)
		{
			if (!MatchAny(types))
			{
				if (IsAtEnd)
				{
					throw new UnexpectedEndOfCodeException(_tokens[_index - 1]);
				}

				throw new UnexpectedTokenException(_tokens[_index]);
			}

			return Advance();
		}

		/// <exception cref="UnexpectedEndOfLineException"></exception>
		private void ExpectSameLine(Token test, Token token)
		{
			if (token.Line > test.Line)
			{
				throw new UnexpectedEndOfLineException(test);
			}
		}
	}
}
