﻿using System.Text.RegularExpressions;

namespace InputMaster
{
  internal static class Constants
  {
    #region Parser
    public static char TokenStart { get; } = '{';
    public static char TokenEnd { get; } = '}';
    public static char SpecialChar { get; } = '√';
    public static char TextEditorSectionIdentifier { get; } = '¶';
    public static string SectionIdentifier { get; } = $"{SpecialChar}>";
    public static string SpecialCommandIdentifier { get; } = $"{SpecialChar}:";
    public static string MultipleCommandsIdentifier { get; } = $"{SpecialChar}+";
    public static string CommentIdentifier { get; } = $"{SpecialChar}#";
    public static string ArgumentDelimiter { get; } = $"{SpecialChar},";
    public static string FlagSectionIdentifier { get; } = "Flag";
    public static string InputModeSectionIdentifier { get; } = "InputMode";
    public static string ComposeModeSectionIdentifier { get; } = "ComposeMode";
    public static string InnerIdentifierTokenPattern { get; } = "[A-Z][a-zA-Z0-9_]+";
    public static string TokenPattern { get; } = CreateTokenPattern($@"({InnerIdentifierTokenPattern}|(0|[1-9][0-9]*)x)");
    public static string IdentifierTokenPattern { get; } = CreateTokenPattern(InnerIdentifierTokenPattern);
    public static bool IsIdentifierCharacter(char c) => c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '_';
    private static string CreateTokenPattern(string text) => Regex.Escape(TokenStart.ToString()) + text + Regex.Escape(TokenEnd.ToString());
    #endregion

    #region TextEditor
    public static string HiddenTag { get; } = "[hidden]";
    public static string SharedPasswordFileTag { get; } = "[sharedPassword]";
    public static string HotkeyFileTag { get; } = "[hotkeys]";
    public static string AccountFileTag { get; } = "[accounts]";
    public static Regex SharedFileRegex { get; } = new Regex(@"\[shared_(?<id>[0-9a-z]+)\]");
    #endregion
  }
}
