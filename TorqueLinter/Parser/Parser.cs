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

		private Node ExpectStatement(bool topLevel = true)
		{
			var peek = !_stream.IsAtEnd ? _stream.Peek() : throw new UnexpectedEndOfCodeException(_stream.Peek(-1).Line);
			var statement = ParseStatement(peek, topLevel);

			return statement ?? throw new SyntaxException(peek.Line, peek.Col, "Expected statement");
		}

		private Node? ParseStatement(Token token, bool topLevel = true)
		{
			if (token.IsDelimiter)
			{
				return null;
			}

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
							throw new SyntaxException(token.Line, "Functions cannot be part of another statement (except packages)");
						}

						return ParseFunction(token);
					}

					case WHILE_TOKEN:
						return ParseWhileLoop(token);

					case FOR_TOKEN:
						return ParseForLoop(token);

					case RETURN_TOKEN:
						return ParseReturn(token);

					default:
						throw new UnexpectedTokenException(token.Line, token.Value);
				}
			}

			return ExpectExpressionStatement();
		}

		private Node ExpectExpressionStatement()
		{
			var peek = _stream.Peek();
			var node = ParseExpression();

			// TODO: Function calls and object declarations are also expression statements.
			if (node is not AssignmentNode && node is not IncrementDecrementNode)
			{
				throw new SyntaxException(peek.Line, peek.Col, "Expected statement");
			}

			_stream.Consume(TokenType.Semicolon);

			return node;
		}

		private Node ParseExpression(TokenType delimiter)
		{
			var expression = ParseExpression();

			_stream.Consume(delimiter);

			return expression;
		}

		private Node ParseExpression()
		{
			Node? expression = null;

			var parentheses = 0;
			var stack = new Stack<Node>();

			while (!_stream.IsAtEnd && expression == null)
			{
				var token = _stream.Read();
				var type = token.Type;
				var value = token.Value;

				switch (type)
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

					case TokenType.IncrementDecrement:
						stack.Push(new IncrementDecrementNode(token, stack.Pop()));
						break;

					case TokenType.Operator or TokenType.Assignment:
					{
						if (stack.Count > 0)
						{
							var left = stack.Pop();
							var right = ParseExpression();

							stack.Push(type == TokenType.Operator ? new BinaryNode(token, left, right) : new AssignmentNode(token, left, right));
						}
						else if (value == SUB_TOKEN || value == LOGIC_NOT_TOKEN || value == BIT_NOT_TOKEN)
						{
							stack.Push(new UnaryNode(token, ParseExpression()));
						}
						else
						{
							throw new UnexpectedTokenException(token.Line, value);
						}

						break;
					}

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
						if (--parentheses < 0)
						{
							expression = stack.Pop();
							_stream.Seek(_stream.Index - 1);
						}

						break;
					}

					case TokenType.Keyword:
						// "break" can be both an identifier and a statement in TorqueScript, for some reason.
						if (token.Value != "break")
						{
							throw new UnexpectedTokenException(token.Line, token.Value);
						}

						stack.Push(new IdentifierNode(token));
						break;

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

		private WhileLoopNode ParseWhileLoop(Token token)
		{
			_stream.ConsumeKeyword(WHILE_TOKEN);
			_stream.Consume(TokenType.ParenLeft);

			var node = new WhileLoopNode(token, ParseExpression());

			_stream.Consume(TokenType.ParenRight);

			var brackets = _stream.AdvanceIfMatch(TokenType.CurlyLeft);

			if (brackets)
			{
				node.Body = ParseStatementList(topLevel: false);

				_stream.Consume(TokenType.CurlyRight);
			}
			else
			{
				node.Body = [ExpectStatement(topLevel: false)];
			}

			return node;
		}

		private ForLoopNode ParseForLoop(Token token)
		{
			_stream.ConsumeKeyword(FOR_TOKEN);
			_stream.Consume(TokenType.ParenLeft);

			var init = ParseExpression(TokenType.Semicolon);
			var test = ParseExpression(TokenType.Semicolon);
			var end = ParseExpression(TokenType.ParenRight);

			var node = new ForLoopNode(token, init, test, end);
			var brackets = _stream.AdvanceIfMatch(TokenType.CurlyLeft);

			if (brackets)
			{
				node.Body = ParseStatementList(topLevel: false);

				_stream.Consume(TokenType.CurlyRight);
			}
			else
			{
				node.Body = [ExpectStatement(topLevel: false)];
			}

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
