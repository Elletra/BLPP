/**
 * Exceptions.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the BLPP source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using BLPP.Preprocessor;

namespace BLPP
{
	public class UndefinedMacroException(Token token) : Exception($"Undefined macro `{token.MacroName}` on line {token.Line}") { }
	public class UndefinedMacroParameterException(Token token) : Exception($"Undefined macro parameter `{token.ParameterName}` on line {token.Line}") { }
	public class MultipleDefinitionsException(string name) : Exception($"Found multiple macro definitions for '{name}'")
	{
		public MultipleDefinitionsException(Token token) : this(token.Value) { }
		public MultipleDefinitionsException(Macro macro) : this(macro.Name) { }
	}
}
