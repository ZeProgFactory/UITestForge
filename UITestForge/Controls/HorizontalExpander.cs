using System.Windows.Input;

namespace UITestForge.Controls;

/// <summary>
/// An expander that collapses horizontally.
/// <para>Expanded: the header sits on top (horizontal text) with the content below.</para>
/// <para>Collapsed: only a narrow strip with the header text rendered vertically remains,
/// freeing horizontal space for sibling panels.</para>
/// </summary>
[ContentProperty(nameof(ExpanderContent))]
public class HorizontalExpander : ContentView
{
   // ── Bindable properties ─────────────────────────────────────────────────────

   public static readonly BindableProperty HeaderProperty = BindableProperty.Create(
      nameof(Header), typeof(string), typeof(HorizontalExpander), string.Empty,
      propertyChanged: (b, _, _) => ((HorizontalExpander)b).UpdateHeaders());

   public static readonly BindableProperty IsExpandedProperty = BindableProperty.Create(
      nameof(IsExpanded), typeof(bool), typeof(HorizontalExpander), true,
      defaultBindingMode: BindingMode.TwoWay,
      propertyChanged: (b, _, n) => ((HorizontalExpander)b).OnIsExpandedChanged((bool)n));

   public static readonly BindableProperty ExpanderContentProperty = BindableProperty.Create(
      nameof(ExpanderContent), typeof(View), typeof(HorizontalExpander), null,
      propertyChanged: (b, _, n) => ((HorizontalExpander)b).OnExpanderContentChanged((View?)n));

   public static readonly BindableProperty CollapsedWidthProperty = BindableProperty.Create(
      nameof(CollapsedWidth), typeof(double), typeof(HorizontalExpander), 30d,
      propertyChanged: (b, _, _) => ((HorizontalExpander)b).UpdateVerticalHeaderSize()
      );

   public static readonly BindableProperty AnimationLengthProperty = BindableProperty.Create(
      nameof(AnimationLength), typeof(uint), typeof(HorizontalExpander), 250u);

   public static readonly BindableProperty HeaderFontSizeProperty = BindableProperty.Create(
      nameof(HeaderFontSize), typeof(double), typeof(HorizontalExpander), 14d,
      propertyChanged: (b, _, n) =>
      {
         var e = (HorizontalExpander)b;
         e._horizontalHeaderLabel.FontSize = (double)n;
         e._verticalHeaderLabel.FontSize = (double)n;
      });

   public string Header
   {
      get => (string)GetValue(HeaderProperty);
      set => SetValue(HeaderProperty, value);
   }

   public bool IsExpanded
   {
      get => (bool)GetValue(IsExpandedProperty);
      set => SetValue(IsExpandedProperty, value);
   }

   public View? ExpanderContent
   {
      get => (View?)GetValue(ExpanderContentProperty);
      set => SetValue(ExpanderContentProperty, value);
   }

   public double CollapsedWidth
   {
      get => (double)GetValue(CollapsedWidthProperty);
      set => SetValue(CollapsedWidthProperty, value);
   }

   /// <summary>Duration of the expand / collapse animation, in milliseconds.</summary>
   public uint AnimationLength
   {
      get => (uint)GetValue(AnimationLengthProperty);
      set => SetValue(AnimationLengthProperty, value);
   }

   public double HeaderFontSize
   {
      get => (double)GetValue(HeaderFontSizeProperty);
      set => SetValue(HeaderFontSizeProperty, value);
   }

   public event EventHandler<bool>? ExpandedChanged;

   public ICommand ToggleCommand { get; }

   // ── Visual tree ─────────────────────────────────────────────────────────────

   private readonly Grid _root;
   private readonly Grid _verticalHeader;      // collapsed state (narrow strip)
   private readonly Label _verticalHeaderLabel;
   private readonly Grid _expandedPanel;       // expanded state (header on top + content)
   private readonly Label _horizontalHeaderLabel;
   private readonly ContentView _contentHost;

   private double _lastExpandedWidth = -1;
   private bool _isAnimating;

