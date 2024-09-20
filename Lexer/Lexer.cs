/**
 * Lexer.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the BLPP source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using System.Collections.Immutable;

namespace BLPP.Lexer
{
	public enum TokenType
	{
		Keyword,
		Identifier, Number, String, Variable,
		ParenLeft, ParenRight, CurlyLeft, CurlyRight, SquareLeft, SquareRight,
		Comma, Period, QuestionMark, Colon, ColonColon, Semicolon,
		Operator, AssignOperator,
		Directive, DirectiveCurlyLeft, DirectiveCurlyRight,
		Macro, MacroVarArgs, MacroArgument, MacroConcat, MacroKeyword,
	};

	public class Token
	{
		public TokenType Type { get; }
		public string Value { get; }
		public int Line { get; set; }

		public Token(TokenType type, string value, int line)
		{
			Type = type;
			Value = value;
			Line = line;
		}

		public Token(Token copy, int line)
		{
			Type = copy.Type;
			Value = copy.Value;
			Line = line;
		}

		public bool IsPreprocessorToken => Type switch
		{
			TokenType.Directive => true,
			TokenType.DirectiveCurlyLeft => true,
			TokenType.DirectiveCurlyRight => true,
			TokenType.Macro => true,
			TokenType.MacroVarArgs => true,
			TokenType.MacroArgument => true,
			TokenType.MacroConcat => true,
			TokenType.MacroKeyword => true,
			_ => false,
		};

		/// <summary>
		/// Whether or not this token can be used in a macro definition body.
		/// </summary>
		public bool IsValidMacroBodyToken => Type switch
		{
			TokenType.Directive => false,
			TokenType.DirectiveCurlyLeft => false,
			TokenType.DirectiveCurlyRight => false,
			TokenType.MacroVarArgs => false,
			_ => true,
		};
	}

	public class Lexer
	{
		private enum CharType
		{
			IdentifierStart,
			Identifier,
			Digit,
			HexDigit,
		}

		static private readonly ImmutableHashSet<string> _keywords =
		[
			"package",
			"function",
			"return",
			"while",
			"for",
			"break",
			"continue",
			"if",
			"else",
			"switch",
			"switch$",
			"case",
			"or",
			"default",
			"datablock",
			"new",
			"SPC",
			"TAB",
			"NL",
		];

		static private readonly ImmutableHashSet<string> _multicharOperators =
		[
			"+=",
			"-=",
			"*=",
			"/=",
			"<=",
			">=",
			"==",
			"|=",
			"&=",
			"%=",
			"^=",
			"!=",
			"<<=",
			">>=",
			"++",
			"--",
			"<<",
			">>",
			"||",
			"&&",
			"$=",
			"!$=",
		];

		static private bool IsMulticharOperator(string str) => _multicharOperators.Contains(str);

		private string _code = "";
		private int _index = 0;
		private int _line = 1;
		private int _col = 1;
		private string _token = "";
		private List<Token> _tokens = [];

		private bool IsAtEnd => _index >= _code.Length;

		public List<Token> Scan(string code)
		{
			_code = code;
			_index = 0;
			_line = 1;
			_col = 1;
			_token = "";
			_tokens = [];

			Scan();

			return _tokens;
		}

		private void Scan()
		{
			while (!IsAtEnd)
			{
				_token = "";

				var tokenLine = _line;
				var tokenCol = _col;
				var ch = Advance();

				switch (ch)
				{
					case ' ' or '\t':
						break;

					case '\r' or '\n':
						ScanNewline(ch);
						break;

					case '#':
						ScanDirective(ch, tokenLine, tokenCol);
						break;

					case '(' or ')' or '{' or '}' or '[' or ']' or '.' or ',' or '?' or ':' or ';':
						ScanDelimiter(ch, tokenLine, tokenCol);
						break;

					case '+' or '-' or '*' or '/' or '<' or '>' or '=' or '|' or '&' or '%' or '^' or '~' or '!' or '@':
						ScanOperator(ch, tokenLine, tokenCol);
						break;

					case '$' or '%':
						ScanVariable(ch, tokenLine, tokenCol);
						break;

					case '\'' or '"':
						ScanString(ch, tokenLine, tokenCol);
						break;

					default:
						if (char.IsAsciiDigit(ch))
						{
							ScanNumber(ch, tokenLine);
						}
						else if (char.IsAsciiLetter(ch) || ch == '_')
						{
							ScanIdentifierOrKeyword(tokenLine);
						}
						else
						{
							throw new UnexpectedTokenException(ch, tokenLine, tokenCol);
						}

						break;
				}
			}
		}

		private void ScanNewline(char ch)
		{
			if (ch == '\r')
			{
				MatchAdvance('\n', append: false);
			}

			_line++;
			_col = 1;
		}

		private void ScanDirective(char ch, int line, int col)
		{
			if (MatchAny("#%!") || MatchIdentifierStart())
			{
				if (MatchAny("#%!") && !MatchIdentifierStart(offset: 1))
				{
					throw new UnexpectedTokenException(ch, line, col);
				}

				Advance();

				while (MatchIdentifierChar())
				{
					Advance();
				}
			}
			else if (MatchAny("{}@"))
			{
				Advance();
			}
			else
			{
				throw new UnexpectedTokenException(ch, line, col);
			}

			AddToken(_token[..2] switch
			{
				"#{" => TokenType.DirectiveCurlyLeft,
				"#}" => TokenType.DirectiveCurlyRight,
				"##" => TokenType.Directive,
				"#%" => TokenType.MacroArgument,
				"#@" => TokenType.MacroConcat,
				"#!" => TokenType.MacroKeyword,
				_ => TokenType.Macro,
			}, line);
		}

		private void ScanDelimiter(char ch, int line, int col)
		{
			TokenType type;

			if (ch == '.')
			{
				type = TokenType.Period;

				if (Match(".."))
				{
					type = TokenType.MacroVarArgs;
					Advance(amount: 2);
				}
			}
			else
			{
				type = ch switch
				{
					'(' => TokenType.ParenLeft,
					')' => TokenType.ParenRight,
					'{' => TokenType.CurlyLeft,
					'}' => TokenType.CurlyRight,
					'[' => TokenType.SquareLeft,
					']' => TokenType.SquareRight,
					'.' => TokenType.Period,
					',' => TokenType.Comma,
					'?' => TokenType.QuestionMark,
					':' => MatchAdvance(':') ? TokenType.ColonColon : TokenType.Colon,
					';' => TokenType.Semicolon,
					_ => throw new UnexpectedTokenException(ch, line, col),
				};
			}

			AddToken(type, line);
		}

		private void ScanOperator(char ch, int line, int col)
		{
			if (ch == '/' && MatchAny("/*"))
			{
				ScanComment(col);
			}
			else if (ch == '%' && MatchIdentifierStart())
			{
				ScanVariable(ch, line, col);
			}
			else
			{
				if (ch == '!' && Match("$="))
				{
					Advance(amount: 2);
				}
				else
				{
					if (Match(ch) && IsMulticharOperator($"{ch}{ch}"))
					{
						Advance();
					}

					if (Match('=') && IsMulticharOperator($"{_token}="))
					{
						Advance();
					}
				}

				AddToken(TokenType.Operator, line);
			}
		}

		private void ScanComment(int col)
		{
			if (Advance(append: false) == '*')
			{
				var matchingEnd = false;

				while (!IsAtEnd && !matchingEnd)
				{
					if (MatchAny("\r\n"))
					{
						ScanNewline(Advance(append: false));
					}
					else
					{
						matchingEnd = Match("*/");
						Advance(amount: matchingEnd ? 2 : 1, append: false);
					}
				}

				if (!matchingEnd)
				{
					throw new UnterminatedCommentException(_line, col);
				}
			}
			else
			{
				while (!IsAtEnd && !MatchAny("\r\n"))
				{
					Advance(append: false);
				}
			}
		}

		private void ScanVariable(char ch, int line, int col)
		{
			var type = TokenType.Variable;

			if (ch == '$' && MatchAdvance('='))
			{
				type = TokenType.Operator;
			}
			else if (MatchIdentifierStart())
			{
				Advance();

				var colons = "";
				var cursor = _index;

				while (!IsAtEnd && (MatchIdentifierChar() || Match(':')))
				{
					var next = Advance(append: false);

					if (next == ':')
					{
						colons += ':';
					}
					else
					{
						_token += $"{colons}{next}";

						colons = "";
						cursor = _index;
					}
				}

				// Rewind if the colon(s) do not have an identifier character after them -- they're not
				// part of the variable name.
				if (colons != "")
				{
					_index = cursor;
				}
			}
			else if (ch == '$')
			{
				throw new UnexpectedTokenException(ch, line, col);
			}

			AddToken(type, line);
		}

		private void ScanString(char quote, int line, int col)
		{
			var escapeChars = 0;
			var matchingQuote = false;

			while (!IsAtEnd && !matchingQuote)
			{
				var next = Advance();

				if (next == '\r' || next == '\n')
				{
					throw new UnexpectedEndOfLineException(line);
				}

				matchingQuote = next == quote && escapeChars % 2 == 0;

				if (!matchingQuote)
				{
					escapeChars = next == '\\' ? escapeChars + 1 : 0;
				}
			}

			if (!matchingQuote)
			{
				throw new UnterminatedStringException(line, col);
			}

			AddToken(TokenType.String, line);
		}

		private void ScanNumber(char ch, int line)
		{
			if (ch == '0' && MatchCaseInsensitive('x') && MatchHexDigit(offset: 1))
			{
				Advance();

				while (MatchHexDigit())
				{
					Advance();
				}
			}
			else
			{
				ScanDigits();

				if (Match('.') && MatchDigit(offset: 1))
				{
					Advance();
					ScanDigits();
				}

				if (MatchCaseInsensitive('e'))
				{
					if (MatchDigit(offset: 1))
					{
						Advance();
						ScanDigits();
					}
					else if (MatchAny("-+", offset: 1) && MatchDigit(offset: 2))
					{
						Advance(amount: 2);
						ScanDigits();
					}
				}
			}

			AddToken(TokenType.Number, line);
		}

		private void ScanDigits()
		{
			while (MatchDigit())
			{
				Advance();
			}
		}

		private void ScanIdentifierOrKeyword(int line)
		{
			while (MatchIdentifierChar())
			{
				Advance();
			}

			AddToken(_keywords.Contains(_token) ? TokenType.Keyword : TokenType.Identifier, line);
		}

		private char Advance(bool append = true)
		{
			char ch = _code[_index++];

			if (append)
			{
				_token += ch;
			}

			_col++;

			return ch;
		}

		private void Advance(int amount, bool append = true)
		{
			while (!IsAtEnd && amount-- > 0)
			{
				Advance(append);
			}
		}

		private char? Peek(int offset = 0)
		{
			var index = _index + offset;

			return index >= 0 && index < _code.Length ? _code[index] : null;
		}

		private bool Match(char ch, int offset = 0) => Peek(offset) == ch;

		private bool Match(string chars)
		{
			var offset = 0;

			foreach (var ch in chars)
			{
				if (!Match(ch, offset++))
				{
					return false;
				}
			}

			return true;
		}

		private bool Match(CharType type, int offset = 0)
		{
			var peek = Peek(offset);
			var ch = peek.GetValueOrDefault();

			return peek.HasValue && type switch
			{
				CharType.IdentifierStart => char.IsAsciiLetter(ch) || ch == '_',
				CharType.Identifier => char.IsAsciiLetterOrDigit(ch) || ch == '_',
				CharType.Digit => char.IsAsciiDigit(ch),
				CharType.HexDigit => char.IsAsciiHexDigit(ch),
				_ => false,
			};
		}

		private bool MatchIdentifierStart(int offset = 0) => Match(CharType.IdentifierStart, offset);
		private bool MatchIdentifierChar(int offset = 0) => Match(CharType.Identifier, offset);
		private bool MatchDigit(int offset = 0) => Match(CharType.Digit, offset);
		private bool MatchHexDigit(int offset = 0) => Match(CharType.HexDigit, offset);

		private bool MatchCaseInsensitive(char ch)
		{
			var peek = Peek();

			return peek.HasValue && char.ToLower(peek.Value) == char.ToLower(ch);
		}

		private bool MatchAny(string chars, int offset = 0) => chars.Any(ch => Match(ch, offset));

		private bool MatchAdvance(char ch, bool append = true)
		{
			var match = Match(ch);

			if (match)
			{
				Advance(append);
			}

			return match;
		}

		private void AddToken(TokenType type, int line)
		{
			_tokens.Add(new(type, _token, line));
			_token = "";
		}
	}
}
