

namespace UITestForge.Models;

public class Params
{
   public string DataFolder { get; set; } = "";


   public string ScriptFolder { get => System.IO.Path.GetDirectoryName(LastScript); }


   public string LastScript { get; set; } = "";
}
