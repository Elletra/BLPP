/**
 * Lexer.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the BLPP source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using Shared;
using Shared.Util;

namespace BLPP.Preprocessor
{
	public enum TokenType : byte
	{
		Identifier,
		String, Number,
		Punctuation,
		ParenLeft, ParenRight, Comma,
		Directive, DirectiveCurlyLeft, DirectiveCurlyRight,
		Macro, MacroVarArgs, MacroParameter, MacroConcat, MacroKeyword,
		Invalid,
	}

	public class Token(TokenType type, string value, int line, string whitespaceBefore = "")
	{
		public TokenType Type { get; } = type;
		public string Value { get; set; } = value;
		public int Line { get; } = line;
		public string WhitespaceBefore { get; set; } = whitespaceBefore;

		public Token(Token copy, int line) : this(copy.Type, copy.Value, line, copy.WhitespaceBefore) { }

		public bool IsValid => Type < TokenType.Invalid;
		public string MacroName => Type == TokenType.Macro ? Value[1..] : Value;
		public string ParameterName => Type == TokenType.MacroParameter ? Value[2..] : Value;
		public bool HasWhitespaceBefore => WhitespaceBefore.Length > 0;

		/// <summary>
		/// Whether or not this token can be used in a macro definition body.
		/// </summary>
		public bool IsValidMacroBodyToken => Type switch
		{
			TokenType.Directive => false,
			TokenType.DirectiveCurlyLeft => false,
			TokenType.DirectiveCurlyRight => false,
			TokenType.MacroVarArgs => false,
			TokenType.Invalid => false,
			_ => true,
		};

		public bool IsPreprocessorToken => Type switch
		{
			TokenType.Directive => true,
			TokenType.DirectiveCurlyLeft => true,
			TokenType.DirectiveCurlyRight => true,
			TokenType.Macro => true,
			TokenType.MacroVarArgs => true,
			TokenType.MacroParameter => true,
			TokenType.MacroConcat => true,
			TokenType.MacroKeyword => true,
			_ => false,
		};

		public bool IsVariadicMacroKeyword => Type == TokenType.MacroKeyword && Value switch
		{
			Constants.Tokens.MACRO_KEYWORD_VARG_COUNT => true,
			Constants.Tokens.MACRO_KEYWORD_VARGS => true,
			Constants.Tokens.MACRO_KEYWORD_VARGS_PREPEND => true,
			_ => false,
		};

		public bool IsValidMacroKeyword => Type == TokenType.MacroKeyword && Value switch
		{
			Constants.Tokens.MACRO_KEYWORD_LINE => true,
			Constants.Tokens.MACRO_KEYWORD_VARG_COUNT =>  true,
			Constants.Tokens.MACRO_KEYWORD_VARGS =>  true,
			Constants.Tokens.MACRO_KEYWORD_VARGS_PREPEND => true,
			_ => false,
		};
	}

	/// <summary>
	/// This class converts a string of code into tokens for the preprocessor to use.
	/// </summary>
	public class Lexer
	{
		private TextStreamReader _stream = new("");
		private int _line = 1;
		private string _whitespace = "";
		private List<Token> _tokens = [];

		public List<Token> Scan(string code)
		{
			_stream = new(code);
			_line = 1;
			_whitespace = "";
			_tokens = [];

			Scan();

			return _tokens;
		}

		private void Scan()
		{
			while (!_stream.IsAtEnd)
			{
				var ch = _stream.Read();
				var clearWhitespace = true;

				switch (ch)
				{
					case ' ' or '\t':
						_whitespace += ch;
						clearWhitespace = false;
						break;

					case '\r' or '\n':
						ScanNewline(ch);
						break;

					case '#':
						ScanDirective(ch);
						break;

					case '(' or ')' or ',':
						AddToken(ch switch
						{
							'(' => TokenType.ParenLeft,
							')' => TokenType.ParenRight,
							',' => TokenType.Comma,
							_ => TokenType.Invalid,
						}, ch);

						break;

					case '\'' or '"':
						ScanString(ch);
						break;

					case '{' or '}' or '[' or ']' or '.' or '?' or ':' or ';'
						or '+' or '-' or '*' or '/' or '<' or '>' or '=' or '|' or '&' or '^' or '@'
						or '~' or '!'
						or '$' or '%':
						ScanPunctuation(ch);
						break;

					default:
						if (char.IsAsciiDigit(ch))
						{
							ScanNumber(ch);
						}
						else if (char.IsAsciiLetter(ch) || ch == '_')
						{
							ScanIdentifier(ch);
						}
						else
						{
							throw new UnexpectedTokenException(_line, ch);
						}

						break;
				}

				if (clearWhitespace)
				{
					_whitespace = "";
				}
			}
		}

		private void ScanNewline(char ch)
		{
			if (ch == '\r' && _stream.Match('\n'))
			{
				_stream.Advance();
			}

			_line++;
		}

		private void ScanDirective(char ch)
		{
			var value = $"{ch}";

			if (_stream.MatchAny("#%!") || _stream.MatchIdentifierStart())
			{
				if (_stream.MatchAny("#%!") && !_stream.MatchIdentifierStart(offset: 1))
				{
					throw new UnexpectedTokenException(_line, ch);
				}

				value += _stream.Read();

				while (_stream.MatchIdentifierChar())
				{
					value += _stream.Read();
				}
			}
			else if (_stream.MatchAny("{}@"))
			{
				value += _stream.Read();
			}
			else
			{
				throw new UnexpectedTokenException(_line, ch);
			}

			AddToken(value[..2] switch
			{
				"#{" => TokenType.DirectiveCurlyLeft,
				"#}" => TokenType.DirectiveCurlyRight,
				"##" => TokenType.Directive,
				"#%" => TokenType.MacroParameter,
				"#@" => TokenType.MacroConcat,
				"#!" => TokenType.MacroKeyword,
				_ => TokenType.Macro,
			}, value);
		}

		private void ScanString(char quote)
		{
			var str = $"{quote}";
			var escapeChars = 0;
			var matchingQuote = false;

			while (!_stream.IsAtEnd && !matchingQuote)
			{
				var next = _stream.Read();

				if (next == '\r' || next == '\n')
				{
					throw new UnexpectedEndOfLineException(_line);
				}

				matchingQuote = next == quote && escapeChars % 2 == 0;

				if (!matchingQuote)
				{
					escapeChars = next == '\\' ? escapeChars + 1 : 0;
				}

				str += next;
			}

			if (!matchingQuote)
			{
				throw new UnterminatedStringException(_line);
			}

			AddToken(TokenType.String, str);
		}

		private void ScanPunctuation(char ch)
		{
			if (ch == '/' && _stream.MatchAny("/*"))
			{
				ScanComment();
			}
			else
			{
				var type = TokenType.Punctuation;
				var value = $"{ch}";

				if (ch == '.' && _stream.Match(".."))
				{
					type = TokenType.MacroVarArgs;
					value += $"{_stream.Read()}{_stream.Read()}";
				}

				AddToken(type, value);
			}
		}

		private void ScanComment()
		{
			if (_stream.Read() == '*')
			{
				var matchingEnd = false;

				while (!_stream.IsAtEnd && !matchingEnd)
				{
					if (_stream.MatchAny("\r\n"))
					{
						ScanNewline(_stream.Read());
					}
					else
					{
						matchingEnd = _stream.Match("*/");
						_stream.Advance(amount: matchingEnd ? 2 : 1);
					}
				}

				if (!matchingEnd)
				{
					throw new UnterminatedCommentException(_line);
				}
			}
			else
			{
				while (!_stream.IsAtEnd && !_stream.MatchAny("\r\n"))
				{
					_stream.Advance();
				}
			}
		}

		private void ScanNumber(char ch)
		{
			var value = $"{ch}";

			while (_stream.MatchDigit())
			{
				value += _stream.Read();
			}

			AddToken(TokenType.Number, value);
		}

		private void ScanIdentifier(char ch)
		{
			var value = $"{ch}";

			while (_stream.MatchIdentifierChar())
			{
				value += _stream.Read();
			}

			AddToken(TokenType.Identifier, value);
		}

		private Token AddToken(TokenType type, string value)
		{
			var token = new Token(type, value, _line, _whitespace);

			_tokens.Add(token);

			return token;
		}

		private Token AddToken(TokenType type, char ch) => AddToken(type, $"{ch}");
	}
}
