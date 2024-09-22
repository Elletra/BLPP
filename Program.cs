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

var preprocessor = new Preprocessor();

try
{
	preprocessor.PreprocessFile("../../test3.blcs");
}
catch (Exception exception)
{
	Console.WriteLine($"[ERROR] {exception.Message}");
}
