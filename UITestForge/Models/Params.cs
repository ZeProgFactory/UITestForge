

namespace UITestForge.Models;

public class Params
{
   public string DataFolder { get; set; } = "";

   public string ScriptFolder
   {
      get
      {
         if (string.IsNullOrEmpty(_ScriptFolder))
         {
            _ScriptFolder = System.IO.Path.GetDirectoryName(LastScript);
         }
         return _ScriptFolder;
      }
      set => _ScriptFolder = value;
   }
   string _ScriptFolder = "";

   public string LastScript { get; set; } = "";
}
