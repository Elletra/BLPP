/**
 * Program.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the TorqueLinter source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using TorqueLinter.Lexer;

var code = File.ReadAllText("./test.cs");

foreach (var token in new Lexer().Scan(code))
{
	Console.WriteLine("{0,-24} {1,-24} {2,-24} {3,-24}", token.Type, token.Value, token.Line, token.Col);
}
