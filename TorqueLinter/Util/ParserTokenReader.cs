/**
 * ParserTokenReader.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the TorqueLinter source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using TorqueLinter.Lexer;
using Shared;
using Shared.Util;

namespace TorqueLinter.Util
{
	public class ParserTokenReader(List<Token> stream) : StreamReader<Token>(stream)
	{
		public bool Match(TokenType type) => !IsAtEnd && Peek().Type == type;
		public bool Match(TokenType type, int offset) => IsValidOffset(offset) && Peek(offset).Type == type;

		public bool Match(params TokenType[] types)
		{
			for (var i = 0; i < types.Length; i++)
			{
				if (!Match(types[i], i))
				{
					return false;
				}
			}

			return true;
		}

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

		public Token ConsumeKeyword(string value)
		{
			var token = Consume(TokenType.Keyword);

			if (token.Value != value)
			{
				throw new SyntaxException(token.Line, $"Expected `{value}`, got `{token.Value}`");
			}

			return token;
		}

		public Token Expect(params TokenType[] types) => Expect(advance: false, types);

		public Token Expect(bool advance, params TokenType[] types)
		{
			if (!types.Any(Match))
			{
				if (IsAtEnd)
				{
					throw new UnexpectedEndOfCodeException(Stream[^1].Line);
				}

				throw new UnexpectedTokenException(Peek().Line, Peek().Value);
			}

			return advance ? Read() : Peek();
		}
	}
}
