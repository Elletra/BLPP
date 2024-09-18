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
	public class MacroDefinition(Token token)
	{
		public Token NameToken { get; } = token;
		public List<Token> Arguments { get; set; } = [];
		public List<Token> Body { get; } = [];

		public string Name => NameToken.Value;
		public int Line => NameToken.Line;
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
			_index = 0;
			_macros = [];

			Process();
		}

		private void Process()
		{
			while (!IsAtEnd)
			{
				var token = Advance();

				switch (token.Type)
				{
					case TokenType.Directive:
						ProcessDirective(token);
						break;

					case TokenType.Macro:
						ProcessMacro(token);
						break;

					case TokenType.DirectiveArgs
					or TokenType.DirectiveVariable
					or TokenType.DirectiveCurlyLeft
					or TokenType.DirectiveCurlyRight
					or TokenType.DirectiveConcat
					or TokenType.MacroKeyword:
						throw new SyntaxException($"'{token.Value}' can only be used in a macro definition", token);

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
			var macro = new MacroDefinition(name);

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
				var arg = Expect(TokenType.Identifier, TokenType.Keyword, TokenType.DirectiveArgs);
				var delimiter = Expect(TokenType.Comma, TokenType.ParenRight);

				ExpectSameLine(prev, arg);
				ExpectSameLine(arg, delimiter);

				macro.Arguments.Add(arg);

				rightParen = delimiter.Type == TokenType.ParenRight;
			}

			foreach (var arg in macro.Arguments)
			{
				if (arg.Type == TokenType.DirectiveArgs && arg != macro.Arguments[^1])
				{
					throw new SyntaxException($"Variadic macro arguments must be at the end of the arguments list", arg);
				}
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

				macro.Body.Add(token);
			}

			if (leftBracket)
			{
				Expect(TokenType.DirectiveCurlyRight);
			}
		}

		private void ProcessMacro(Token token)
		{

		}

		private Token Peek() => _tokens[_index];
		private Token Advance() => _tokens[_index++];
		private bool Match(TokenType type) => _index < _tokens.Count ? _tokens[_index].Type == type : false;
		private bool MatchAny(params TokenType[] types) => types.Any(Match);

		private bool MatchAdvance(TokenType type)
		{
			var matched = Match(type);

			if (matched)
			{
				Advance();
			}

			return matched;
		}

		private Token Expect(params TokenType[] types)
		{
			if (!MatchAny(types))
			{
				if (IsAtEnd)
				{
					throw new UnexpectedEndOfCodeException(_tokens[_index - 1]);
				}

				throw new UnexpectedTokenException(_tokens[_index - 1]);
			}

			return Advance();
		}

		private void ExpectSameLine(Token test, Token token)
		{
			if (token.Line > test.Line)
			{
				throw new UnexpectedEndOfLineException(test);
			}
		}
	}
}
