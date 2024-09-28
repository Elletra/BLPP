/**
 * FunctionNode.cs
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
	public class PackageNode(Token startToken, string name) : Node(startToken)
	{
		public readonly string Name = name;
		public readonly List<Node> Functions = [];
	}

	public class FunctionNode(Token startToken, string? @namespace, string name) : Node(startToken)
	{
		public readonly string? Namespace = @namespace;
		public readonly string Name = name;
		public readonly List<string> Arguments = [];
		public List<Node> Body { get; set; } = [];

		public bool HasNamespace => Namespace != null;
	}
}
