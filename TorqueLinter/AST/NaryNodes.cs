/**
 * NaryNodes.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the TorqueLinter source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using TorqueLinter.Lexer;

namespace TorqueLinter.AST
{
	public class UnaryNode(Token operatorToken, Node value) : Node(operatorToken)
	{
		public readonly string Operator = operatorToken.Value;
		public readonly Node Value = value;
	}

	public class BinaryNode(Token operatorToken, Node left, Node right) : Node(left.StartLine, left.StartCol)
	{
		public readonly string Operator = operatorToken.Value;
		public readonly Node Left = left;
		public readonly Node Right = right;
	}

	public class TernaryNode(Token startToken) : Node(startToken)
	{
		public Node? Test { get; set; } = null;
		public Node? True { get; set; } = null;
		public Node? False { get; set; } = null;
	}

	public class IncrementDecrementNode(Token token, Node left) : UnaryNode(token, left) { }
	public class AssignmentNode(Token token, Node left, Node right) : BinaryNode(token, left, right) { }
}
