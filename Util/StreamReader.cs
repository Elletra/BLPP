/**
 * StreamReader.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the BLPP source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using BLPP.Preprocessor;

namespace BLPP.Util
{
	public class StreamReader<T>(List<T> stream)
	{
		protected List<T> _stream = stream;
		public int Index { get; protected set; } = 0;

		public int Length => _stream.Count;
		public bool IsAtEnd => Index >= _stream.Count;

		public bool IsValidIndex(int index) => index >= 0 && index < _stream.Count;
		public bool IsValidOffset(int offset) => IsValidIndex(Index + offset);

		public T Read() => _stream[Index++];
		public void Advance(int amount = 1) => Index += amount;
		public T Peek(int offset = 0) => _stream[Index + offset];

		public bool Seek(int seekIndex)
		{
			if (!IsValidIndex(seekIndex))
			{
				return false;
			}

			Index = seekIndex;

			return true;
		}
	}

	public class TextStreamReader : StreamReader<char>
	{
		public TextStreamReader(List<char> stream) : base(stream) { }
		public TextStreamReader(string stream) : base([..stream]) { }

		public bool Match(char value, int offset = 0) => IsValidOffset(offset) && value == Peek(offset);

		public bool Match(string chars)
		{
			for (var i = 0; i < chars.Length; i++)
			{
				if (!Match(chars[i], i))
				{
					return false;
				}
			}

			return true;
		}

		public bool MatchAny(string chars) => chars.Any(ch => Match(ch));
		public bool MatchDigit() => !IsAtEnd && char.IsAsciiDigit(Peek());

		public bool MatchIdentifierStart(int offset = 0) => IsValidOffset(offset)
			&& (char.IsAsciiLetter(Peek(offset)) || Peek(offset) == '_');

		public bool MatchIdentifierChar() => !IsAtEnd
			&& (char.IsAsciiLetterOrDigit(Peek()) || Peek() == '_');
	}

	public class PreprocessorTokenReader(List<Token> stream) : StreamReader<Token>(stream)
	{
		public bool Match(TokenType type) => !IsAtEnd && Peek().Type == type;

		public Token Consume(params TokenType[] types) => Expect(advance: true, types);
		public Token Expect(params TokenType[] types) => Expect(advance: false, types);

		public Token Expect(bool advance, params TokenType[] types)
		{
			if (!types.Any(Match))
			{
				if (IsAtEnd)
				{
					throw new UnexpectedEndOfCodeException(_stream[^1]);
				}

				throw new UnexpectedTokenException(Peek());
			}

			return advance ? Read() : Peek();
		}
	}
}
