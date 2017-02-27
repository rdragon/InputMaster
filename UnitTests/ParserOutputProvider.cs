using InputMaster;
using InputMaster.Parsers;
using System;

namespace UnitTests
{
  class ParserOutputProvider : IParserOutputProvider
  {
    public event Action<ParserOutput> NewParserOutput = delegate { };

    public void SetParserOutput(ParserOutput parserOutput)
    {
      NewParserOutput(parserOutput);
    }
  }
}
