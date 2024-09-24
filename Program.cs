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
using BLPP.Util;

Console.Title = $"Blockland Preprocessor ({Constants.Preprocessor.VERSION})";

var errorCode = 0;
var exitImmediately = true;

try
{
	var (error, options) = CommandLineParser.Parse(args);
	exitImmediately = options.CommandLineMode;

	if (error)
	{
		errorCode = 1;
	}
	else
	{
		Logger.Quiet = options.Quiet;

		if (!options.CommandLineMode)
		{
			Logger.LogHeader();
		}

		new Preprocessor().Preprocess(options);
	}
}
catch (Exception exception)
{
	Logger.LogMessage(exception.Message);
	Logger.LogMessage(exception.StackTrace ?? "");

	errorCode = 1;
}

if (!exitImmediately)
{
	// The input is "redirected" when the program is called from a terminal.
	var redirected = Console.IsInputRedirected;

	if (redirected)
	{
		Console.WriteLine("\nPress enter key to exit...\n");
	}
	else
	{
		Console.WriteLine("\nPress any key to exit...\n");
	}

	while (true)
	{
		if (redirected)
		{
			if (Console.In.Peek() >= 0)
			{
				break;
			}
		}
		else if (Console.KeyAvailable && Console.ReadKey(true).Key != ConsoleKey.None)
		{
			break;
		}
	}
}

return errorCode;
