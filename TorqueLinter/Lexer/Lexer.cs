/**
 * Lexer.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the TorqueLinter source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using Shared;
using Shared.Util;
using System.Collections.Immutable;
using static TorqueLinter.Constants.Parser;

namespace TorqueLinter.Lexer
{
	public class Lexer
	{
		private TextStreamReader _stream = new("");
		private List<Token> _tokens = [];
		private string _token = "";
		private bool _whitespaceBefore = false;

		static private readonly ImmutableHashSet<string> _keywords =
		[
			PACKAGE_TOKEN,
			FUNCTION_TOKEN,
			RETURN_TOKEN,
			WHILE_TOKEN,
			FOR_TOKEN,
			BREAK_TOKEN,
			CONTINUE_TOKEN,
			IF_TOKEN,
			ELSE_TOKEN,
			SWITCH_TOKEN,
			SWITCH_STRING_TOKEN,
			CASE_TOKEN,
			OR_TOKEN,
			DEFAULT_TOKEN,
			DATABLOCK_TOKEN,
			NEW_TOKEN,
		];

		static private readonly ImmutableHashSet<string> _multicharOperators =
		[
			STR_NOT_EQ_TOKEN,
			SHL_ASSIGN_TOKEN,
			SHR_ASSIGN_TOKEN,
			ADD_ASSIGN_TOKEN,
			SUB_ASSIGN_TOKEN,
			MUL_ASSIGN_TOKEN,
			DIV_ASSIGN_TOKEN,
			MOD_ASSIGN_TOKEN,
			BIT_OR_ASSIGN_TOKEN,
			BIT_AND_ASSIGN_TOKEN,
			BIT_XOR_ASSIGN_TOKEN,
			INCREMENT_TOKEN,
			DECREMENT_TOKEN,
			STR_EQUAL_TOKEN,
			EQUAL_TOKEN,
			NOT_EQUAL_TOKEN,
			LT_EQUAL_TOKEN,
			GT_EQUAL_TOKEN,
			SHL_TOKEN,
			SHR_TOKEN,
			LOGIC_OR_TOKEN,
			LOGIC_AND_TOKEN,
		];

		static private readonly ImmutableHashSet<string> _concatCharOperators =
		[
			CONCAT_SPACE_TOKEN,
			CONCAT_TAB_TOKEN,
			CONCAT_NEWLINE_TOKEN,
		];

		public List<Token> Scan(string code)
		{
			_stream = new(code);
			_tokens = [];
			_token = "";
			_whitespaceBefore = false;

			Scan();

			return _tokens;
		}

		private void Scan()
		{
			while (!_stream.IsAtEnd)
			{
				var col = _stream.Col;

				Scan(_stream.Read(), col);
			}
		}

		private void Scan(char ch, int col)
		{
			var clearWhitespace = true;

			switch (ch)
			{
				case ' ' or '\t':
					_whitespaceBefore = true;
					clearWhitespace = false;
					break;

				case '\r' or '\n':
					ScanNewline(ch);
					break;

				case '(' or ')' or '{' or '}' or '[' or ']' or '.' or ',' or '?' or ':' or ';':
					ScanDelimiter(ch, col);
					break;

				case '@':
					_token = $"{ch}";
					AddToken(TokenType.Concat, col);
					break;

				case '+' or '-' or '*' or '/' or '<' or '>' or '=' or '|' or '&' or '%' or '$' or '^' or '~' or '!':
					if (ch == '/' && _stream.AdvanceIfMatch('/'))
					{
						ScanComment();
					}
					else if ((ch == '%' || ch == '$') && _stream.MatchIdentifierStart())
					{
						ScanVariable(ch, col);
					}
					else
					{
						ScanOperator(ch, col);
					}

					break;

				case '\'' or '"':
					ScanString(ch, col);
					break;

				default:
					if (char.IsAsciiDigit(ch))
					{
						ScanNumber(ch, col);
					}
					else if (char.IsAsciiLetter(ch) || ch == '_')
					{
						ScanIdentifierOrKeyword(ch, col);
					}
					else
					{
						throw new UnexpectedTokenException(_stream.Line, col, ch);
					}

					break;
			}

			if (clearWhitespace)
			{
				_whitespaceBefore = false;
			}
		}

		private void ScanNewline(char ch)
		{
			if (ch == '\r')
			{
				_stream.AdvanceIfMatch('\n');
			}

			_token = "";
		}

		private void ScanDelimiter(char ch, int col)
		{
			_token += ch;

			if (ch == ':' && _stream.Match(':'))
			{
				_token += _stream.Read();
			}

			AddToken(_token switch
			{
				"(" => TokenType.ParenLeft,
				")" => TokenType.ParenRight,
				"{" => TokenType.CurlyLeft,
				"}" => TokenType.CurlyRight,
				"[" => TokenType.SquareLeft,
				"]" => TokenType.SquareRight,
				"." => TokenType.Period,
				"," => TokenType.Comma,
				"?" => TokenType.QuestionMark,
				":" => TokenType.Colon,
				"::" => TokenType.ColonColon,
				";" => TokenType.Semicolon,
				_ => throw new UnexpectedTokenException(_stream.Line, col, ch),
			}, col);
		}

		private void ScanComment()
		{
			while (!_stream.MatchAny("\r\n"))
			{
				_stream.Advance();
			}

			_token = "";
		}

		private void ScanVariable(char ch, int col)
		{
			_token = $"{ch}{_stream.Read()}";

			var endOfVariable = false;

			while (!_stream.IsAtEnd && !endOfVariable)
			{
				while (_stream.MatchIdentifierChar())
				{
					_token += _stream.Read();
				}

				var colonIndex = _stream.Index;
				var colonCol = _stream.Col;
				var colons = "";

				while (_stream.Match(':'))
				{
					colons += _stream.Read();
				}

				if (_stream.MatchIdentifierChar())
				{
					_token += colons;
				}
				else
				{
					// Rewind if the colons aren't part of the variable name.
					_stream.Seek(colonIndex);
					_stream.Col = colonCol;

					endOfVariable = true;
				}
			}

			AddToken(TokenType.Variable, col);
		}

		private void ScanOperator(char ch, int col)
		{
			try
			{
				// This works because the operators are sorted by longest first.
				_token = _multicharOperators.First(op => _stream.Match(op, offset: -1));

				_stream.Advance(amount: _token.Length - 1);
			}
			catch (InvalidOperationException)
			{
				if (ch == '$')
				{
					throw new UnexpectedTokenException(_stream.Line, col, ch);
				}

				// Nothing was found, so it's a single-character operator.
				_token = $"{ch}";
			}

			AddToken(_token switch
			{
				ASSIGN_TOKEN or MOD_ASSIGN_TOKEN or
				ADD_ASSIGN_TOKEN or SUB_ASSIGN_TOKEN or
				MUL_ASSIGN_TOKEN or DIV_ASSIGN_TOKEN or
				SHL_ASSIGN_TOKEN or SHR_ASSIGN_TOKEN or
				BIT_OR_ASSIGN_TOKEN or BIT_AND_ASSIGN_TOKEN => TokenType.Assignment,
				INCREMENT_TOKEN or DECREMENT_TOKEN => TokenType.IncrementDecrement,
				_ => TokenType.Operator,
			}, col);
		}

		private void ScanString(char quote, int col)
		{
			_token += quote;

			var escapeChars = 0;
			var matchingQuote = false;

			while (!_stream.IsAtEnd && !matchingQuote)
			{
				var ch = _stream.Read();

				if (ch == '\r' || ch == '\n')
				{
					throw new UnterminatedStringException(_stream.Line);
				}

				matchingQuote = ch == quote && escapeChars % 2 == 0;

				if (!matchingQuote)
				{
					escapeChars = ch == '\\' ? escapeChars + 1 : 0;
				}

				_token += ch;
			}

			if (!matchingQuote)
			{
				throw new UnterminatedStringException(_stream.Line);
			}

			AddToken(TokenType.String, col);
		}

		private void ScanNumber(char ch, int col)
		{
			_token = $"{ch}";

			var type = TokenType.Integer;

			if (ch == '0' && _stream.MatchAny("xX") && _stream.MatchHexDigit())
			{
				/* Handle hex */

				_token += _stream.Read();

				while (_stream.MatchHexDigit())
				{
					_token += _stream.Read();
				}
			}
			else
			{
				ScanDigits();

				if (_stream.Match('.') && _stream.MatchDigit(offset: 1))
				{
					/* Handle decimals */

					type = TokenType.Float;
					_token += _stream.Read();

					ScanDigits();
				}

				if (_stream.MatchAny("eE"))
				{
					/* Handle scientific notation */

					if (_stream.MatchDigit(offset: 1))
					{
						type = TokenType.Float;
						_token += _stream.Read();

						ScanDigits();
					}
					else if (_stream.MatchAny("+-", offset: 1) && _stream.MatchDigit(offset: 2))
					{
						type = TokenType.Float;
						_token += $"{_stream.Read()}{_stream.Read()}";

						ScanDigits();
					}
				}
			}

			AddToken(type, col);
		}

		private void ScanDigits()
		{
			while (_stream.MatchDigit())
			{
				_token += _stream.Read();
			}
		}

		private void ScanIdentifierOrKeyword(char ch, int col)
		{
			_token = $"{ch}";

			while (_stream.MatchIdentifierChar())
			{
				_token += _stream.Read();
			}

			var type = TokenType.Identifier;

			if (_concatCharOperators.Contains(_token))
			{
				type = TokenType.Concat;
			}
			else if (_keywords.Contains(_token))
			{
				type = TokenType.Keyword;
			}

			AddToken(type, col);
		}

		private Token AddToken(TokenType type, int col)
		{
			var token = new Token(type, _token, _stream.Line, col)
			{
				WhitespaceBefore = _whitespaceBefore,
			};

			_token = "";
			_tokens.Add(token);

			return token;
		}
	}
}
