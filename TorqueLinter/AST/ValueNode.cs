using TorqueLinter.Lexer;

namespace TorqueLinter.AST
{
	public abstract class ValueNode<T>(int line, int col, T value) : Node(line, col)
	{
		public readonly T Value = value;
	}

	public class VariableNode(Token token) : ValueNode<string>(token.Line, token.Col, token.Value)
	{
		public bool IsGlobal => Value[0] == '$';
		public bool IsLocal => Value[0] == '%';
		public bool IsValid => IsGlobal || IsLocal;
	}

	public class StringNode(Token token) : ValueNode<string>(token.Line, token.Col, token.Value)
	{
		public bool IsTaggedString => Value[0] == '\'' && Value[^1] == '\'';
	}

	public class IdentifierNode(Token token) : ValueNode<string>(token.Line, token.Col, token.Value) { }
	public class IntegerNode(Token token) : ValueNode<uint>(token.Line, token.Col, uint.Parse(token.Value)) { }
	public class FloatNode(Token token) : ValueNode<double>(token.Line, token.Col, double.Parse(token.Value)) { }
}
