/**
 * Exceptions.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the BLPP source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using BLPP.Preprocessor;

namespace BLPP
{
	public class UnexpectedTokenException(string token, int line) : Exception($"Unexpected token `{token}` on line {line}")
	{
		public UnexpectedTokenException(char ch, int line) : this($"{ch}", line) { }
		public UnexpectedTokenException(Token token) : this(token.Value, token.Line) { }
	}

	public class UnexpectedEndOfLineException(int line) : Exception($"Unexpected end of line on line {line}")
	{
		public UnexpectedEndOfLineException(Token token) : this(token.Line) { }
	}

	public class UnexpectedEndOfCodeException(int line) : Exception($"Unexpected end of code on line {line}")
	{
		public UnexpectedEndOfCodeException(Token token) : this(token.Line) { }
	}

	public class UnterminatedStringException(int line) : Exception($"Unterminated string at line {line}") { }
	public class UnterminatedCommentException(int line) : Exception($"Unterminated comment at line {line}") { }

	public class SyntaxException(string message, int line) : Exception($"Syntax error on line {line}: {message}")
	{
		public SyntaxException(string message, Token token) : this(message, token.Line) { }
	}

	public class UndefinedMacroException(Token token) : Exception($"Undefined macro `{token.MacroName}` on line {token.Line}") { }
	public class UndefinedMacroParameterException(Token token) : Exception($"Undefined macro parameter `{token.ParameterName}` on line {token.Line}") { }
	public class MultipleDefinitionsException(string name) : Exception($"Found multiple macro definitions for '{name}'")
	{
		public MultipleDefinitionsException(Token token) : this(token.Value) { }
		public MultipleDefinitionsException(Macro macro) : this(macro.Name) { }
	}
}
