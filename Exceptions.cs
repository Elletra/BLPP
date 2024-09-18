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
		public UnexpectedTokenException(char ch, int line, int col) : base($"Unexpected token '{ch}' at line {line}, col {col}") { }
		public UnexpectedTokenException(Token token) : base($"Unexpected token '{token.Value}' at line {token.Line}, col {token.Col}") { }
		public UnexpectedTokenException(string message) : base(message) { }
		public UnexpectedTokenException(string message, Exception inner) : base(message, inner) { }
	}

	public class UnexpectedEndOfLineException : Exception
	{
		public UnexpectedEndOfLineException() { }
		public UnexpectedEndOfLineException(int line, int col) : base($"Unexpected end of line at line {line}, col {col}") { }
		public UnexpectedEndOfLineException(Token token) : this(token.Line, token.Col + token.Value.Length) { }
		public UnexpectedEndOfLineException(string message) : base(message) { }
		public UnexpectedEndOfLineException(string message, Exception inner) : base(message, inner) { }
	}

	public class UnexpectedEndOfCodeException : Exception
	{
		public UnexpectedEndOfCodeException() { }
		public UnexpectedEndOfCodeException(int line, int col) : base($"Unexpected end of code at line {line}, col {col}") { }
		public UnexpectedEndOfCodeException(Token token) : this(token.Line, token.Col) { }
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
		public SyntaxException(string message, Token token) : base($"Syntax error at line {token.Line}, col {token.Col}: {message}") { }
		public SyntaxException(string message) : base(message) { }
		public SyntaxException(string message, Exception inner) : base(message, inner) { }
	}
}
