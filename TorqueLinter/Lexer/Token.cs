/**
 * Token.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the TorqueLinter source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using static TorqueLinter.Constants.Parser;

namespace TorqueLinter.Lexer
{
	public enum TokenType : byte
	{
		Identifier,
		Keyword,
		String, Integer, Float, Variable,
		ParenLeft, ParenRight, CurlyLeft, CurlyRight, SquareLeft, SquareRight,
		Period, Comma, QuestionMark, Colon, ColonColon, Semicolon,
		Operator, Concat, Assignment, IncrementDecrement,
		Invalid,
	}

	public class Token(TokenType type, string value, int line, int col)
	{
		public TokenType Type { get; } = type;
		public string Value { get; } = value;
		public int Line { get; } = line;
		public int Col { get; } = col;
		public bool WhitespaceBefore { get; set; } = false;

		public bool IsValid => Type < TokenType.Invalid;

		public bool IsExpressionEnd => Type switch
		{
			TokenType.Keyword => Value == OR_TOKEN,
			TokenType.Comma or TokenType.Colon or TokenType.Semicolon => true,
			_ => false,
		};

		public bool IsDelimiter => Type switch
		{
			TokenType.ParenLeft or TokenType.ParenRight or
			TokenType.CurlyLeft or TokenType.CurlyRight or
			TokenType.SquareLeft or TokenType.SquareRight or
			TokenType.Period or TokenType.Comma or
			TokenType.Colon or TokenType.ColonColon or
			TokenType.Semicolon => true,
			_ => false,
		};

		public bool IsOperator => Type switch
		{
			TokenType.Operator or TokenType.Concat or
			TokenType.Assignment or TokenType.IncrementDecrement => true,
			_ => false,
		};
	}
}
