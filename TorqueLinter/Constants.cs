/**
 * Constants.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the TorqueLinter source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

namespace TorqueLinter
{
	static public class Constants
	{
		static public class Parser
		{
			public const string PACKAGE_TOKEN = "package";
			public const string FUNCTION_TOKEN = "function";
			public const string RETURN_TOKEN = "return";
			public const string WHILE_TOKEN = "while";
			public const string FOR_TOKEN = "for";
			public const string BREAK_TOKEN = "break";
			public const string CONTINUE_TOKEN = "continue";
			public const string IF_TOKEN = "if";
			public const string ELSE_TOKEN = "else";
			public const string SWITCH_TOKEN = "switch";
			public const string SWITCH_STRING_TOKEN = "switch$";
			public const string CASE_TOKEN = "case";
			public const string OR_TOKEN = "or";
			public const string DEFAULT_TOKEN = "default";
			public const string DATABLOCK_TOKEN = "datablock";
			public const string NEW_TOKEN = "new";

			public const string STR_NOT_EQ_TOKEN = "!$=";
			public const string SHL_ASSIGN_TOKEN = "<<=";
			public const string SHR_ASSIGN_TOKEN = ">>=";
			public const string STR_EQUAL_TOKEN = "$=";
			public const string ADD_ASSIGN_TOKEN = "+=";
			public const string SUB_ASSIGN_TOKEN = "-=";
			public const string MUL_ASSIGN_TOKEN = "*=";
			public const string DIV_ASSIGN_TOKEN = "/=";
			public const string MOD_ASSIGN_TOKEN = "%=";
			public const string EQUAL_TOKEN = "==";
			public const string NOT_EQUAL_TOKEN = "!=";
			public const string LT_EQUAL_TOKEN = "<=";
			public const string GT_EQUAL_TOKEN = ">=";
			public const string BIT_OR_ASSIGN_TOKEN = "|=";
			public const string BIT_AND_ASSIGN_TOKEN = "&=";
			public const string BIT_XOR_TOKEN = "^=";
			public const string INCREMENT_TOKEN = "++";
			public const string DECREMENT_TOKEN = "--";
			public const string SHL_TOKEN = "<<";
			public const string SHR_TOKEN = ">>";
			public const string LOGIC_OR_TOKEN = "||";
			public const string LOGIC_AND_TOKEN = "&&";

			public const string CONCAT_SPACE_TOKEN = "SPC";
			public const string CONCAT_TAB_TOKEN = "TAB";
			public const string CONCAT_NEWLINE_TOKEN = "NL";
		}
	}
}
