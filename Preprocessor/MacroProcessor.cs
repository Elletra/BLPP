/**
 * MacroProcessor.cs
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

		public bool IsVariadic => Arguments[^1] == Token.MACRO_VAR_ARGS_STRING;
		public int FixedArgumentCount => IsVariadic ? Arguments.Count - 1 : Arguments.Count;

		public bool HasArgument(string arg) => Arguments.Contains(arg[2..]);
	}

	public class MacroProcessor
	{
		private PreprocessorTokenReader _stream = new([]);
		private Dictionary<string, Macro> _macros = [];

		public Dictionary<string, Macro> Process(List<Token> tokens)
		{
			_stream = new(tokens);
			_macros = [];

			CollectMacros();
			ValidateMacros();

			return _macros;
		}

		private void CollectMacros()
		{
			while (!_stream.IsAtEnd)
			{
				Process(_stream.Read());
			}
		}

		private void Process(Token token)
		{
			if (token.Type == TokenType.Directive)
			{
				if (token.Value == "##define")
				{
					ProcessDefine(token);
				}
				else if (token.Value == "##include")
				{
					throw new NotImplementedException();
				}
				else
				{
					throw new SyntaxException($"Unknown or unsupported preprocessor directive '{token.Value}'", token);
				}
			}
		}

		private void ProcessDefine(Token define)
		{
			var name = _stream.Consume(TokenType.Identifier);
			var macro = new Macro(name.Value, define.Line);

			ExpectSameLine(define, name);

			if (_macros.ContainsKey(name.Value))
			{
				throw new MultipleDefinitionsException(name);
			}

			if (_stream.Match(TokenType.ParenLeft))
			{
				ExpectSameLine(name, _stream.Read());
				ProcessDefineArgs(macro);
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

			ProcessDefineBody(macro, brackets);

			if (!brackets && macro.Body.Count <= 0)
			{
				throw new UnexpectedEndOfLineException(define);
			}

			_macros[macro.Name] = macro;
		}

		private void ProcessDefineArgs(Macro macro)
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

				// Consume delimiter
				_stream.Advance();
			}

			_stream.Consume(TokenType.ParenRight);

			for (var i = 0; i < macro.Arguments.Count; i++)
			{
				if (macro.Arguments[i] == Token.MACRO_VAR_ARGS_STRING && i != macro.Arguments.Count - 1)
				{
					throw new SyntaxException($"Variadic macro parameters must be at the end of a parameter list", macro.Line);
				}
			}
		}

		private void ProcessDefineBody(Macro macro, bool brackets)
		{
			while (!_stream.IsAtEnd)
			{
				if ((brackets && _stream.Match(TokenType.DirectiveCurlyRight)) || (!brackets && _stream.Peek().Line > macro.Line))
				{
					break;
				}

				var token = _stream.Read();

				if (!token.IsValidMacroBodyToken)
				{
					throw new SyntaxException($"`{token.Value}` cannot be used in a macro body", token);
				}

				if (token.Type == TokenType.Macro)
				{
					var name = token.MacroName;

					if (name == macro.Name)
					{
						throw new SyntaxException($"Macro '{name}' cannot invoke itself", token.Line);
					}

					macro.Macros.Add(name);
				}
				else if (token.Type == TokenType.MacroParameter && !macro.HasArgument(token.Value))
				{
					throw new UndefinedMacroParameterException(token);
				}

				macro.Body.Add(token);
			}

			if (brackets)
			{
				_stream.Consume(TokenType.DirectiveCurlyRight);
			}
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

		private void ExpectSameLine(Token token1, Token token2)
		{
			if (token1.Line < token2.Line)
			{
				throw new UnexpectedEndOfLineException(token1);
			}
		}
	}
}
