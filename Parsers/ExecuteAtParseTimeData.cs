namespace InputMaster.Parsers
{
  class ExecuteAtParseTimeData
  {
    public ParserOutput ParserOutput { get; }
    public Section Section { get; }
    public Chord Chord { get; }
    public LocatedString LocatedName { get; }
    public LocatedString LocatedArguments { get; }

    public ExecuteAtParseTimeData(ParserOutput parserOutput, Section section, Chord chord, LocatedString locatedName, LocatedString locatedArguments)
    {
      ParserOutput = parserOutput;
      Section = section;
      Chord = chord;
      LocatedName = locatedName;
      LocatedArguments = locatedArguments;
    }
  }
}
