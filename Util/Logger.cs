/**
 * Logger.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the BLPP source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using BLPP.Preprocessor;

namespace BLPP.Util
{
	static public class Logger
	{
		static public bool Silent = false;

		static public void LogError(string message, bool indented = false) => LogMessage($"{(indented ? "\t" : "")}[ERROR] {message}", ConsoleColor.DarkRed);
		static public void LogWarning(string message) => LogMessage(message, ConsoleColor.Yellow);

		static public void LogMessage(string message, ConsoleColor textColor)
		{
			var prev = Console.ForegroundColor;

			Console.ForegroundColor = textColor;
			LogMessage(message);
			Console.ForegroundColor = prev;
		}

		static public void LogMessage(string message)
		{
			if (!Silent)
			{
				Console.WriteLine(message);
			}
		}

		static public void LogHeader()
		{
			LogMessage($"### Blockland Preprocessor v{Constants.Preprocessor.VERSION} (by {Constants.Preprocessor.AUTHOR}) ###\n", ConsoleColor.White);
		}
	}
}
