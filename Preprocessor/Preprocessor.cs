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
		public List<Token> Arguments { get; set; } = [];
		public List<Token> Body { get; } = [];
		public HashSet<string> Macros { get; } = [];

		public string Name => NameToken.Value;
		public int Line => MacroToken.Line;

		public bool HasArgument(string test) => Arguments.Any(arg => arg.Value == test[2..]);
		public int ArgumentIndexOf(string strArg)
		{
			var index = -1;

			for (var i = 0; i < Arguments.Count && index < 0; i++)
			{
				if (Arguments[i].Value == strArg[2..])
				{
					index = i;
				}
			}

			return index;
		}

		public Token? FindArgument(string strArg)
		{
			var index = ArgumentIndexOf(strArg);

			return index >= 0 ? Arguments[index] : null;
		}
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
					or TokenType.MacroArgument
					or TokenType.MacroKeyword:
						throw new SyntaxException($"'{token.Value}' can only be used in a macro definition", token);

					default:
						break;
				}
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
				var arg = Expect(TokenType.Identifier, TokenType.Keyword);
				var delimiter = Expect(TokenType.Comma, TokenType.ParenRight);

				ExpectSameLine(prev, arg);
				ExpectSameLine(arg, delimiter);

				macro.Arguments.Add(arg);

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
					throw new SyntaxException($"'{token.Value}' cannot be used in a macro body", token);
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

		private void ExpandMacro(Token macroToken)
		{
			var name = macroToken.Value[1..];

			MacroDefinition? macro;

			if (!_macros.TryGetValue(name, out macro))
			{
				throw new UndefinedMacroException(macroToken);
			}

			var startIndex = _index - 1;
			var argLine = macroToken.Line;
			var macroArgs = CollectMacroArguments(macroToken, macro);
			var insertTokens = new List<Token>();

			foreach (var token in macro.Body)
			{
				var type = token.Type;
				var value = token.Value;

				switch (type)
				{
					case TokenType.MacroArgument:
					{
						foreach (var arg in macroArgs[value[2..]])
						{
							insertTokens.Add(new(arg, argLine));
						}

						break;
					}

					case TokenType.MacroKeyword:
					{
						if (value == "#!argc")
						{
							insertTokens.Add(new(TokenType.Number, $"{macro.Arguments.Count}", argLine));
						}
						else if (value == "#!line")
						{
							insertTokens.Add(new(TokenType.Number, $"{macroToken.Line}", argLine));
						}

						break;
					}

					default:
						insertTokens.Add(new(token, argLine));
						break;
				}
			}

			_tokens.RemoveRange(startIndex, _index - startIndex);
			_tokens.InsertRange(startIndex, insertTokens);

			_index = startIndex;
		}

		private Dictionary<string, List<Token>> CollectMacroArguments(Token token, MacroDefinition macro)
		{
			var args = new Dictionary<string, List<Token>>();

			if (macro.Arguments.Count > 0)
			{
				Expect(TokenType.ParenLeft);

				var parentheses = 1;
				var argIndex = 0;

				/* Collect argument tokens. */

				while (parentheses > 0)
				{
					var argToken = Peek();
					var tokenType = argToken.Type;

					if (tokenType == TokenType.ParenLeft)
					{
						parentheses++;
					}
					else if (tokenType == TokenType.ParenRight)
					{
						parentheses--;
					}

					if (parentheses > 0)
					{
						Advance();

						if (parentheses == 1 && tokenType == TokenType.Comma)
						{
							argIndex++;

							if (argIndex >= macro.Arguments.Count)
							{
								throw new SyntaxException($"Invalid number of arguments for macro '{macro.Name}' on line {token.Line}");
							}
						}
						else
						{
							var argName = macro.Arguments[argIndex].Value;

							if (!args.ContainsKey(argName))
							{
								args[argName] = [];
							}

							args[argName].Add(new(argToken, token.Line));
						}
					}
				}

				Expect(TokenType.ParenRight);
			}

			return args;
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
