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

var errorCode = 0;

try
{
	var (error, options) = CommandLineParser.Parse(args);

	if (error)
	{
		errorCode = 1;
	}
	else
	{
		Logger.Silent = options.Silent;
		Logger.LogHeader();

		new Preprocessor().Preprocess(options);
	}
}
catch (Exception exception)
{
	Logger.LogMessage(exception.Message);
	Logger.LogMessage(exception.StackTrace ?? "");

	errorCode = 1;
}

return errorCode;
