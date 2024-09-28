using TorqueLinter.Lexer;

namespace TorqueLinter.AST
{
	public class ReturnNode(Token token, Node? value = null) : Node(token)
	{
		public readonly Node? Value = value;
	}
}
