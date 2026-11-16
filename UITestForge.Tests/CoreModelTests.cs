using UITestForge.Models;

namespace UITestForge.Tests;

[TestClass]
public class CoreModelTests
{
   [TestMethod]
   public void TreeNodeItem_IndentLeft_ScalesWithDepth()
   {
      var item = new TreeNodeItem { Node = new TreeNode(), Depth = 3 };
      Assert.AreEqual(54.0, item.IndentLeft);
   }

   [TestMethod]
   public void Params_ScriptFolder_ReturnsDirectoryOfLastScript()
   {
      var p = new Params { LastScript = Path.Combine("C:", "scripts", "a.df") };
      Assert.AreEqual(Path.Combine("C:", "scripts"), p.ScriptFolder);
   }
}

