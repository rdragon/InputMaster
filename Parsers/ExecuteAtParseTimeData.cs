namespace InputMaster.Parsers
{
  internal class ExecuteAtParseTimeData
  {
    public ParserOutput ParserOutput { get; }
    public Section Section { get; }
    public Chord Chord { get; }
    public LocatedString LocatedName { get; }

    public ExecuteAtParseTimeData(ParserOutput parserOutput, Section section, Chord chord, LocatedString locatedName)
    {
      ParserOutput = parserOutput;
      Section = section;
      Chord = chord;
      LocatedName = locatedName;
    }
  }
}
