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
  }
}
