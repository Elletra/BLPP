using TorqueLinter.Lexer;

namespace TorqueLinter.AST
{
	public class TernaryNode(Token startToken) : Node(startToken)
	{
		public Node? Test { get; set; } = null;
		public Node? True { get; set; } = null;
		public Node? False { get; set; } = null;
	}
}