   public HorizontalExpander()
   {
      ToggleCommand = new Command(() => IsExpanded = !IsExpanded);

      // Collapsed strip: vertical title.
      _verticalHeaderLabel = new Label
      {
         FontAttributes = FontAttributes.Bold,
         FontSize = HeaderFontSize,
         LineBreakMode = LineBreakMode.NoWrap,
         HorizontalOptions = LayoutOptions.Center,
         VerticalOptions = LayoutOptions.Center,
         //Margin = new Thickness(0, 6, 0, 0),
      };

      _verticalHeader = new Grid
      {
         Rotation = -90,
         // Rotation is render-only: lay the strip out horizontally (width == expander height)
         // and let the -90 degree rotation about the center place it into the narrow strip.
         HorizontalOptions = LayoutOptions.Center,
         VerticalOptions = LayoutOptions.Center,
         IsVisible = false,
         Children = { _verticalHeaderLabel },
      };
      AddToggleGesture(_verticalHeader);

      // Expanded panel: horizontal title on top, content below.
      _horizontalHeaderLabel = new Label
      {
         FontAttributes = FontAttributes.Bold,
         FontSize = HeaderFontSize,
         LineBreakMode = LineBreakMode.TailTruncation,
         VerticalOptions = LayoutOptions.Center,
      };

      var horizontalHeader = new HorizontalStackLayout
      {
         Spacing = 6,
         Padding = new Thickness(2, 0, 2, 4),
         Children = { _horizontalHeaderLabel },
      };
      AddToggleGesture(horizontalHeader);

      _contentHost = new ContentView
      {
         HorizontalOptions = LayoutOptions.Fill,
         VerticalOptions = LayoutOptions.Fill,
      };

      _expandedPanel = new Grid
      {
         RowDefinitions =
         {
            new RowDefinition(GridLength.Auto),
            new RowDefinition(GridLength.Star),
         },
         RowSpacing = 2,
      };
      _expandedPanel.Add(horizontalHeader, 0, 0);
      _expandedPanel.Add(_contentHost, 0, 1);

      _root = new Grid
      {
         ColumnDefinitions =
         {
            new ColumnDefinition(GridLength.Auto),
            new ColumnDefinition(GridLength.Star),
         },
      };

      _root.Add(_verticalHeader, 0, 0);
      Grid.SetColumnSpan( _verticalHeader, 2 );
      _root.Add(_expandedPanel, 1, 0);

      Content = _root;
      UpdateHeaders();

      SizeChanged += (_, _) => UpdateVerticalHeaderSize();
      UpdateVerticalHeaderSize();
   }

   /// <summary>
   /// Sizes the rotated header strip in unrotated space so its text is never
   /// measured against the (animated) width of the expander.
   /// </summary>
   private void UpdateVerticalHeaderSize()
   {
      _verticalHeader.WidthRequest = Height > 0 ? Height : 0;
      _verticalHeader.HeightRequest = CollapsedWidth;
   }

   private void AddToggleGesture(View view)
      => view.GestureRecognizers.Add(new TapGestureRecognizer { Command = ToggleCommand });

   // ── State handling ──────────────────────────────────────────────────────────

   private void OnExpanderContentChanged(View? content) => _contentHost.Content = content;

   private void OnIsExpandedChanged(bool expanded)
   {
      UpdateHeaders();
      ExpandedChanged?.Invoke(this, expanded);
      _ = AnimateAsync(expanded);
   }

   private void UpdateHeaders()
   {
      var arrow = IsExpanded ? "⏷" : "⏵";

      _horizontalHeaderLabel.Text = $"{arrow} {Header}";
      _verticalHeaderLabel.Text = $"{arrow} {Header}";
   }


   private async Task AnimateAsync(bool expand)
   {
      if (_isAnimating) this.AbortAnimation("ExpanderWidth");
      _isAnimating = true;

      var duration = AnimationLength;

      try
      {
         if (expand)
         {
            var from = Width > 0 ? Width : CollapsedWidth;
            var to = _lastExpandedWidth > 0 ? _lastExpandedWidth : Math.Max(from * 4, 240);

            _verticalHeader.IsVisible = false;
            _expandedPanel.IsVisible = true;
            _expandedPanel.Opacity = 0;
            HorizontalOptions = LayoutOptions.Fill;

            await AnimateWidthAsync(from, to, duration);

            // Hand the width back to the parent layout (star sizing / fill).
            WidthRequest = -1;
            await _expandedPanel.FadeTo(1, duration / 2);
         }
         else
         {
            if (Width > CollapsedWidth) _lastExpandedWidth = Width;

            await _expandedPanel.FadeTo(0, duration / 2);
            _expandedPanel.IsVisible = false;
            _verticalHeader.IsVisible = true;
            HorizontalOptions = LayoutOptions.Start;

            await AnimateWidthAsync(Width > 0 ? Width : _lastExpandedWidth, CollapsedWidth, duration);
            WidthRequest = CollapsedWidth;
         }
      }
      finally
      {
         _isAnimating = false;
      }
   }

   private Task AnimateWidthAsync(double from, double to, uint duration)
   {
      var tcs = new TaskCompletionSource<bool>();

      new Animation(v => WidthRequest = v, from, to, Easing.CubicInOut)
         .Commit(this, "ExpanderWidth", 16, duration, finished: (_, _) => tcs.TrySetResult(true));

      return tcs.Task;
   }
}
