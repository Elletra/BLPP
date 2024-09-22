/**
 * DirectiveParser.cs
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
	public class Macro(string name, int line)
	{
		public readonly string Name = name;
		public readonly int Line = line;
		public readonly List<string> Arguments = [];
		public readonly List<Token> Body = [];
		public readonly HashSet<string> Macros = [];

		public bool IsVariadic => Arguments[^1] == Constants.Tokens.MACRO_VAR_ARGS;
		public int FixedArgumentCount => IsVariadic ? Arguments.Count - 1 : Arguments.Count;

		public bool HasArgument(string arg) => Arguments.Contains(arg[2..]);
	}

	public class DirectiveData
	{
		public readonly Dictionary<string, Macro> Macros = [];
		public readonly HashSet<string> Files = [];

		public bool AddMacro(Macro macro)
		{
			if (Macros.ContainsKey(macro.Name))
			{
				return false;
			}

			Macros[macro.Name] = macro;

			return true;
		}

		public bool AddFile(string fileName)
		{
			if (Files.Contains(fileName))
			{
				return false;
			}

			Files.Add(fileName);

			return true;
		}
	}

	/// <summary>
	/// This class parses directives and then returns macros and file names to import.
	/// </summary>
	public class DirectiveParser
	{
		private PreprocessorTokenReader _stream = new([]);
		private DirectiveData _data = new();

		public DirectiveData Parse(List<Token> tokens)
		{
			_stream = new(tokens);
			_data = new();

			ParseMacros();

			return _data;
		}

		private void ParseMacros()
		{
			while (!_stream.IsAtEnd)
			{
				Parse(_stream.Read());
			}
		}

		private void Parse(Token token)
		{
			if (token.Type == TokenType.Directive)
			{
				if (token.Value == Constants.Tokens.DIRECTIVE_DEFINE)
				{
					ParseDefine(token);
				}
				else if (token.Value == Constants.Tokens.DIRECTIVE_USE)
				{
					ParseUse(token);
				}
				else
				{
					throw new SyntaxException($"Unknown or unsupported preprocessor directive `{token.Value}`", token);
				}
			}
		}

		private void ParseDefine(Token define)
		{
			var name = _stream.Consume(TokenType.Identifier);
			var macro = new Macro(name.Value, define.Line);

			ExpectSameLine(define, name);

			if (!_data.AddMacro(macro))
			{
				throw new MultipleDefinitionsException(name);
			}

			if (_stream.Match(TokenType.ParenLeft))
			{
				ExpectSameLine(name, _stream.Read());
				ParseDefineArgs(macro);
			}

			var brackets = _stream.Match(TokenType.DirectiveCurlyLeft);

			if (brackets)
			{
				var bracket = _stream.Read();

				if (bracket.Line > macro.Line + 1)
				{
					throw new SyntaxException($"Opening curly bracket cannot be more than 1 line below macro declaration", bracket);
				}
			}

			ParseDefineBody(macro, brackets);

			if (!brackets && macro.Body.Count <= 0)
			{
				throw new UnexpectedEndOfLineException(define);
			}

			if (macro.Body.Count > 0)
			{
				macro.Body[0].WhitespaceBefore = "";
			}
		}

		private void ParseDefineArgs(Macro macro)
		{
			while (!_stream.IsAtEnd)
			{
				var prev = _stream.Peek(-1);
				var arg = _stream.Consume(TokenType.Identifier, TokenType.MacroVarArgs);
				var delimiter = _stream.Expect(TokenType.Comma, TokenType.ParenRight);

				ExpectSameLine(prev, arg);
				ExpectSameLine(arg, delimiter);

				macro.Arguments.Add(arg.Value);

				if (delimiter.Type == TokenType.ParenRight)
				{
					break;
				}

				_stream.Advance();
			}

			_stream.Consume(TokenType.ParenRight);

			for (var i = 0; i < macro.Arguments.Count; i++)
			{
				if (macro.Arguments[i] == Constants.Tokens.MACRO_VAR_ARGS && i != macro.Arguments.Count - 1)
				{
					throw new SyntaxException($"Variadic macro parameters must be at the end of a parameter list", macro.Line);
				}
			}
		}

		private void ParseDefineBody(Macro macro, bool brackets)
		{
			while (!_stream.IsAtEnd)
			{
				if ((brackets && _stream.Match(TokenType.DirectiveCurlyRight)) || (!brackets && !_stream.MatchLine(macro.Line)))
				{
					break;
				}

				var token = _stream.Read();
				var value = token.Value;

				switch (token.Type)
				{
					case TokenType.Macro:
						macro.Macros.Add(token.MacroName);
						break;

					case TokenType.MacroParameter:
						if (!macro.HasArgument(value))
						{
							throw new UndefinedMacroParameterException(token);
						}

						break;

					case TokenType.MacroKeyword:
						if (token.IsVariadicMacroKeyword && !macro.IsVariadic)
						{
							throw new SyntaxException($"Cannot use `{value}` in a non-variadic macro", token);
						}

						break;

					default:
						break;
				}

				macro.Body.Add(token);
			}

			if (brackets)
			{
				_stream.Consume(TokenType.DirectiveCurlyRight);
			}
		}

		private void ParseUse(Token token)
		{
			var name = _stream.Consume(TokenType.String);

			ExpectSameLine(token, name);

			if (_stream.MatchLine(token))
			{
				throw new UnexpectedTokenException(_stream.Peek());
			}

			_data.AddFile(name.Value);
		}

		private void ExpectSameLine(Token baseToken, Token testToken)
		{
			if (baseToken.Line != testToken.Line)
			{
				throw new UnexpectedEndOfLineException(baseToken);
			}
		}
	}
}
