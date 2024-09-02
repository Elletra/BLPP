/**
 * Exceptions.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the TorqueSharp source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

namespace TorqueSharp.Lexer
{
	public class UnexpectedTokenException : Exception
	{
		public UnexpectedTokenException() { }
		public UnexpectedTokenException(char ch, int line, int col) : base($"Unexpected token '{ch}' at {line}:{col}") { }
		public UnexpectedTokenException(string message) : base(message) { }
		public UnexpectedTokenException(string message, Exception inner) : base(message, inner) { }
	}

	public class UnexpectedEndOfLine : Exception
	{
		public UnexpectedEndOfLine() { }
		public UnexpectedEndOfLine(int line, int col) : base($"Unexpected end of line at {line}:{col}") { }
		public UnexpectedEndOfLine(string message) : base(message) { }
		public UnexpectedEndOfLine(string message, Exception inner) : base(message, inner) { }
	}

	public class UnterminatedString : Exception
	{
		public UnterminatedString() { }
		public UnterminatedString(int line, int col) : base($"Unterminated string at {line}:{col}") { }
		public UnterminatedString(string message) : base(message) { }
		public UnterminatedString(string message, Exception inner) : base(message, inner) { }
	}

	public class UnterminatedComment : Exception
	{
		public UnterminatedComment() { }
		public UnterminatedComment(int line, int col) : base($"Unterminated comment at {line}:{col}") { }
		public UnterminatedComment(string message) : base(message) { }
		public UnterminatedComment(string message, Exception inner) : base(message, inner) { }
	}

}
