namespace BLPP.Preprocessor
{
	static public class Constants
	{
		static public class Tokens
		{
			public const string DIRECTIVE_DEFINE = "##define";
			public const string DIRECTIVE_USE = "##use";

			public const string MACRO_VAR_ARGS = "...";

			public const string MACRO_KEYWORD_LINE = "#!line";
			public const string MACRO_KEYWORD_VARG_COUNT = "#!vargc";
			public const string MACRO_KEYWORD_VARGS = "#!vargs";
			public const string MACRO_KEYWORD_VARGS_PREPEND = "#!vargsp";
		}
	}
}
