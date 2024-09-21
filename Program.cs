/**
 * Program.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the BLPP source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using BLPP.Preprocessor;

var code = File.ReadAllText("../../test.blcs");
var tokens = new Lexer().Preprocess(code);

new MacroExpander().Expand(tokens, new DirectiveProcessor().Process(tokens));

var line = 1;

foreach (var token in tokens)
{
	for (var i = 0; i < token.Line - line; i++)
	{
		Console.WriteLine("");
	}

	Console.Write($"{token.WhitespaceBefore}{token.Value}");

	line = token.Line;
}
