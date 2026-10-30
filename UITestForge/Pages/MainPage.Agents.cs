using System.Diagnostics;
using System.Text;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Maui.Views;
using ScriptPad.Sample;
using UITestForge.Helpers;
using UITestForge.ViewModels;
using UITestForge.Views;
using ZPF.Maui.Script;
using ZPF.Maui.Script.ScriptEditing;

namespace UITestForge;

public partial class MainPage
{
   private void OnAgentsCollectionChanged(object? sender, EventArgs e)
   {
      // MAUI's Picker doesn't always sync its dropdown list when the bound
      // ObservableCollection is mutated; resetting ItemsSource forces a rebuild.
      AgentsPicker.ItemsSource = null;
      AgentsPicker.ItemsSource = _viewModel.Agents;

      // Restore selection after ItemsSource reset
      if (_viewModel.SelectedAgent is not null)
      {
         AgentsPicker.SelectedItem = _viewModel.SelectedAgent;
      }
   }

   private void OnScreenshotCaptured(object? sender, string imagePath)
   {
      ScreenshotImage.Source = ImageSource.FromFile(imagePath);
   }

   private void OnAgentSelectionChanged(object? sender, EventArgs e)
   {
      // Sync the ViewModel's SelectedAgent with the Picker
      _viewModel.SelectedAgent = AgentsPicker.SelectedItem as DevFlowAgent;
   }

}
