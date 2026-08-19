#if WINDOWS
using Microsoft.Maui.Platform;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using ZPF.AT;
#endif

namespace ZPF.Maui;

/// <summary>
/// <code>
///    public partial class App : Application
///    {
///       public App()
///       {
///          InitializeComponent();
///    
///          this.PageAppearing += (sender, e) =>
///          {
///             new WindowsPos(e.Window);
///          };
///       }
///       
///       ...
/// </code>
/// </summary>
public class WindowsPos
{
   // - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -

   public WindowsPos(Window window)
   {
      // Constructor logic if needed
      _Window = window;

      if (string.IsNullOrEmpty(window.Title))
      {
         Init(window);
      }
   }

   // - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -

#if MACCATALYST
   public void Init(Window window)
   {
      // _Window = window;
   }
#endif

   // - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -

#if IOS
   void Init(Window window)
   {
      _Window = window;
   }
#endif

   // - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -

#if ANDROID
   // private Window _Window;

   public void Init(Window window)
   {
      _Window = window;
   }
#endif

   // - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -
   private Window _Window;

#if WINDOWS
   public Window Window { get => _Window; }

   //https://github.com/dotnet/maui/issues/7592

   private const string LastWidthPropertyKey = "windows_last_window_width";
   private const string LastHeightPropertyKey = "windows_last_window_height";
   private const string LastXPropertyKey = "windows_last_window_x";
   private const string LastYPropertyKey = "windows_last_window_y";


   //private bool IsPositionOnScreen(int x, int y, double width, double height)
   //{
   //   // Get all display information
   //   var displays = DeviceDisplay.Current.MainDisplayInfo;

   //   // Check if the window position is within any screen bounds
   //   // At least part of the window (e.g., the top-left corner) should be visible
   //   foreach (var screen in Microsoft.Maui.Devices.DeviceDisplay.Displays)
   //   {
   //      // Convert MAUI display coordinates to screen coordinates
   //      var screenBounds = new
   //      {
   //         Left = (int)(screen.X / screen.Density),
   //         Top = (int)(screen.Y / screen.Density),
   //         Right = (int)((screen.X + screen.Width) / screen.Density),
   //         Bottom = (int)((screen.Y + screen.Height) / screen.Density)
   //      };

   //      // Check if window's top-left corner is within this screen
   //      if (x >= screenBounds.Left && x < screenBounds.Right &&
   //          y >= screenBounds.Top && y < screenBounds.Bottom)
   //      {
   //         return true;
   //      }

   //      // Check if window's top-right corner is within this screen
   //      if ((x + width) >= screenBounds.Left && (x + width) < screenBounds.Right &&
   //          y >= screenBounds.Top && y < screenBounds.Bottom)
   //      {
   //         return true;
   //      }
   //   }

   //   return false;
   //}

   //private bool IsPositionOnScreen(int x, int y, double width, double height)
   //{
   //   // Get main display information
   //   var mainDisplay = DeviceDisplay.Current.MainDisplayInfo;

   //   // Convert MAUI display coordinates to screen coordinates
   //   var screenBounds = new
   //   {
   //      Left = 0,
   //      Top = 0,
   //      Right = (int)(mainDisplay.Width / mainDisplay.Density),
   //      Bottom = (int)(mainDisplay.Height / mainDisplay.Density)
   //   };

   //   // Check if window's top-left corner is within the screen
   //   if (x >= screenBounds.Left && x < screenBounds.Right &&
   //       y >= screenBounds.Top && y < screenBounds.Bottom)
   //   {
   //      return true;
   //   }

   //   // Check if window's top-right corner is within the screen
   //   if ((x + width) >= screenBounds.Left && (x + width) < screenBounds.Right &&
   //       y >= screenBounds.Top && y < screenBounds.Bottom)
   //   {
   //      return true;
   //   }

   //   return false;
   //}

   //private bool IsPositionOnScreen(int x, int y, double width, double height)
   //{
   //   // Get main display information
   //   var mainDisplay = DeviceDisplay.Current.MainDisplayInfo;

   //   // For Windows, we need to use WinUI APIs to get all display information
   //   // The MAUI DeviceDisplay API doesn't expose X/Y coordinates or support for multiple monitors

   //   // Simple approach: Check against a reasonable virtual screen space
   //   // Windows typically supports coordinates from -8192 to +8192 for multi-monitor setups
   //   // If the window is at least partially visible in a reasonable range, accept it

