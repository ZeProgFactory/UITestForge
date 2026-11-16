using UITestForge;
using UITestForge.Helpers;

namespace UITestForge.Tests;

[TestClass]
public class ScriptRunnerCliTests
{
   [TestMethod]
   public void SelectFirstAgent_ReturnsFirst()
   {
      var a = new DevFlowAgent { Id = "1" };
      var b = new DevFlowAgent { Id = "2" };
      Assert.AreSame(a, ScriptRunnerCli.SelectFirstAgent(new[] { a, b }));
   }

   [TestMethod]
   public void SelectFirstAgent_EmptyOrNull_ReturnsNull()
   {
      Assert.IsNull(ScriptRunnerCli.SelectFirstAgent(new List<DevFlowAgent>()));
      Assert.IsNull(ScriptRunnerCli.SelectFirstAgent(null));
   }

   [TestMethod]
   public void CountFailures_CountsCrossMarks()
   {
      var log = "[1] tap A\n    \u2713 ok\n[2] tap B\n    \u2717 fail\n    \u2717 fail2\n";
      Assert.AreEqual(2, ScriptRunnerCli.CountFailures(log));
      Assert.AreEqual(0, ScriptRunnerCli.CountFailures(""));
   }

   [TestMethod]
   public void ResolveScriptPath_AddsDefaultExtensionAndUsesBaseFolder()
   {
      var dir = Path.Combine(Path.GetTempPath(), "uitf_" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(dir);
      try
      {
         var file = Path.Combine(dir, "demo.df");
         File.WriteAllText(file, "print hi");
         Assert.AreEqual(file, ScriptRunnerCli.ResolveScriptPath("demo", dir));
         Assert.AreEqual(file, ScriptRunnerCli.ResolveScriptPath(file));
         Assert.IsNull(ScriptRunnerCli.ResolveScriptPath("missing_script_xyz", dir));
         Assert.IsNull(ScriptRunnerCli.ResolveScriptPath(" "));
      }
      finally { Directory.Delete(dir, true); }
   }

   [TestMethod]
   public async Task RunAsync_NoArgs_ReturnsUsageError()
   {
      var err = new StringWriter();
      var code = await ScriptRunnerCli.RunAsync([], new StringWriter(), err);
      Assert.AreEqual(2, code);
      StringAssert.Contains(err.ToString(), "Usage");
   }

   [TestMethod]
   public async Task RunAsync_MissingScript_ReturnsError()
   {
      var err = new StringWriter();
      var code = await ScriptRunnerCli.RunAsync(["does_not_exist_xyz.df"], new StringWriter(), err);
      Assert.AreEqual(2, code);
      StringAssert.Contains(err.ToString(), "not found");
   }
}
