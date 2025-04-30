namespace MaterialSkin.Controls
{
    using MaterialSkin;
    using MaterialSkin.Animations;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.Drawing.Text;
    using System.Windows.Forms;

    public class MaterialDrawer : Control, IMaterialControl
    {
        // TODO: Invalidate when changing custom properties

        private bool _showIconsWhenHidden;

        [Category("Drawer")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool ShowIconsWhenHidden
        {
            get
            {
                return _showIconsWhenHidden;
            }
            set
            {
                if (_showIconsWhenHidden != value)
                {
                    _showIconsWhenHidden = value;
                    UpdateTabRects();
                    PreProcessIcons();
                    ShowHideAnimation();
                    Paint(new PaintEventArgs(CreateGraphics(), ClientRectangle));
                    DrawerShowIconsWhenHiddenChanged?.Invoke(this);
                }
            }
        }

        private bool _isOpen;

        [Category("Drawer")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool IsOpen
        {
            get
            {
                return _isOpen;
            }
            set
            {
                _isOpen = value;
                if (value)
                {
                    Show();
                }
                else
                {
                    Hide();
                }
            }
        }

        [Category("Drawer")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool AutoHide { get; set; }

        [Category("Drawer")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool AutoShow { get; set; }

        [Category("Drawer")]
        private bool _useColors;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool UseColors
        {
            get
            {
                return _useColors;
            }
            set
            {
                _useColors = value;
                PreProcessIcons();
                Invalidate();
            }
        }

        [Category("Drawer")]
        private bool _highlightWithAccent;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool HighlightWithAccent
        {
            get
            {
                return _highlightWithAccent;
            }
            set
            {
                _highlightWithAccent = value;
                PreProcessIcons();
                Invalidate();
            }
        }

        [Category("Drawer")]
        private bool _backgroundWithAccent;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool BackgroundWithAccent
        {
            get
            {
                return _backgroundWithAccent;
            }
            set
            {
                _backgroundWithAccent = value;
                Invalidate();
            }
        }

        [Category("Drawer")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int IndicatorWidth { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int Depth { get; set; }

        [Browsable(false)]
        public MaterialSkinManager SkinManager => MaterialSkinManager.Instance;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public MouseState MouseState { get; set; }

        public delegate void DrawerStateHandler(object sender);

        public event DrawerStateHandler DrawerStateChanged;

        public event DrawerStateHandler DrawerBeginOpen;

        public event DrawerStateHandler DrawerEndOpen;

        public event DrawerStateHandler DrawerBeginClose;

        public event DrawerStateHandler DrawerEndClose;

        public event DrawerStateHandler DrawerShowIconsWhenHiddenChanged;

        public event EventHandler<Cursor> CursorUpdate;

        // icons
        private Dictionary<string, TextureBrush> iconsBrushes;

        private Dictionary<string, TextureBrush> iconsSelectedBrushes;
        private Dictionary<string, Rectangle> iconsSize;
        private int prevLocation;

        private int rippleSize = 0;

        private MaterialTabControl _baseTabControl;

        [Category("Behavior")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public MaterialTabControl BaseTabControl
        {
            get { return _baseTabControl; }
            set
            {
                _baseTabControl = value;
                if (_baseTabControl == null)
                {
                    return;
                }

                UpdateTabRects();
                PreProcessIcons();

                // Other helpers

                _previousSelectedTabIndex = _baseTabControl.SelectedIndex;
                _baseTabControl.Deselected += (sender, args) =>
                {
                    _previousSelectedTabIndex = _baseTabControl.SelectedIndex;
                };
                _baseTabControl.SelectedIndexChanged += (sender, args) =>
                {
                    _clickAnimManager.SetProgress(0);
                    _clickAnimManager.StartNewAnimation(AnimationDirection.In);
                };
                _baseTabControl.ControlAdded += delegate
                {
                    Invalidate();
                };
                _baseTabControl.ControlRemoved += delegate
                {
                    Invalidate();
                };
            }
        }

        private void PreProcessIcons()
        {
            // pre-process and pre-allocate texture brushes (icons)
            if (_baseTabControl == null || _baseTabControl.TabCount == 0 || _baseTabControl.ImageList == null || _drawerItemRects == null || _drawerItemRects.Count == 0)
            {
                return;
            }

            // Calculate lightness and color
            float l = UseColors ? SkinManager.ColorScheme.TextColor.R / 255 : SkinManager.Theme == MaterialSkinManager.Themes.LIGHT ? 0f : 1f;
            float r = (_highlightWithAccent ? SkinManager.ColorScheme.AccentColor.R : SkinManager.ColorScheme.PrimaryColor.R) / 255f;
            float g = (_highlightWithAccent ? SkinManager.ColorScheme.AccentColor.G : SkinManager.ColorScheme.PrimaryColor.G) / 255f;
            float b = (_highlightWithAccent ? SkinManager.ColorScheme.AccentColor.B : SkinManager.ColorScheme.PrimaryColor.B) / 255f;

            // Create matrices
            float[][] matrixGray = {
                    new float[] {   0,   0,   0,   0,  0}, // Red scale factor
                    new float[] {   0,   0,   0,   0,  0}, // Green scale factor
                    new float[] {   0,   0,   0,   0,  0}, // Blue scale factor
                    new float[] {   0,   0,   0, .7f,  0}, // alpha scale factor
                    new float[] {   l,   l,   l,   0,  1}};// offset

            float[][] matrixColor = {
                    new float[] {   0,   0,   0,   0,  0}, // Red scale factor
                    new float[] {   0,   0,   0,   0,  0}, // Green scale factor
                    new float[] {   0,   0,   0,   0,  0}, // Blue scale factor
                    new float[] {   0,   0,   0,   1,  0}, // alpha scale factor
                    new float[] {   r,   g,   b,   0,  1}};// offset

            ColorMatrix colorMatrixGray = new ColorMatrix(matrixGray);
            ColorMatrix colorMatrixColor = new ColorMatrix(matrixColor);

            ImageAttributes grayImageAttributes = new ImageAttributes();
            ImageAttributes colorImageAttributes = new ImageAttributes();

            // Set color matrices
            grayImageAttributes.SetColorMatrix(colorMatrixGray, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            colorImageAttributes.SetColorMatrix(colorMatrixColor, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

            // Create brushes
            iconsBrushes = new Dictionary<string, TextureBrush>(_baseTabControl.TabPages.Count);
            iconsSelectedBrushes = new Dictionary<string, TextureBrush>(_baseTabControl.TabPages.Count);
            iconsSize = new Dictionary<string, Rectangle>(_baseTabControl.TabPages.Count);

            foreach (TabPage tabPage in _baseTabControl.TabPages)
            {
                // skip items without image
                if (String.IsNullOrEmpty(tabPage.ImageKey) || _drawerItemRects == null)
                {
                    continue;
                }

                // Image Rect
                Image? image = _baseTabControl.ImageList.Images[tabPage.ImageKey];
                if (image == null) 
                {
                    continue;
                }
                Rectangle destRect = new Rectangle(0, 0, image.Width, image.Height);

                // Create a pre-processed copy of the image (GRAY)
                Bitmap bgray = new Bitmap(destRect.Width, destRect.Height);
                using (Graphics gGray = Graphics.FromImage(bgray))
                {
                    gGray.DrawImage(image,
                        new Point[] {
                                new Point(0, 0),
                                new Point(destRect.Width, 0),
                                new Point(0, destRect.Height),
                        },
                        destRect, GraphicsUnit.Pixel, grayImageAttributes);
                }

                // Create a pre-processed copy of the image (PRIMARY COLOR)
                Bitmap bcolor = new Bitmap(destRect.Width, destRect.Height);
                using (Graphics gColor = Graphics.FromImage(bcolor))
                {
                    gColor.DrawImage(image,
                        new Point[] {
                                new Point(0, 0),
                                new Point(destRect.Width, 0),
                                new Point(0, destRect.Height),
                        },
                        destRect, GraphicsUnit.Pixel, colorImageAttributes);
                }

                // added processed image to brush for drawing
                TextureBrush textureBrushGray = new TextureBrush(bgray);
                TextureBrush textureBrushColor = new TextureBrush(bcolor);

                textureBrushGray.WrapMode = WrapMode.Clamp;
                textureBrushColor.WrapMode = WrapMode.Clamp;

                // Translate the brushes to the correct positions
                int currentTabIndex = _baseTabControl.TabPages.IndexOf(tabPage);

                Rectangle iconRect = new Rectangle(
                   _drawerItemRects[currentTabIndex].X + (drawerItemHeight / 2) - (image.Width / 2),
                   _drawerItemRects[currentTabIndex].Y + (drawerItemHeight / 2) - (image.Height / 2),
                   image.Width, image.Height);

                textureBrushGray.TranslateTransform(iconRect.X + iconRect.Width / 2 - image.Width / 2,
                                                    iconRect.Y + iconRect.Height / 2 - image.Height / 2);
                textureBrushColor.TranslateTransform(iconRect.X + iconRect.Width / 2 - image.Width / 2,
                                                     iconRect.Y + iconRect.Height / 2 - image.Height / 2);

                // add to dictionary
                string ik = string.Concat(tabPage.ImageKey, "_", tabPage.Name);
                iconsBrushes.Add(ik, textureBrushGray);
                iconsSelectedBrushes.Add(ik, textureBrushColor);
                iconsSize.Add(ik, new Rectangle(0, 0, iconRect.Width, iconRect.Height));
            }
        }

        private int _previousSelectedTabIndex;

        private Point _animationSource;

        private readonly AnimationManager _clickAnimManager;

        private readonly AnimationManager _showHideAnimManager;

        private List<Rectangle> _drawerItemRects;
        private List<GraphicsPath> _drawerItemPaths;

        private const int TAB_HEADER_PADDING = 24;
        private const int BORDER_WIDTH = 7;

        private int drawerItemHeight;

        public int MinWidth;
        private int _lastMouseY;
        private int _lastLocationY;

        public MaterialDrawer()
        {
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            Height = 120;
            Width = 250;
            IndicatorWidth = 0;
            _isOpen = false;
            ShowIconsWhenHidden = false;
            AutoHide = false;
            AutoShow = false;
            HighlightWithAccent = true;
            BackgroundWithAccent = false;

            _showHideAnimManager = new AnimationManager
            {
                AnimationType = AnimationType.EaseInOut,
                Increment = 0.04
            };
            _showHideAnimManager.OnAnimationProgress += sender =>
            {
                Invalidate();
                ShowHideAnimation();
            };
            _showHideAnimManager.OnAnimationFinished += sender =>
            {
                if (_baseTabControl != null && _drawerItemRects?.Count > 0)
                {
                    rippleSize = _drawerItemRects[_baseTabControl.SelectedIndex].Width;
                }

                if (_isOpen)
                {
                    DrawerEndOpen?.Invoke(this);
                }
                else
                {
                    DrawerEndClose?.Invoke(this);
                }
            };

            SkinManager.ColorSchemeChanged += sender =>
            {
                PreProcessIcons();
            };

            SkinManager.ThemeChanged += sender =>
            {
                PreProcessIcons();
            };

            _clickAnimManager = new AnimationManager
            {
                AnimationType = AnimationType.EaseOut,
                Increment = 0.04
            };
            _clickAnimManager.OnAnimationProgress += sender => Invalidate();

            MouseWheel += MaterialDrawer_MouseWheel;
        }

        private void MaterialDrawer_MouseWheel(object? sender, MouseEventArgs e)
        {
            int step = 20;
            if (e.Delta > 0)
            {
                if (Location.Y < 0)
                {
                    Location = new Point(Location.X, Location.Y + step > 0 ? 0 : Location.Y + step);
                    Height = Location.Y + step > 0 ? Parent?.Height ?? 0 : Height - step;
                }
            }
            else
            {
                if (Height < (8 + drawerItemHeight) * _drawerItemRects.Count)
                {
                    Location = new Point(Location.X, Location.Y - step);
                    Height += step;
                }
            }
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        protected override void InitLayout()
        {
            drawerItemHeight = TAB_HEADER_PADDING * 2 - SkinManager.FORM_PADDING / 2;
            MinWidth = (int)(SkinManager.FORM_PADDING * 1.5 + drawerItemHeight);
            _showHideAnimManager.SetProgress(_isOpen ? 0 : 1);
            ShowHideAnimation();
            Invalidate();

            base.InitLayout();
        }

        private void ShowHideAnimation()
        {
            double showHideAnimProgress = _showHideAnimManager.GetProgress();
            if (_showHideAnimManager.IsAnimating())
            {
                if (ShowIconsWhenHidden)
                {
                    Location = new Point((int)((-Width + MinWidth) * showHideAnimProgress), Location.Y);
                }
                else
                {
                    Location = new Point((int)(-Width * showHideAnimProgress), Location.Y);
                }
            }
            else
            {
                if (_isOpen)
                {
                    Location = new Point(0, Location.Y);
                }
                else
                {
                    if (ShowIconsWhenHidden)
                    {
                        Location = new Point(-Width + MinWidth, Location.Y);
                    }
                    else
                    {
                        Location = new Point(-Width, Location.Y);
                    }
                }
            }
            UpdateTabRects();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Paint(e);
        }

        private new void Paint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // redraw stuff
            g.Clear(UseColors ? SkinManager.ColorScheme.PrimaryColor : SkinManager.BackdropColor);

            if (_baseTabControl == null)
            {
                return;
            }

            if (!_clickAnimManager.IsAnimating() || _drawerItemRects == null || _drawerItemRects.Count != _baseTabControl.TabCount)
            {
                UpdateTabRects();
            }

            if (_drawerItemRects == null || _drawerItemRects.Count != _baseTabControl.TabCount)
            {
                return;
            }

            // Click Animation
            double clickAnimProgress = _clickAnimManager.GetProgress();
            // Show/Hide Drawer Animation
            double showHideAnimProgress = _showHideAnimManager.GetProgress();
            int rSize = (int)(clickAnimProgress * rippleSize * 1.75);

            int dx = prevLocation - Location.X;
            prevLocation = Location.X;

            // Ripple
            if (_clickAnimManager.IsAnimating())
            {
                SolidBrush rippleBrush = new SolidBrush(Color.FromArgb((int)(70 - (clickAnimProgress * 70)),
                    UseColors ? SkinManager.ColorScheme.AccentColor : // Using colors
                    SkinManager.Theme == MaterialSkinManager.Themes.LIGHT ? SkinManager.ColorScheme.PrimaryColor : // light theme
                    SkinManager.ColorScheme.LightPrimaryColor)); // dark theme

                g.SetClip(_drawerItemPaths[_baseTabControl.SelectedIndex]);
                g.FillEllipse(rippleBrush, new Rectangle(_animationSource.X + dx - (rSize / 2), _animationSource.Y - rSize / 2, rSize, rSize));
                g.ResetClip();
                rippleBrush.Dispose();
            }

            // Draw menu items
            foreach (TabPage tabPage in _baseTabControl.TabPages)
            {
                int currentTabIndex = _baseTabControl.TabPages.IndexOf(tabPage);

                // Background
                Brush bgBrush = new SolidBrush(Color.FromArgb(CalculateAlpha(60, 0, currentTabIndex, clickAnimProgress),
                    UseColors ? _backgroundWithAccent ? SkinManager.ColorScheme.AccentColor : SkinManager.ColorScheme.LightPrimaryColor : // using colors
                    _backgroundWithAccent ? SkinManager.ColorScheme.AccentColor : // defaul accent
                    SkinManager.Theme == MaterialSkinManager.Themes.LIGHT ? SkinManager.ColorScheme.PrimaryColor : // default light
                    SkinManager.ColorScheme.LightPrimaryColor)); // default dark
                g.FillPath(bgBrush, _drawerItemPaths[currentTabIndex]);
                bgBrush.Dispose();

                // Text
                Color textColor = Color.FromArgb(CalculateAlphaZeroWhenClosed(SkinManager.TextHighEmphasisColor.A, UseColors ? SkinManager.TextMediumEmphasisColor.A : 255, currentTabIndex, clickAnimProgress, 1 - showHideAnimProgress), // alpha
                    UseColors ? (currentTabIndex == _baseTabControl.SelectedIndex ? (_highlightWithAccent ? SkinManager.ColorScheme.AccentColor : SkinManager.ColorScheme.PrimaryColor) // Use colors - selected
                    : SkinManager.ColorScheme.TextColor) :  // Use colors - not selected
                    (currentTabIndex == _baseTabControl.SelectedIndex ? (_highlightWithAccent ? SkinManager.ColorScheme.AccentColor : SkinManager.ColorScheme.PrimaryColor) : // selected
                    SkinManager.TextHighEmphasisColor));

                IntPtr textFont = SkinManager.GetLogFontByType(MaterialSkinManager.FontType.Subtitle2);

                Rectangle textRect = _drawerItemRects[currentTabIndex];
                textRect.X += _baseTabControl.ImageList != null ? drawerItemHeight : (int)(SkinManager.FORM_PADDING * 0.75);
                textRect.Width -= SkinManager.FORM_PADDING << 2;

                using (NativeTextRenderer NativeText = new NativeTextRenderer(g))
                {
                    NativeText.DrawTransparentText(tabPage.Text, textFont, textColor, textRect.Location, textRect.Size, NativeTextRenderer.TextAlignFlags.Left | NativeTextRenderer.TextAlignFlags.Middle);
                }

                // Icons
                if (_baseTabControl.ImageList != null && !String.IsNullOrEmpty(tabPage.ImageKey))
                {
                    string ik = string.Concat(tabPage.ImageKey, "_", tabPage.Name);
                    if (ShowIconsWhenHidden)
                    {
                        iconsBrushes[ik].TranslateTransform(dx, 0);
                        iconsSelectedBrushes[ik].TranslateTransform(dx, 0);
                    }
                    if (tabPage is MaterialTabPage materialTabPage && !materialTabPage.DrawIconSilhouette)
                    {
                        Image? image = _baseTabControl.ImageList.Images[tabPage.ImageKey];
                        if (image != null)
                        {
                            g.DrawImage(image,
                                _drawerItemRects[currentTabIndex].X + (drawerItemHeight >> 1) - (iconsSize[ik].Width >> 1),
                                _drawerItemRects[currentTabIndex].Y + (drawerItemHeight >> 1) - (iconsSize[ik].Height >> 1));
                        }
                    }
                    else
                    {
                        Rectangle iconRect = new Rectangle(
                            _drawerItemRects[currentTabIndex].X + (drawerItemHeight >> 1) - (iconsSize[ik].Width >> 1),
                            _drawerItemRects[currentTabIndex].Y + (drawerItemHeight >> 1) - (iconsSize[ik].Height >> 1),
                            iconsSize[ik].Width, iconsSize[ik].Height);
                        g.FillRectangle(currentTabIndex == _baseTabControl.SelectedIndex ? iconsSelectedBrushes[ik] : iconsBrushes[ik], iconRect);
                    }
                }
            }

            // Draw divider if not using colors
            if (!UseColors)
            {
                using (Pen dividerPen = new Pen(SkinManager.DividersColor, 1))
                {
                    g.DrawLine(dividerPen, Width - 1, 0, Width - 1, Height);
                }
            }

            // Animate tab indicator
            int previousSelectedTabIndexIfHasOne = _previousSelectedTabIndex == -1 ? _baseTabControl.SelectedIndex : _previousSelectedTabIndex;
            Rectangle previousActiveTabRect = _drawerItemRects[previousSelectedTabIndexIfHasOne];
            Rectangle activeTabPageRect = _drawerItemRects[_baseTabControl.SelectedIndex];

            int y = previousActiveTabRect.Y + (int)((activeTabPageRect.Y - previousActiveTabRect.Y) * clickAnimProgress);
            int x = ShowIconsWhenHidden ? -Location.X : 0;
            int height = drawerItemHeight;

            g.FillRectangle(SkinManager.ColorScheme.AccentBrush, x, y, IndicatorWidth, height);
        }

        public new void Show()
        {
            _isOpen = true;
            DrawerStateChanged?.Invoke(this);
            DrawerBeginOpen?.Invoke(this);
            _showHideAnimManager.StartNewAnimation(AnimationDirection.Out);
        }

        public new void Hide()
        {
            _isOpen = false;
            DrawerStateChanged?.Invoke(this);
            DrawerBeginClose?.Invoke(this);
            _showHideAnimManager.StartNewAnimation(AnimationDirection.In);
        }

        public void Toggle()
        {
            if (_isOpen)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        private int CalculateAlphaZeroWhenClosed(int primaryA, int secondaryA, int tabIndex, double clickAnimProgress, double showHideAnimProgress)
        {
            // Drawer is closed
            if (!_isOpen && !_showHideAnimManager.IsAnimating())
            {
                return 0;
            }
            // Active menu (no change)
            if (tabIndex == _baseTabControl.SelectedIndex && (!_clickAnimManager.IsAnimating() || _showHideAnimManager.IsAnimating()))
            {
                return (int)(primaryA * showHideAnimProgress);
            }
            // Previous menu (changing)
            if (tabIndex == _previousSelectedTabIndex && !_showHideAnimManager.IsAnimating())
            {
                return primaryA - (int)((primaryA - secondaryA) * clickAnimProgress);
            }
            // Inactive menu (no change)
            if (tabIndex != _baseTabControl.SelectedIndex)
            {
                return (int)(secondaryA * showHideAnimProgress);
            }
            // Active menu (changing)
            return secondaryA + (int)((primaryA - secondaryA) * clickAnimProgress);
        }

        private int CalculateAlpha(int primaryA, int secondaryA, int tabIndex, double clickAnimProgress)
        {
            if (tabIndex == _baseTabControl.SelectedIndex && !_clickAnimManager.IsAnimating())
            {
                return primaryA;
            }
            if (tabIndex != _previousSelectedTabIndex && tabIndex != _baseTabControl.SelectedIndex)
            {
                return secondaryA;
            }
            if (tabIndex == _previousSelectedTabIndex)
            {
                return primaryA - (int)((primaryA - secondaryA) * clickAnimProgress);
            }
            return secondaryA + (int)((primaryA - secondaryA) * clickAnimProgress);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            if (_drawerItemRects == null)
            {
                UpdateTabRects();
            }

            for (int i = 0; i < _drawerItemRects?.Count; i++)
            {
                if (_drawerItemRects[i].Contains(e.Location) && _lastLocationY == Location.Y)
                {
                    _baseTabControl.SelectedIndex = i;
                    if (AutoHide && !AutoShow)
                    {
                        Hide();
                    }
                }
            }

            _animationSource = e.Location;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            _lastMouseY = e.Y;
            _lastLocationY = Location.Y; // memorize Y location of drawer
            base.OnMouseDown(e);
            if (DesignMode)
            {
                return;
            }

            MouseState = MouseState.DOWN;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (DesignMode)
            {
                return;
            }

            MouseState = MouseState.OUT;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (DesignMode)
            {
                return;
            }

            if (e.Button == MouseButtons.Left && e.Y != _lastMouseY && (Location.Y < 0 || Height < (8 + drawerItemHeight) * _drawerItemRects.Count))
            {
                int diff = e.Y - _lastMouseY;
                if (diff > 0) 
                {
                    if (Location.Y < 0)
                    {
                        Location = new Point(Location.X, Location.Y + diff > 0 ? 0 : Location.Y + diff);
                        Height = Parent?.Height ?? 0 + Math.Abs(Location.Y);
                    }
                }
                else 
                {
                    if (Height < (8 + drawerItemHeight) * _drawerItemRects.Count)
                    {
                        Location = new Point(Location.X, Location.Y + diff);
                        Height = Parent?.Height ?? 0 + Math.Abs(Location.Y);
                    }
                }
                //return;
            }
            
            base.OnMouseMove(e);

            if (_drawerItemRects == null)
            {
                UpdateTabRects();
            }

            Cursor previousCursor = Cursor;

            if (e.Location.X + Location.X < BORDER_WIDTH)
            {
                if (e.Location.Y > Height - BORDER_WIDTH)
                {
                    Cursor = Cursors.SizeNESW;                  //Bottom Left
                }
                else
                {
                    Cursor = Cursors.SizeWE;                    //Left
                }
            }
            else if (e.Location.Y > Height - BORDER_WIDTH)
            {
                Cursor = Cursors.SizeNS;                        //Bottom
            }
            else
            {
                if (_drawerItemRects != null && e.Location.Y < _drawerItemRects[^1].Bottom && (e.Location.X + Location.X) >= BORDER_WIDTH)
                {
                    Cursor = Cursors.Hand;
                }
                else
                {
                    Cursor = Cursors.Default;
                }
            }

            if (previousCursor != Cursor)
            {
                CursorUpdate?.Invoke(this, Cursor);
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            if (AutoShow && _isOpen==false)
            {
                Show();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            if (MouseState != MouseState.DOWN)
            {
                Cursor = Cursors.Default;
                CursorUpdate?.Invoke(this, Cursor);
            }

            if (AutoShow)
            {
                Hide();
            }
        }

        private void UpdateTabRects()
        {
            //If there isn't a base tab control, the rects shouldn't be calculated
            //or if there aren't tab pages in the base tab control, the list should just be empty
            if (_baseTabControl == null || _baseTabControl.TabCount == 0 || SkinManager == null || _drawerItemRects == null)
            {
                _drawerItemRects = new List<Rectangle>();
                _drawerItemPaths = new List<GraphicsPath>();
                return;
            }

            if (_drawerItemRects.Count != _baseTabControl.TabCount)
            {
                _drawerItemRects = new List<Rectangle>(_baseTabControl.TabCount);
                _drawerItemPaths = new List<GraphicsPath>(_baseTabControl.TabCount);

                for (int i = 0; i < _baseTabControl.TabCount; i++)
                {
                    _drawerItemRects.Add(new Rectangle());
                    _drawerItemPaths.Add(new GraphicsPath());
                }
            }

            //Calculate the bounds of each tab header specified in the base tab control
            for (int i = 0; i < _baseTabControl.TabPages.Count; i++)
            {
                _drawerItemRects[i] = new Rectangle(
                    (int)(SkinManager.FORM_PADDING * 0.75) - (ShowIconsWhenHidden ? Location.X : 0),
                    TAB_HEADER_PADDING * 2 * i + (SkinManager.FORM_PADDING >> 1),
                    Width + (ShowIconsWhenHidden ? Location.X : 0) - (int)(SkinManager.FORM_PADDING * 1.5) - 1,
                    drawerItemHeight);

                _drawerItemPaths[i] = DrawHelper.CreateRoundRect(new RectangleF(_drawerItemRects[i].X - 0.5f, _drawerItemRects[i].Y - 0.5f, _drawerItemRects[i].Width, _drawerItemRects[i].Height), 4);
            }
        }
    }
}
