/**
 * IfNode.cs
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
	public class IfNode(Token startToken, Node test) : Node(startToken)
	{
		public Node? Test = test;

		public List<Node> Body { get; set; } = [];
		public List<Node> Else { get; set; } = [];

		public bool HasElse => Else.Count > 0;
	}
}
