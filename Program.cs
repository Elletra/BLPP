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

foreach (var token in tokens)
{
	Console.WriteLine("{0,-24} {1,-24} {2,-24}", token.Type, token.WhitespaceBefore.Length, token.Value);
}

new MacroProcessor().Process(tokens);
