using Microsoft.Extensions.DependencyInjection;
using ZPF.Maui;

namespace UITestForge
{
   public partial class App : Application
   {
      public App()
      {
         InitializeComponent();
      }

#if MACCATALYST
   protected override Window CreateWindow(IActivationState? activationState)
   {
      return new Window(new AppShell());
   }
#endif

#if ANDROID
      protected override Window CreateWindow(IActivationState? activationState)
      {
         return new Window(new AppShell());
      }
#endif

#if WINDOWS
      protected override Window CreateWindow(IActivationState? activationState)
      {
         var w = new MainWindow();

         var wp = new WindowsPos(w);
         wp.Init(w);

         return w;
      }
      //protected override Window CreateWindow(IActivationState? activationState)
      //{
      //   return new Window(new AppShell());
      //}
#endif
   }
}
