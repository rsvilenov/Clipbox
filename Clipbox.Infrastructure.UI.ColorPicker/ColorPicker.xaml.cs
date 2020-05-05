using Clipbox.Infrastructure.UI.ColorPicker.ColorConversion;
using Clipbox.Infrastructure.UI.ColorPicker.ColorStructures;
using Clipbox.Infrastructure.UI.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Clipbox.Infrastructure.UI.ColorPicker
{
    /// <summary>
    /// Interaction logic for ColorPicker.xaml
    /// </summary>
    public partial class ColorPicker : UserControl
    {
        public ColorPicker()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;

            IsVisibleChanged += ColorPicker_IsVisibleChanged;
            Loaded += ColorPicker_Loaded;
            SizeChanged += ColorPicker_SizeChanged;
        }
        
        private RenderTargetBitmap _paletteRtBitmap;

        Dictionary<Point, HSVColor> _colorMap = new Dictionary<Point, HSVColor>();
        private void StorePalette()
        {
            _colorMap.Clear();

            Size pixelSize = GetPixelSize(new Size(mainCanvas.ActualWidth, mainCanvas.ActualHeight));
            // Use RenderTargetBitmap to get the visual, in case the image has been transformed.
            _paletteRtBitmap = new RenderTargetBitmap((int)pixelSize.Width,
                                                            (int)pixelSize.Height,
                                                            96, 96, PixelFormats.Default);

            _paletteRtBitmap.Render(mainCanvas);
            System.Drawing.Bitmap bitmap;
            using (MemoryStream stream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(_paletteRtBitmap));
                enc.Save(stream);
                bitmap = new System.Drawing.Bitmap(stream);
            }
            //bitmap.Save("palette.png");

            System.Drawing.Imaging.BitmapData bmd = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                  System.Drawing.Imaging.ImageLockMode.ReadWrite,
                                  bitmap.PixelFormat);

            int PixelSize = 4;

            unsafe
            {
                for (int y = 0; y < bmd.Height; y++)
                {
                    byte* row = (byte*)bmd.Scan0 + (y * bmd.Stride);

                    for (int x = 0; x < bmd.Width; x++)
                    {
                        // there shouldn't be any transparency in the
                        // current implementaion of the color canvas,
                        // but anyway...
                        RGBColor rgb = GetRGBFromARGB(
                            r: row[x * PixelSize + 2],
                            g: row[x * PixelSize + 1],
                            b: row[x * PixelSize],
                            a: row[x * PixelSize + 3]);


                        _colorMap.Add(new Point(x, y), HsvConverter.ToColorSpace(rgb));
                    }
                }
            }

            bitmap.UnlockBits(bmd);
            bitmap.Dispose();
            //bitmap.Save(@"text.png");

        }

        private RGBColor GetRGBFromARGB(byte r, byte g, byte b, byte a)
        {
            if (a == 255)
            {
                return new RGBColor
                {
                    R = r,
                    B = b,
                    G = g
                };
            }

            var alpha = a / 255.0;
            var diff = 1.0 - alpha;
            return new RGBColor
            {
                R = (byte)(r * alpha + r * diff),
                G = (byte)(g * alpha + g * diff),
                B = (byte)(b * alpha + b * diff)
            };
        }

        private Size GetPixelSize(Size wpfSize)
        {
            var source = PresentationSource.FromVisual(this);
            Matrix transformToDevice = source.CompositionTarget.TransformToDevice;
            return (Size)transformToDevice.Transform((Vector)wpfSize);
        }

        private void ColorPicker_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.IsLoaded)
            {
                StorePalette();
            }
        }

        private void ColorPicker_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            lock (_setupLockHandle)
            {
                if (!(bool)e.NewValue || _colorMap.Any())
                {
                    return;
                }
            }

            if (ActualWidth > 0 && ActualHeight > 0)
            {
                SetUp();
            }

            var popupParent = Traversing.FindParent<Popup>(this);

            PresentationSource source = PresentationSource.FromVisual(popupParent ?? (Visual)this);
            source.ContentRendered += Source_ContentRendered;
        }


        private void ColorPicker_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private readonly object _setupLockHandle = new object();
        private void SetUp()
        {
            lock (_setupLockHandle)
            {
                if (_colorMap.Any())
                {
                    return;
                }

                StorePalette();
            }

            // wait for the binding oprerations to complete
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
            {
                // check whether the current color has been already set by SetSelectedColor()
                if (SelectedColor == null)
                {
                    // if not, set the selector at the centre and get the color below it
                    Canvas.SetLeft(selector, mainCanvas.ActualWidth / 2 - selector.ActualWidth / 2);
                    Canvas.SetTop(selector, mainCanvas.ActualHeight / 2 - selector.ActualHeight / 2);
                    SelectColorUnderSelector();
                }
            }));
        }

        private void Source_ContentRendered(object sender, EventArgs e)
        {
            if (ActualWidth > 0 && ActualHeight > 0)
            {
                SetUp();
            }
        }

        private Brush SelectorColorBrush
        {
            get { return (Brush)GetValue(SelectorColorBrushProperty); }
            set { SetValue(SelectorColorBrushProperty, value); }
        }

        private static readonly DependencyProperty SelectorColorBrushProperty =
            DependencyProperty.Register(nameof(SelectorColorBrush), typeof(Brush), typeof(ColorPicker), new PropertyMetadata(Brushes.White));





        private static readonly DependencyPropertyKey SelectedColorHSVPropertyKey
        = DependencyProperty.RegisterReadOnly(
            nameof(SelectedColorHSV),
            typeof(HSVColorBindable), typeof(ColorPicker),
            new FrameworkPropertyMetadata(default(HSVColorBindable),
                FrameworkPropertyMetadataOptions.None));

        public static readonly DependencyProperty SelectedColorHSVProperty
            = SelectedColorHSVPropertyKey.DependencyProperty;

        public HSVColorBindable SelectedColorHSV
        {
            get { return (HSVColorBindable)GetValue(SelectedColorHSVProperty); }
            protected set { SetValue(SelectedColorHSVPropertyKey, value); }
        }

        public void SetSelectedColor(HSVColorBindable color)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                HSVColor hsv = color.GetHSV();
                _originalColorUnderSelector = HsvConverter.ToRGB(hsv);
                Color newColor = HsvConverter.ToColor(hsv);

                if (_originalColorUnderSelector != null && newColor.R == _originalColorUnderSelector.R && newColor.B == _originalColorUnderSelector.B && newColor.G == _originalColorUnderSelector.G)
                {
                    return;
                }
                UpdateSelectedColorDPs(hsv, true);
            }));


        }

        private static readonly DependencyPropertyKey SelectedColorPropertyKey
        = DependencyProperty.RegisterReadOnly(
            nameof(SelectedColor),
            typeof(Color), typeof(ColorPicker),
            new FrameworkPropertyMetadata(default(Color),
                FrameworkPropertyMetadataOptions.None));

        public static readonly DependencyProperty SelectedColorProperty
            = SelectedColorPropertyKey.DependencyProperty;
        
        public Color SelectedColor
        {
            get { return (Color)GetValue(SelectedColorProperty); }
            protected set { SetValue(SelectedColorPropertyKey, value); }
        }



        private static readonly DependencyPropertyKey SelectedColorContrastPropertyKey
        = DependencyProperty.RegisterReadOnly(
            nameof(SelectedColorContrast),
            typeof(Color), typeof(ColorPicker),
            new FrameworkPropertyMetadata(default(Color),
                FrameworkPropertyMetadataOptions.None));

        public static readonly DependencyProperty SelectedColorContrastProperty
            = SelectedColorContrastPropertyKey.DependencyProperty;

        public Color SelectedColorContrast
        {
            get { return (Color)GetValue(SelectedColorContrastProperty); }
            protected set { SetValue(SelectedColorContrastPropertyKey, value); }
        }

        public void SetSelectedColor(Color color)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                _originalColorUnderSelector = new RGBColor
                {
                    R = color.R,
                    G = color.G,
                    B = color.B
                };

                var hsv = HsvConverter.ToColorSpace(_originalColorUnderSelector);
                UpdateSelectedColorDPs(hsv, true);
            }));

        }

        public Brush SelectedColorBrush
        {
            get { return (Brush)GetValue(SelectedColorBrushProperty); }
            set { SetValue(SelectedColorBrushProperty, value); }
        }

        public static readonly DependencyProperty SelectedColorBrushProperty =
            DependencyProperty.Register(nameof(SelectedColorBrush), typeof(Brush), typeof(ColorPicker), new PropertyMetadata(Brushes.Transparent));



        private Color SelectedColorMaxVVariant
        {
            get { return (Color)GetValue(SelectedColorMaxVVariantProperty); }
            set { SetValue(SelectedColorMaxVVariantProperty, value); }
        }


        private static readonly DependencyProperty SelectedColorMaxVVariantProperty =
            DependencyProperty.Register(nameof(SelectedColorMaxVVariant), typeof(Color), typeof(ColorPicker), new PropertyMetadata(Colors.Transparent));


        private Color SelectedColorMinVVariant
        {
            get { return (Color)GetValue(SelectedColorMinVVariantProperty); }
            set { SetValue(SelectedColorMinVVariantProperty, value); }
        }

        private static readonly DependencyProperty SelectedColorMinVVariantProperty =
            DependencyProperty.Register(nameof(SelectedColorMinVVariant), typeof(Color), typeof(ColorPicker), new PropertyMetadata(Colors.Transparent));


        private double SelectedColorMinV
        {
            get { return (double)GetValue(SelectedColorMinVProperty); }
            set { SetValue(SelectedColorMinVProperty, value); }
        }

        private static readonly DependencyProperty SelectedColorMinVProperty =
            DependencyProperty.Register(nameof(SelectedColorMinV), typeof(double), typeof(ColorPicker), new PropertyMetadata(0d));




        private double SelectedColorMaxV
        {
            get { return (double)GetValue(SelectedColorMaxVProperty); }
            set { SetValue(SelectedColorMaxVProperty, value); }
        }

        private static readonly DependencyProperty SelectedColorMaxVProperty =
            DependencyProperty.Register(nameof(SelectedColorMaxV), typeof(double), typeof(ColorPicker), new PropertyMetadata(1d));



        private double SelectedColorCurrentV
        {
            get { return (double)GetValue(SelectedColorCurrentVProperty); }
            set { SetValue(SelectedColorCurrentVProperty, value); }
        }

        private static readonly DependencyProperty SelectedColorCurrentVProperty =
            DependencyProperty.Register(nameof(SelectedColorCurrentV), typeof(double), typeof(ColorPicker), new PropertyMetadata(1d, SelectedColorCurrentV_PropertyChanged));

        private static void SelectedColorCurrentV_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ColorPicker)d).SetSelectedColorV((double)e.NewValue);
        }

        private void SetSelectedColorV(double value)
        {
            var hsv = HsvConverter.ToColorSpace(_originalColorUnderSelector);
            hsv.V = value;
            var color = HsvConverter.ToColor(hsv);
            SelectedColor = color;
            SelectedColorBrush = new SolidColorBrush(color);
        }

        private RGBColor _originalColorUnderSelector;
        private double _selectedColorOriginalV;

        private void SelectColorUnderSelector()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background,
                new Action(() =>
                {
                    _originalColorUnderSelector = GetColorUnderSelector();
                    var hsv = HsvConverter.ToColorSpace(_originalColorUnderSelector);

                    UpdateSelectedColorDPs(hsv, false);
                }));
        }

        private void UpdateSelectedColorDPs(HSVColor hsv, bool adjustSelectorPosition)
        {
            HSVColor hscForSelector = GetContrastHSV(HsvConverter.ToColorSpace(_originalColorUnderSelector));
            SelectorColorBrush = new SolidColorBrush(HsvConverter.ToColor(hscForSelector));



            SelectedColorMaxV = 1;
            SelectedColorMinV = 0;

            _selectedColorOriginalV = hsv.V;
            if (adjustSelectorPosition)
            {
                SelectedColorCurrentV = _selectedColorOriginalV;
            }
            else
            {
                hsv.V = SelectedColorCurrentV;
            }


            // the contrast uses the adjusted hsv value for V, not the original color.
            SelectedColorContrast = HsvConverter.ToColor(hsv);

            SelectedColorMinVVariant = HsvConverter.ToColor(new HSVColor
            {
                V = 0,
                S = hsv.S,
                H = hsv.H
            });

            SelectedColorMaxVVariant = HsvConverter.ToColor(new HSVColor
            {
                V = 1,
                S = hsv.S,
                H = hsv.H
            });

            Color newColor = HsvConverter.ToColor(hsv);

            if (SelectedColor != newColor)
            {

                if (adjustSelectorPosition)
                {
                    Point newPositin;
                    if (_colorMap.Values.Any(v => v.V == hsv.V && v.H == hsv.H && v.S == hsv.S))
                    {
                        newPositin = _colorMap.First(v => v.Value.V == hsv.V && v.Value.H == hsv.H && v.Value.S == hsv.S).Key;
                    }
                    else
                    {
                        HSVColor closestHsvColor = GetClosestColorInMap(hsv);
                        newPositin = _colorMap.First(v => v.Value.V == closestHsvColor.V && v.Value.H == closestHsvColor.H && v.Value.S == closestHsvColor.S).Key;
                    }

                    Canvas.SetLeft(selector, newPositin.X);
                    Canvas.SetTop(selector, newPositin.Y);
                }

                SelectedColor = newColor;
                SelectedColorHSV = new HSVColorBindable(hsv);
                SelectedColorBrush = new SolidColorBrush(SelectedColor);
            }
        }

        private static HSVColor GetContrastHSV(HSVColor hsv)
        {
            double newV = (hsv.H < 200 || hsv.H > 300) || hsv.S < 0.5 ? 0 : 1;
            return new HSVColor
            {
                H = 0,
                V = hsv.V < 0.5 ? 1 : newV,
                S = 0
            };
        }

        public static Color GetContrastColor(Color color)
        {
            HSVColor contrastingHSV = GetContrastHSV(HsvConverter.ToColorSpace(color));
            return HsvConverter.ToColor(contrastingHSV);
        }

        private HSVColor GetClosestColorInMap(HSVColor color)
        {
            double mihHue = _colorMap.Values.Select(n => new { Hue = n.H, Distance = Math.Abs(n.H - color.H) })
              .OrderBy(p => p.Distance)
              .First().Hue;

            var colorsForMinHue = _colorMap.Values.Where(hsv => hsv.H == mihHue);

            double minSaturation = colorsForMinHue.Select(n => new { Saturation = n.S, Distance = Math.Abs(n.S - color.S) })
              .OrderBy(p => p.Distance)
              .First().Saturation;

            var colorsForMinHueAndSaturation = colorsForMinHue.Where(hsv => hsv.H == mihHue && hsv.S == minSaturation);

            double minValue = colorsForMinHueAndSaturation.Select(n => new { Value = n.V, Distance = Math.Abs(n.V - color.V) })
              .OrderBy(p => p.Distance)
              .First().Value;

            return new HSVColor
            {
                H = mihHue,
                S = minSaturation,
                V = minValue
            };
        }

        private RGBColor GetColorUnderSelector()
        {
            Size selectorPixelSize = GetPixelSize(new Size(selector.ActualWidth, selector.ActualHeight));
            double left = Canvas.GetLeft(selector);
            double top = Canvas.GetTop(selector);
            if (left < 0)
            {
                left = 0;
            }
            if (top < 0)
            {
                top = 0;
            }

            Size selectorPixelPosition = GetPixelSize(new Size(left, top));

            Point point = new Point(selectorPixelPosition.Width + selectorPixelSize.Width / 2, selectorPixelPosition.Height + selectorPixelSize.Height / 2); //Mouse.GetPosition(this);
            HSVColor hsv = _colorMap[new Point((int)point.X, (int)point.Y)];

            return HsvConverter.ToRGB(hsv);
        }


        #region selector dragging support
        private volatile bool _isDragging;
        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(mainCanvas);
            Canvas.SetLeft(selector, p.X - selector.ActualWidth / 2);
            Canvas.SetTop(selector, p.Y - selector.ActualHeight / 2);
            SelectColorUnderSelector();
            _isDragging = true;
        }

        private void MainCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
        }

        private void MainCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                SelectColorUnderSelector();
                Point p = e.GetPosition(mainCanvas);

                if (p.X + selector.ActualWidth / 2 < mainCanvas.ActualWidth
                    && p.X - selector.ActualWidth / 2 > 0
                    && p.Y + selector.ActualHeight / 2 < mainCanvas.ActualHeight
                    && p.Y - selector.ActualHeight / 2 > 0)
                {
                    Canvas.SetLeft(selector, p.X - selector.ActualWidth / 2);
                    Canvas.SetTop(selector, p.Y - selector.ActualHeight / 2);
                }
            }
        }
        #endregion

    }
}
