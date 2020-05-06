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
            SizeChanged += ColorPicker_SizeChanged;
        }

        public event EventHandler SelectedColorChanged;

        public event EventHandler PickerInitialized;

        public bool IsPickerInitialized { get; private set; }

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

            try
            {

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
            }
            finally
            {
                bitmap.Dispose();
            }
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

                IsPickerInitialized = true;
                PickerInitialized?.Invoke(this, new EventArgs());
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


        private static readonly DependencyPropertyKey SelectedColorHexStrPropertyKey
        = DependencyProperty.RegisterReadOnly(
            nameof(SelectedColorHexStr),
            typeof(string), typeof(ColorPicker),
            new FrameworkPropertyMetadata(default(string),
                FrameworkPropertyMetadataOptions.None));

        public static readonly DependencyProperty SelectedColorHexStrProperty
            = SelectedColorHexStrPropertyKey.DependencyProperty;

        public string SelectedColorHexStr
        {
            get { return (string)GetValue(SelectedColorHexStrProperty); }
            protected set { SetValue(SelectedColorHexStrPropertyKey, value); }
        }



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
            if (!IsPickerInitialized)
            {
                throw new InvalidOperationException($"Please wait for the picker to initialize. You can subscribe to {nameof(PickerInitialized)} event.");
            }

            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
            {
                HSVColor hsv = color.GetHSV();
                Color newColor = HsvConverter.ToColor(hsv);

                if (_originalColorUnderSelector != null && newColor.R == _originalColorUnderSelector.R && newColor.B == _originalColorUnderSelector.B && newColor.G == _originalColorUnderSelector.G)
                {
                    return;
                }
                _initialColorV = hsv.V;
                colorVSlider.Value = hsv.V;
                
                _originalColorUnderSelector = HsvConverter.ToRGB(hsv);
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

        private double _initialColorV;

        public void SetSelectedColor(Color color)
        {
            if (!IsPickerInitialized)
            {
                throw new InvalidOperationException($"Please wait for the picker to initialize. You can subscribe to {nameof(PickerInitialized)} event.");
            }

            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
            {
                var newColor = new RGBColor
                {
                    R = color.R,
                    G = color.G,
                    B = color.B
                };

                var hsv = HsvConverter.ToColorSpace(newColor);
                _initialColorV = hsv.V;
                colorVSlider.Value = hsv.V;
                
                _originalColorUnderSelector = HsvConverter.ToRGB(hsv);
                UpdateSelectedColorDPs(hsv, true);
            }));

        }

        private Brush SelectedColorBrush
        {
            get { return (Brush)GetValue(SelectedColorBrushProperty); }
            set { SetValue(SelectedColorBrushProperty, value); }
        }

        private static readonly DependencyProperty SelectedColorBrushProperty =
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

        
        private RGBColor _originalColorUnderSelector;

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
            HSVColor hsvForSelector = HsvConverter.ToColorSpace(_originalColorUnderSelector);
            // the colors in the canvas all have V = 1
            // and if the selected color has been passed as
            // an initial color, it means that we cannot
            // guarantee that it has V = 1, so getting
            // the contrast color will not be accurate.
            hsvForSelector.V = 1; 
            HSVColor hsvContrastForSelector = GetContrastHSV(hsvForSelector);
            SelectorColorBrush = new SolidColorBrush(HsvConverter.ToColor(hsvContrastForSelector));
            
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

            hsv.V = colorVSlider.Value;
            Color newColor = HsvConverter.ToColor(hsv);

            if (SelectedColor != newColor)
            {

                if (adjustSelectorPosition)
                {
                    HSVColor hsvOfPalette = hsv;
                    hsvOfPalette.V = 1;

                    Point newPositin;
                    if (_colorMap.Values.Any(v => v.V == hsvOfPalette.V && v.H == hsvOfPalette.H && v.S == hsvOfPalette.S))
                    {
                        newPositin = _colorMap.First(v => v.Value.V == hsvOfPalette.V && v.Value.H == hsvOfPalette.H && v.Value.S == hsvOfPalette.S).Key;
                    }
                    else
                    {
                        HSVColor closestHsvColor = GetClosestColorInMap(hsvOfPalette);
                        newPositin = _colorMap.First(v => v.Value.V == closestHsvColor.V && v.Value.H == closestHsvColor.H && v.Value.S == closestHsvColor.S).Key;
                    }

                    Canvas.SetLeft(selector, newPositin.X);
                    Canvas.SetTop(selector, newPositin.Y);
                }

                SelectedColor = newColor;
                SelectedColorHSV = new HSVColorBindable(hsv);
                SelectedColorBrush = new SolidColorBrush(SelectedColor);
                SelectedColorHexStr = new RGBColor
                {
                    R = newColor.R,
                    G = newColor.G,
                    B = newColor.B
                }.HexString;

                SelectedColorChanged?.Invoke(this, new EventArgs());
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
            return _colorMap.Values.Select(n => new
            {
                Color = n,
                // https://answers.opencv.org/question/186712/find-an-image-by-nearest-color/
                Distance = Math.Sqrt(Math.Pow(n.H - color.H, 2) + Math.Pow(n.S - color.S, 2) + Math.Pow(n.V - color.V, 2))
            })
            .OrderBy(p => p.Distance)
            .First()
            .Color;
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
            mainCanvas.CaptureMouse();
            _isDragging = true;
        }

        private void MainCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            mainCanvas.ReleaseMouseCapture();
            _isDragging = false;
        }

        private void MainCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                SelectColorUnderSelector();
                Point p = e.GetPosition(mainCanvas);

                double newXPos = p.X - selector.ActualWidth / 2;
                if (p.X < 1)
                {
                    newXPos = 1 - selector.ActualWidth / 2;
                }
                else if (p.X + selector.ActualWidth / 2 > mainCanvas.ActualWidth - 1)
                {
                    newXPos = mainCanvas.ActualWidth - selector.ActualWidth / 2 - 1;
                }

                double newYPos = p.Y - selector.ActualHeight / 2;
                if (p.Y < 1)
                {
                    newYPos = 1 - selector.ActualHeight / 2;
                }
                else if (p.Y + selector.ActualHeight / 2 > mainCanvas.ActualHeight - 1)
                {
                    newYPos = mainCanvas.ActualHeight - selector.ActualHeight / 2 - 1;
                }

                Canvas.SetLeft(selector, newXPos);
                Canvas.SetTop(selector, newYPos);
            }
        }
        #endregion

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_originalColorUnderSelector != null && e.NewValue != _initialColorV)
            {
                var hsv = HsvConverter.ToColorSpace(_originalColorUnderSelector);
                hsv.V = e.NewValue;
                UpdateSelectedColorDPs(hsv, false);
            }
        }
    }
}
