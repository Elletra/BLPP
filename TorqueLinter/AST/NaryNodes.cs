using TorqueLinter.Lexer;

namespace TorqueLinter.AST
{
	public class UnaryNode(Token operatorToken, Node value) : Node(operatorToken)
	{
		public readonly string Operator = operatorToken.Value;
		public readonly Node Value = value;
	}

	public class BinaryNode(Token operatorToken, Node left, Node right) : Node(left.StartLine, left.StartCol)
	{
		public readonly string Operator = operatorToken.Value;
		public readonly Node Left = left;
		public readonly Node Right = right;
	}

	public class TernaryNode(Token startToken) : Node(startToken)
	{
		public Node? Test { get; set; } = null;
		public Node? True { get; set; } = null;
		public Node? False { get; set; } = null;
	}

	public class IncrementDecrementNode(Token token, Node left) : UnaryNode(token, left) { }
	public class AssignmentNode(Token token, Node left, Node right) : BinaryNode(token, left, right) { }
}
