using InputMaster.Parsers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
  [TestClass]
  public class TestClass
  {
    [TestMethod]
    public void Test()
    {
      new TestBrain().Run();
    }

    [TestMethod]
    public void LocatedString_Split()
    {
      var x = new LocatedString("Some located string", new Location(1, 1));
      var ar = x.Split(" ");
      Assert.AreEqual(3, ar.Length);
      Assert.AreEqual("Some", ar[0].Value);
      Assert.AreEqual("located", ar[1].Value);
      Assert.AreEqual("string", ar[2].Value);
      Assert.AreEqual(new Location(1, 1), ar[0].Location);
      Assert.AreEqual(new Location(1, 6), ar[1].Location);
      Assert.AreEqual(new Location(1, 14), ar[2].Location);
    }

    [TestMethod]
    public void LocatedString_TrimStart()
    {
      var x = new LocatedString("  x", new Location(1, 1));
      x = x.TrimStart();
      Assert.AreEqual("x", x.Value);
      Assert.AreEqual(new Location(1, 3), x.Location);
    }
  }
}
