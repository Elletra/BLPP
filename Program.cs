/**
 * Program.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the TorqueSharp source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using TorqueSharp.Lexer;

foreach (var token in new Lexer().Scan(File.ReadAllText("../../test.cs")))
{
	Console.WriteLine($"{token.Type}: {token.Value}");
}
