using ZPF;

namespace UITestForge;

public class MainWindowViewModel : BaseViewModel<MainWindowViewModel>
{
   private string _title = $"UITestForge - {VersionInfo.Current.sVersion} ({VersionInfo.Current.BuildOn})";
   public string Title
   {
      get { return _title; }
      set
      {
         _title = value;
         OnPropertyChanged();
      }
   }

   private string _subtitle = "";
   public string Subtitle
   {
      get { return _subtitle; }
      set
      {
         _subtitle = value;
         OnPropertyChanged();
      }
   }

   private bool _showTitleBar = true;
   public bool ShowTitleBar
   {
      get { return _showTitleBar; }
      set
      {
         _showTitleBar = value;
         OnPropertyChanged();
      }
   }
}
