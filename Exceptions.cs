/**
 * Exceptions.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the BLPP source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using BLPP.Lexer;

namespace BLPP
{
	public class UnexpectedTokenException : Exception
	{
		public UnexpectedTokenException() { }
		public UnexpectedTokenException(string token, int line, int col) : base($"Unexpected token `{token}` at line {line}, col {col}") { }
		public UnexpectedTokenException(string token, int line) : base($"Unexpected token `{token}` on line {line}") { }
		public UnexpectedTokenException(char ch, int line, int col) : this($"{ch}", line, col) { }
		public UnexpectedTokenException(Token token) : this(token.Value, token.Line) { }
		public UnexpectedTokenException(string message) : base(message) { }
		public UnexpectedTokenException(string message, Exception inner) : base(message, inner) { }
	}

	public class UnexpectedEndOfLineException : Exception
	{
		public UnexpectedEndOfLineException() { }
		public UnexpectedEndOfLineException(int line) : base($"Unexpected end of line on line {line}") { }
		public UnexpectedEndOfLineException(Token token) : this(token.Line) { }
		public UnexpectedEndOfLineException(string message) : base(message) { }
		public UnexpectedEndOfLineException(string message, Exception inner) : base(message, inner) { }
	}

	public class UnexpectedEndOfCodeException : Exception
	{
		public UnexpectedEndOfCodeException() { }
		public UnexpectedEndOfCodeException(int line) : base($"Unexpected end of code on line {line}") { }
		public UnexpectedEndOfCodeException(Token token) : this(token.Line) { }
		public UnexpectedEndOfCodeException(string message) : base(message) { }
		public UnexpectedEndOfCodeException(string message, Exception inner) : base(message, inner) { }
	}

	public class UnterminatedStringException : Exception
	{
		public UnterminatedStringException() { }
		public UnterminatedStringException(int line, int col) : base($"Unterminated string at line {line}, col {col}") { }
		public UnterminatedStringException(string message) : base(message) { }
		public UnterminatedStringException(string message, Exception inner) : base(message, inner) { }
	}

	public class UnterminatedCommentException : Exception
	{
		public UnterminatedCommentException() { }
		public UnterminatedCommentException(int line, int col) : base($"Unterminated comment at line {line}, col {col}") { }
		public UnterminatedCommentException(string message) : base(message) { }
		public UnterminatedCommentException(string message, Exception inner) : base(message, inner) { }
	}

	public class SyntaxException : Exception
	{
		public SyntaxException() { }
		public SyntaxException(string message, int line) : base($"Syntax error on line {line}: {message}") { }
		public SyntaxException(string message, Token token) : this(message, token.Line) { }
		public SyntaxException(string message) : base(message) { }
		public SyntaxException(string message, Exception inner) : base(message, inner) { }
	}

	public class UndefinedMacroException : Exception
	{
		public UndefinedMacroException() { }
		public UndefinedMacroException(Token token) : base($"Undefined macro `{token.Value}` on line {token.Line}") { }
		public UndefinedMacroException(string message) : base(message) { }
		public UndefinedMacroException(string message, Exception inner) : base(message, inner) { }
	}

	public class UndefinedMacroArgumentException : Exception
	{
		public UndefinedMacroArgumentException() { }
		public UndefinedMacroArgumentException(Token token) : base($"Undefined macro variable `{token.Value}` on line {token.Line}") { }
		public UndefinedMacroArgumentException(string message) : base(message) { }
		public UndefinedMacroArgumentException(string message, Exception inner) : base(message, inner) { }
	}
}