   //   // Check if the position is completely off-screen (e.g., minimized window at -32000)
   //   if (x <= -8192 || y <= -8192 || x >= 8192 || y >= 8192)
   //   {
   //      return false;
   //   }

   //   return true;
   //}

   private bool IsPositionOnScreen(int x, int y, double width, double height)
   {
      // Get all display areas using Windows-specific API
      var displayAreas = DisplayArea.FindAll();

      // Check if the window position is within any screen bounds
      // Use indexed access instead of foreach to avoid WinRT enumeration issues
      for (int i = 0; i < displayAreas.Count; i++)
      {
         var displayArea = displayAreas[i];
         var outerBounds = displayArea.OuterBounds;

         // Check if window's top-left corner is within this screen
         if (x >= outerBounds.X && x < (outerBounds.X + outerBounds.Width) &&
             y >= outerBounds.Y && y < (outerBounds.Y + outerBounds.Height))
         {
            return true;
         }

         // Check if window's top-right corner is within this screen
         if ((x + width) >= outerBounds.X && (x + width) < (outerBounds.X + outerBounds.Width) &&
             y >= outerBounds.Y && y < (outerBounds.Y + outerBounds.Height))
         {
            return true;
         }
      }

      return false;
   }

   public void Init(Window window)
   {
      //_Window = base.CreateWindow(activationState);
      _Window = window ?? throw new ArgumentNullException(nameof(window));
      AppWindow appWindow = null!;

      if (DeviceInfo.Current.Platform == DevicePlatform.WinUI)
      {
         //_Window.Title = System.Reflection.Assembly.GetEntryAssembly().GetName().Name;

         _Window.Title = $"{System.Reflection.Assembly.GetEntryAssembly().GetName().Name} - {VersionInfo.Current.sVersion} ({VersionInfo.Current.BuildOn})";
      }

      _Window.Created += (_, _) =>
      {
         var nativeWindow = (MauiWinUIWindow)_Window.Handler!.PlatformView!;
         appWindow = nativeWindow.GetAppWindow()!;

         try
         {
            if (Preferences.Default.ContainsKey(LastWidthPropertyKey) &&
               Preferences.Default.ContainsKey(LastHeightPropertyKey))
            {
               _Window.Width = Preferences.Default.Get(LastWidthPropertyKey, -1.0);
               _Window.Height = Preferences.Default.Get(LastHeightPropertyKey, -1.0);
            }

            if (Preferences.Default.ContainsKey(LastXPropertyKey) &&
                  Preferences.Default.ContainsKey(LastYPropertyKey))
            {
               var savedX = Preferences.Default.Get(LastXPropertyKey, 0);
               var savedY = Preferences.Default.Get(LastYPropertyKey, 0);

               // Check for minimized window state or position outside of screens
               if (savedX == -32000 ||
                   !IsPositionOnScreen(savedX, savedY, _Window.Width, _Window.Height))
               {
                  // If the saved position is outside of the visible area, move to (0,0)
                  appWindow.Move(new PointInt32(0, 0));
               }
               else
               {
                  // Using appWindow.Move as setting window.X and window.Y was not working properly
                  // with monitors where scaling wasn't 100%.
                  appWindow.Move(new PointInt32(savedX, savedY));
               }
            }
         }
         catch (Exception ex)
         {
            // Handle exceptions that may occur during saving preferences
            AT.Log.Write(new AuditTrail(ex, AuditTrail.TextFormat.TxtEx));

            appWindow.Move(new PointInt32(0, 0));
         }
      };

      _Window.Destroying += (_, _) =>
      {
         try
         {
            Preferences.Default.Set(LastWidthPropertyKey, _Window.Width);
            Preferences.Default.Set(LastHeightPropertyKey, _Window.Height);
            Preferences.Default.Set(LastXPropertyKey, appWindow.Position.X);
            Preferences.Default.Set(LastYPropertyKey, appWindow.Position.Y);
         }
         catch (Exception ex)
         {
            // Handle exceptions that may occur during saving preferences
            AT.Log.Write(new AuditTrail(ex, AuditTrail.TextFormat.TxtEx));
         }
      };
   }

   // - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -

#else
   //protected override Window CreateWindow(IActivationState activationState)
   //{
   //   return new Window(new AppShell());
   //}
#endif

   // - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -
}

