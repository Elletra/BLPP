/**
 * StreamReader.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the BLPP source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using BLPP.Preprocessor;
using Shared.Util;

namespace BLPP.Util
{
	public class PreprocessorTokenReader(List<Token> stream) : StreamReader<Token>(stream)
	{
		public bool Match(TokenType type) => !IsAtEnd && Peek().Type == type;
		public bool MatchLine(Token token) => MatchLine(token.Line);
		public bool MatchLine(int line) => !IsAtEnd && Peek().Line == line;

		public bool AdvanceIfMatch(TokenType type)
		{
			var matched = Match(type);

			if (matched)
			{
				Advance();
			}

			return matched;
		}

		public Token Consume(params TokenType[] types) => Expect(advance: true, types);
		public Token Expect(params TokenType[] types) => Expect(advance: false, types);

		public Token Expect(bool advance, params TokenType[] types)
		{
			if (!types.Any(Match))
			{
				if (IsAtEnd)
				{
					throw new UnexpectedEndOfCodeException(Stream[^1]);
				}

				throw new UnexpectedTokenException(Peek());
			}

			return advance ? Read() : Peek();
		}

		public void Insert(int index, IEnumerable<Token> tokens) => Stream.InsertRange(index, tokens);
		public void Remove(int index, int count) => Stream.RemoveRange(index, count);
	}
}
