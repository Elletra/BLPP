namespace TorqueLint.Lexer
{
	public enum TokenType : byte
	{
		Identifier,
		Keyword,
		String, Integer, Float, Variable,
		ParenLeft, ParenRight, CurlyLeft, CurlyRight, SquareLeft, SquareRight,
		Period, Comma, QuestionMark, Colon, ColonColon, Semicolon,
		Operator, Concat, Assignment,
		Invalid,
	}

	public class Token(TokenType type, string value, int line, int col)
	{
		public TokenType Type { get; } = type;
		public string Value { get; } = value;
		public int Line { get; } = line;
		public int Col { get; } = col;

		public bool IsValid => Type < TokenType.Invalid;
	}
}
