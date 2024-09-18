/**
 * Program.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the BLPP source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using BLPP.Lexer;
using BLPP.Preprocessor;

var code = File.ReadAllText("../../test.cs");
var tokens = new Lexer().Scan(code);

try
{
	new Preprocessor().Process(tokens);
}
catch (Exception except)
{
	Console.WriteLine($"[ERROR] {except.Message}");
}
