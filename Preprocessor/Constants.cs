﻿/**
 * Constants.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the BLPP source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

namespace BLPP.Preprocessor
{
	static public class Constants
	{
		static public class Tokens
		{
			public const string DIRECTIVE_BLCS = "##blcs";
			public const string DIRECTIVE_DEFINE = "##define";
			public const string DIRECTIVE_USE = "##use";

			public const string MACRO_VAR_ARGS = "...";

			public const string MACRO_KEYWORD_LINE = "#!line";
			public const string MACRO_KEYWORD_VARG_COUNT = "#!vargc";
			public const string MACRO_KEYWORD_VARGS = "#!vargs";
			public const string MACRO_KEYWORD_VARGS_PREPEND = "#!vargsp";
		}

		static public class Preprocessor
		{
			public const string FILE_EXTENSION = ".blcs";
			public const string VERSION = "0.3.3";
			public const string AUTHOR = "Elletra";
			public const string FILE_TOP_COMMENT = $"// ** File generated by the Blockland Preprocessor (version {VERSION}) **";
			public const string FILE_BOTTOM_COMMENT = "// ** End of generated file **";
		}
	}
}
