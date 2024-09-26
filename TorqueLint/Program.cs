using TorqueLint.Lexer;

var code = File.ReadAllText("./test.cs");

foreach (var token in new Lexer().Scan(code))
{
	Console.WriteLine("{0,-24} {1,-24}", token.Type, token.Value);
}
