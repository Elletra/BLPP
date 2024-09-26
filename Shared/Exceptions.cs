namespace Shared
{
	public class FileExtensionException(string file, string ext) : Exception($"File \"{file}\" does not have a '{ext}' extension") { }

	public class UnexpectedTokenException(int line, string token) : Exception($"Unexpected token `{token}` on line {line}")
	{
		public UnexpectedTokenException(int line, char ch) : this(line, $"{ch}") { }
	}

	public class UnexpectedEndOfLineException(int line) : Exception($"Unexpected end of line on line {line}") { }
	public class UnexpectedEndOfCodeException(int line) : Exception($"Unexpected end of code on line {line}") { }
	public class UnterminatedStringException(int line) : Exception($"Unterminated string at line {line}") { }
	public class UnterminatedCommentException(int line) : Exception($"Unterminated comment at line {line}") { }
	public class SyntaxException(int line, string message) : Exception($"Syntax error on line {line}: {message}") { }
}
