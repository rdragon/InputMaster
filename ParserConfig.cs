using System.Text.RegularExpressions;

namespace InputMaster
{
  //todo: put these constants somewhere else
  class ParserConfig
  {
    public static char TokenStart = '{';
    public static char TokenEnd = '}';
    public static char SpecialChar = '√';
    public static char TextEditorSectionIdentifier = '¶';
    public static string SectionIdentifier = $"{SpecialChar}>";
    public static string SpecialCommandIdentifier = $"{SpecialChar}:";
    public static string MultipleCommandsIdentifier = $"{SpecialChar}+";
    public static string CommentIdentifier = $"{SpecialChar}#";
    public static string ArgumentDelimiter = $"{SpecialChar},";
    public static string FlagSectionIdentifier = "Flag";
    public static string InputModeSectionIdentifier = "InputMode";
    public static string ComposeModeSectionIdentifier = "ComposeMode";
    public static string InnerIdentifierTokenPattern = "[A-Z][a-zA-Z0-9_]+";
    public static string TokenPattern = CreateTokenPattern($@"({InnerIdentifierTokenPattern}|(0|[1-9][0-9]*)x)");
    public static string IdentifierTokenPattern = CreateTokenPattern(InnerIdentifierTokenPattern);

    private static string CreateTokenPattern(string text)
    {
      return Regex.Escape(TokenStart.ToString()) + text + Regex.Escape(TokenEnd.ToString());
    }

    public static bool IsIdentifierCharacter(char c)
    {
      return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_';
    }
  }
}
