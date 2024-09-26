namespace Shared
{
	public class FileExtensionException(string file, string ext) : Exception($"File \"{file}\" does not have a '{ext}' extension") { }

	public class UnexpectedTokenException : Exception
	{
		public UnexpectedTokenException(int line, string token) : base($"Unexpected token `{token}` on line {line}") { }
		public UnexpectedTokenException(int line, char ch) : this(line, $"{ch}") { }
		public UnexpectedTokenException(int line, int col, string token) : base($"Unexpected token `{token}` on line {line}, col {col}") { }
		public UnexpectedTokenException(int line, int col, char ch) : this(line, col, $"{ch}") { }
	}

	public class UnexpectedEndOfLineException(int line) : Exception($"Unexpected end of line on line {line}") { }
	public class UnexpectedEndOfCodeException(int line) : Exception($"Unexpected end of code on line {line}") { }
	public class UnterminatedStringException(int line) : Exception($"Unterminated string on line {line}") { }
	public class UnterminatedCommentException(int line) : Exception($"Unterminated comment on line {line}") { }

	public class SyntaxException : Exception
	{
		public SyntaxException(int line, string message) : base($"Syntax error on line {line}: {message}") { }
		public SyntaxException(int line, int col, string message) : base($"Syntax error on line {line}, col {col}: {message}") { }
	}
}
