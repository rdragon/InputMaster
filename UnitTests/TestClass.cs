using InputMaster;
using InputMaster.Instances;
using InputMaster.Parsers;
using InputMaster.Properties;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Text;

namespace UnitTests
{
  [TestClass]
  public class TestClass
  {
    [TestMethod]
    public void Test()
    {
      new TestFactory().Run();
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

    [TestMethod]
    public void LocatedString_TrimEnd()
    {
      var x = new LocatedString("x  ", new Location(1, 1));
      x = x.TrimEnd();
      Assert.AreEqual("x", x.Value);
      Assert.AreEqual(new Location(1, 1), x.Location);
    }

    [TestMethod]
    public void LocatedString_Trim()
    {
      var x = new LocatedString("  x  ", new Location(1, 1));
      x = x.Trim();
      Assert.AreEqual("x", x.Value);
      Assert.AreEqual(new Location(1, 3), x.Location);
    }

    [TestMethod]
    public void LocatedString_Substring()
    {
      var x = new LocatedString("abcdef", new Location(1, 1));
      x = x.Substring(2);
      Assert.AreEqual("cdef", x.Value);
      Assert.AreEqual(new Location(1, 3), x.Location);
      x = x.Substring(1, 2);
      Assert.AreEqual("de", x.Value);
      Assert.AreEqual(new Location(1, 4), x.Location);
    }

    [TestMethod]
    public void PasswordMatrix()
    {
      var matrix = new PasswordMatrix(Resources.PasswordMatrix6x5);
      Assert.AreEqual(6, matrix.Width);
      Assert.AreEqual(5, matrix.Height);
      var pretty = new StringBuilder();
      var actualText = new StringBuilder();
      var expectedQueue = new Queue<string>(Resources.PasswordMatrixOutput.Replace("\r", "").Split('\n'));
      foreach (var blueprint in matrix.GetAllBlueprints(5))
      {
        var value = matrix.GetPasswordValue(blueprint);
        var actual = value ?? "(none)";
        var expected = expectedQueue.Dequeue();
        Assert.AreEqual(expected, actual);
        actualText.AppendLine(actual);
        if (value != null)
          pretty.AppendLine($"{blueprint} - {value}");
      }
      // Comment the assert and uncomment the next line to store the actual output.
      // System.IO.File.WriteAllText(@"C:\io\m2qwdu", actualText.ToString());
      // Comment the assert and uncomment the next line to store the pretty output.
      // System.IO.File.WriteAllText(@"C:\io\m2qwdu", pretty.ToString());
    }

    [TestMethod]
    public void Cipher()
    {
      var key = Helper.GetKey("xwel", Helper.GetRandomBytes(16), 1);
      var cipher = new Cipher(key);
      var expected = "xtpi";
      var bytes = cipher.Encrypt(expected);
      var actual = cipher.DecryptToString(bytes);
      Assert.AreEqual(expected, actual);
    }
  }
}
