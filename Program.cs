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

var filePath = "../../test.blcs";
var code = File.ReadAllText(filePath);

var lexer = new Lexer();
var parser = new DirectiveParser();
var processor = new DirectiveProcessor();

var tokens = lexer.Scan(code);
var data = parser.Parse(tokens);

var processed = processor.Process(tokens, data.Macros);

var line = 1;

foreach (var token in processed)
{
	for (var i = 0; i < token.Line - line; i++)
	{
		Console.WriteLine("");
	}

	Console.Write($"{token.WhitespaceBefore}{token.Value}");

	line = token.Line;
}
