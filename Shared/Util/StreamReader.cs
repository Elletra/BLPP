﻿/**
 * StreamReader.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the TorqueSharp source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

namespace Shared.Util
{
	public class StreamReader<T>(List<T> stream)
	{
		public List<T> Stream { get; protected set; } = stream;
		public int Index { get; protected set; } = 0;

		public int Length => Stream.Count;
		public bool IsAtEnd => Index >= Stream.Count;

		public bool IsValidIndex(int index) => index >= 0 && index < Stream.Count;
		public bool IsValidOffset(int offset) => IsValidIndex(Index + offset);

		public virtual T Read() => Stream[Index++];
		public virtual void Advance() => Index++;
		public virtual T Peek(int offset = 0) => Stream[Index + offset];

		public bool Seek(int seekIndex)
		{
			if (!IsValidIndex(seekIndex))
			{
				return false;
			}

			Index = seekIndex;

			return true;
		}

		public void Rewind() => Seek(0);
	}

	public class TextStreamReader : StreamReader<char>
	{
		public int Line { get; set; } = 1;
		public int Col { get; set; } = 1;

		public TextStreamReader(List<char> stream) : base(stream) { }
		public TextStreamReader(string stream) : base([..stream]) { }

		private void UpdateLineAndColumn()
		{
			if (Match('\r'))
			{
				Line++;
				Col = 1;
			}
			else if (Match('\n'))
			{
				if (!Match('\r', offset: -1))
				{
					Line++;
					Col = 1;
				}
			}
			else
			{
				Col++;
			}
		}

		public override void Advance()
		{
			UpdateLineAndColumn();
			base.Advance();
		}

		public override char Read()
		{
			UpdateLineAndColumn();
			return base.Read();
		}

		public void Advance(int amount)
		{
			for (var i = 0; i < amount; i++)
			{
				Advance();
			}
		}

		public bool Match(char value, int offset = 0) => IsValidOffset(offset) && value == Peek(offset);

		public bool Match(string chars, int offset = 0)
		{
			for (var i = 0; i < chars.Length; i++)
			{
				if (!Match(chars[i], i + offset))
				{
					return false;
				}
			}

			return true;
		}

		public bool AdvanceIfMatch(char value)
		{
			var matched = Match(value);

			if (matched)
			{
				Advance();
			}

			return matched;
		}

		public bool MatchAny(string chars, int offset = 0) => chars.Any(ch => Match(ch, offset));
		public bool MatchDigit(int offset = 0) => IsValidOffset(offset) && char.IsAsciiDigit(Peek(offset));
		public bool MatchHexDigit(int offset = 0) => IsValidOffset(offset) && char.IsAsciiHexDigit(Peek(offset));

		public bool MatchIdentifierStart(int offset = 0) => IsValidOffset(offset)
			&& (char.IsAsciiLetter(Peek(offset)) || Peek(offset) == '_');

		public bool MatchIdentifierChar(int offset = 0) => IsValidOffset(offset)
			&& (char.IsAsciiLetterOrDigit(Peek(offset)) || Peek(offset) == '_');
	}
}
