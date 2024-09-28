/**
 * Parser.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the TorqueLinter source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using Shared;
using TorqueLinter.AST;
using TorqueLinter.Lexer;
using TorqueLinter.Util;

using static TorqueLinter.Constants.Parser;

namespace TorqueLinter.Parser
{
	public class Parser
	{
		private ParserTokenReader _stream = new([]);

		public List<Node> Parse(List<Token> tokens)
		{
			_stream = new(tokens);

			return ParseStatementList();
		}

		private List<Node> ParseStatementList(bool topLevel = true)
		{
			var list = new List<Node>();

			while (!_stream.IsAtEnd)
			{
				var statement = ParseStatement(_stream.Peek(), topLevel);

				if (statement == null)
				{
					break;
				}

				list.Add(statement);
			}

			return list;
		}

		private Node? ParseStatement(Token token, bool topLevel = true)
		{
			if (token.Type == TokenType.Keyword)
			{
				switch (token.Value)
				{
					case PACKAGE_TOKEN:
						return ParsePackage(token);

					case FUNCTION_TOKEN:
					{
						if (!topLevel)
						{
							throw new SyntaxException(token.Line, "Functions cannot be part of another statement other than packages");
						}

						return ParseFunction(token);
					}

					case RETURN_TOKEN:
						return ParseReturn(token);

					default:
						throw new UnexpectedTokenException(token.Line, token.Value);
				}
			}

			return null;
		}

		private Node ParseExpression()
		{
			Node? expression = null;

			var parentheses = 0;
			var stack = new Stack<Node>();

			while (!_stream.IsAtEnd && expression == null)
			{
				var token = _stream.Read();

				switch (token.Type)
				{
					case TokenType.Variable:
						stack.Push(new VariableNode(token));
						break;

					case TokenType.String:
						stack.Push(new StringNode(token));
						break;

					case TokenType.Integer:
						stack.Push(new IntegerNode(token));
						break;

					case TokenType.Float:
						stack.Push(new FloatNode(token));
						break;

					case TokenType.Identifier:
						if (_stream.Match(TokenType.ColonColon, TokenType.Identifier, TokenType.ParenLeft))
						{
							throw new NotImplementedException("TODO: ParseFunctionCall()");
						}
						else
						{
							stack.Push(new IdentifierNode(token));
						}

						break;

					case TokenType.QuestionMark:
					{
						var test = stack.Pop();
						var @true = ParseExpression();

						_stream.Consume(TokenType.Colon);

						expression = new TernaryNode(token)
						{
							Test = test,
							True = @true,
							False = ParseExpression(),
						};

						break;
					}

					case TokenType.ParenLeft:
						if (stack.Count > 0)
						{
							throw new NotImplementedException("TODO: ParseFunctionCall()");
						}
						else
						{
							parentheses++;
						}

						break;

					case TokenType.ParenRight:
					{
						if (parentheses >= 0)
						{
							parentheses--;
						}
						else
						{
							expression = stack.Pop();
						}

						break;
					}

					default:
						throw new UnexpectedTokenException(token.Line, token.Value);
				}

				if (!_stream.IsAtEnd && _stream.Peek().IsExpressionEnd)
				{
					break;
				}
			}

			expression ??= stack.Pop();

			if (_stream.IsAtEnd)
			{
				throw new UnexpectedEndOfCodeException(_stream.Stream[^1].Line);
			}

			if (stack.Count > 0)
			{
				throw new Exception("Fatal error: Expression stack is not empty!");
			}

			return expression;
		}

		private PackageNode ParsePackage(Token token)
		{
			_stream.ConsumeKeyword(PACKAGE_TOKEN);

			var node = new PackageNode(token, _stream.Consume(TokenType.Identifier).Value);

			while (!_stream.Match(TokenType.ParenRight))
			{
				node.Functions.Add(ParseFunction(_stream.Peek()));
			}

			_stream.Consume(TokenType.ParenRight);
			_stream.Consume(TokenType.Semicolon);

			return node;
		}

		private FunctionNode ParseFunction(Token token)
		{
			_stream.ConsumeKeyword(FUNCTION_TOKEN);

			var next = _stream.Consume(TokenType.Identifier);
			var name = next.Value;
			string? @namespace = null;

			// Check if it has a namespace
			if (_stream.AdvanceIfMatch(TokenType.ColonColon))
			{
				@namespace = name;
				name = _stream.Consume(TokenType.Identifier).Value;
			}

			var node = new FunctionNode(token, @namespace, name);

			_stream.Consume(TokenType.ParenLeft);

			while (!_stream.Match(TokenType.ParenRight))
			{
				if (node.Arguments.Count > 0)
				{
					_stream.Consume(TokenType.Comma);
				}

				node.Arguments.Add(_stream.Consume(TokenType.Variable).Value);
			}

			_stream.Consume(TokenType.ParenRight);
			_stream.Consume(TokenType.CurlyLeft);

			node.Body = ParseStatementList(topLevel: false);

			_stream.Consume(TokenType.CurlyRight);

			return node;
		}

		private ReturnNode ParseReturn(Token token)
		{
			_stream.ConsumeKeyword(RETURN_TOKEN);

			ReturnNode node = _stream.Match(TokenType.Semicolon) ? new(token) : new(token, ParseExpression());

			_stream.Consume(TokenType.Semicolon);

			return node;
		}
	}
}
