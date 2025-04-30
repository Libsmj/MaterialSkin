namespace MaterialSkin.Controls
{
    using MaterialSkin.Animations;
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Linq;
    using System.Data;
    using System.Windows.Forms;
    using MaterialSkin;

    public class MaterialComboBox : ComboBox, IMaterialControl
    {
        // For some reason, even when overriding the AutoSize property, it doesn't appear on the properties panel, so we have to create a new one.
        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always), Category("Layout")]
        private bool _AutoResize;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool AutoResize
        {
            get { return _AutoResize; }
            set
            {
                _AutoResize = value;
                RecalculateAutoSize();
            }
        }

        //Properties for managing the material design properties
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int Depth { get; set; }

        [Browsable(false)]
        public MaterialSkinManager SkinManager => MaterialSkinManager.Instance;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public MouseState MouseState { get; set; }

        private bool _UseTallSize;

        [Category("Material Skin"), DefaultValue(true), Description("Using a larger size enables the hint to always be visible")]
        public bool UseTallSize
        {
            get { return _UseTallSize; }
            set
            {
                _UseTallSize = value;
                SetHeightVars();
                Invalidate();
            }
        }

        [Category("Material Skin"), DefaultValue(true)]
        public bool UseAccent { get; set; }

        private string _hint = string.Empty;

        [Category("Material Skin"), DefaultValue(""), Localizable(true)]
        public string Hint
        {
            get { return _hint; }
            set
            {
                _hint = value;
                hasHint = !String.IsNullOrEmpty(Hint);
                Invalidate();
            }
        }

        private int _startIndex;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int StartIndex
        {
            get => _startIndex;
            set
            {
                _startIndex = value;
                try
                {
                    if (Items.Count > 0)
                    {
                        base.SelectedIndex = value;
                    }
                }
                catch
                {
                }
                Invalidate();
            }
        }

        private const int TEXT_SMALL_SIZE = 18;
        private const int TEXT_SMALL_Y = 4;
        private const int BOTTOM_PADDING = 3;
        private int HEIGHT = 50;
        private int LINE_Y;

        private bool hasHint;

        private readonly AnimationManager _animationManager;

        public MaterialComboBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            // Material Properties
            Hint = "";
            UseAccent = true;
            UseTallSize = true;
            MaxDropDownItems = 4;

            Font = SkinManager.GetFontByType(MaterialSkinManager.FontType.Subtitle2);
            BackColor = SkinManager.BackgroundColor;
            ForeColor = SkinManager.TextHighEmphasisColor;
            DrawMode = DrawMode.OwnerDrawVariable;
            DropDownStyle = ComboBoxStyle.DropDownList;
            DropDownWidth = Width;

            // Animations
            _animationManager = new AnimationManager(true)
            {
                Increment = 0.08,
                AnimationType = AnimationType.EaseInOut
            };
            _animationManager.OnAnimationProgress += sender => Invalidate();
            _animationManager.OnAnimationFinished += sender => _animationManager.SetProgress(0);
            DropDownClosed += (sender, args) =>
            {
                MouseState = MouseState.OUT;
                if (SelectedIndex < 0 && !Focused)
                {
                    _animationManager.StartNewAnimation(AnimationDirection.Out);
                }
            };
            LostFocus += (sender, args) =>
            {
                MouseState = MouseState.OUT;
                if (SelectedIndex < 0)
                {
                    _animationManager.StartNewAnimation(AnimationDirection.Out);
                }
            };
            DropDown += (sender, args) =>
            {
                _animationManager.StartNewAnimation(AnimationDirection.In);
            };
            GotFocus += (sender, args) =>
            {
                _animationManager.StartNewAnimation(AnimationDirection.In);
                Invalidate();
            };
            MouseEnter += (sender, args) =>
            {
                MouseState = MouseState.HOVER;
                Invalidate();
            };
            MouseLeave += (sender, args) =>
            {
                MouseState = MouseState.OUT;
                Invalidate();
            };
            SelectedIndexChanged += (sender, args) =>
            {
                Invalidate();
            };
            KeyUp += (sender, args) =>
            { 
                if (Enabled && DropDownStyle == ComboBoxStyle.DropDownList && (args.KeyCode == Keys.Delete || args.KeyCode == Keys.Back))
                {
                    SelectedIndex = -1;
                    Invalidate();
                }
            };
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;

            if (Parent != null)
            {
                g.Clear(Parent.BackColor);
            }
            g.FillRectangle(Enabled ? Focused ?
                SkinManager.BackgroundFocusBrush : // Focused
                MouseState == MouseState.HOVER ?
                SkinManager.BackgroundHoverBrush : // Hover
                SkinManager.BackgroundAlternativeBrush : // normal
                SkinManager.BackgroundDisabledBrush // Disabled
                , ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, LINE_Y);

            //Set color and brush
            _ = new Color();
            Color SelectedColor;
            if (UseAccent)
            {
                SelectedColor = SkinManager.ColorScheme.AccentColor;
            }
            else
            {
                SelectedColor = SkinManager.ColorScheme.PrimaryColor;
            }

            SolidBrush SelectedBrush = new SolidBrush(SelectedColor);

            // Create and Draw the arrow
            System.Drawing.Drawing2D.GraphicsPath pth = new System.Drawing.Drawing2D.GraphicsPath();
            PointF TopRight = new PointF(Width - 0.5f - SkinManager.FORM_PADDING, (Height >> 1) - 2.5f);
            PointF MidBottom = new PointF(Width - 4.5f - SkinManager.FORM_PADDING, (Height >> 1) + 2.5f);
            PointF TopLeft = new PointF(Width - 8.5f - SkinManager.FORM_PADDING, (Height >> 1) - 2.5f);
            pth.AddLine(TopLeft, TopRight);
            pth.AddLine(TopRight, MidBottom);

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.FillPath((SolidBrush)(Enabled ? DroppedDown || Focused ?
                SelectedBrush : //DroppedDown or Focused
                SkinManager.TextHighEmphasisBrush : //Not DroppedDown and not Focused
                new SolidBrush(DrawHelper.BlendColor(SkinManager.TextHighEmphasisColor, SkinManager.SwitchOffDisabledThumbColor, 197))  //Disabled
                ), pth);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

            // HintText
            bool userTextPresent = SelectedIndex >= 0;
            Rectangle hintRect = new Rectangle(SkinManager.FORM_PADDING, ClientRectangle.Y, Width, LINE_Y);
            int hintTextSize = 16;

            // bottom line base
            g.FillRectangle(SkinManager.DividersAlternativeBrush, 0, LINE_Y, Width, 1);

            if (!_animationManager.IsAnimating())
            {
                // No animation
                if (hasHint && UseTallSize && (DroppedDown || Focused || SelectedIndex >= 0))
                {
                    // hint text
                    hintRect = new Rectangle(SkinManager.FORM_PADDING, TEXT_SMALL_Y, Width, TEXT_SMALL_SIZE);
                    hintTextSize = 12;
                }

                // bottom line
                if (DroppedDown || Focused)
                {
                    g.FillRectangle(SelectedBrush, 0, LINE_Y, Width, 2);
                }
            }
            else
            {
                // Animate - Focus got/lost
                double animationProgress = _animationManager.GetProgress();

                // hint Animation
                if (hasHint && UseTallSize)
                {
                    hintRect = new Rectangle(
                        SkinManager.FORM_PADDING,
                        userTextPresent && !_animationManager.IsAnimating() ? TEXT_SMALL_Y : ClientRectangle.Y + (int)((TEXT_SMALL_Y - ClientRectangle.Y) * animationProgress),
                        Width,
                        userTextPresent && !_animationManager.IsAnimating() ? TEXT_SMALL_SIZE : (int)(LINE_Y + (TEXT_SMALL_SIZE - LINE_Y) * animationProgress));
                    hintTextSize = userTextPresent && !_animationManager.IsAnimating() ? 12 : (int)(16 + (12 - 16) * animationProgress);
                }

                // Line Animation
                int LineAnimationWidth = (int)(Width * animationProgress);
                int LineAnimationX = (Width / 2) - (LineAnimationWidth / 2);
                g.FillRectangle(SelectedBrush, LineAnimationX, LINE_Y, LineAnimationWidth, 2);
            }

            // Calc text Rect
            Rectangle textRect = new Rectangle(
                SkinManager.FORM_PADDING,
                hasHint && UseTallSize ? hintRect.Y + hintRect.Height - 2 : ClientRectangle.Y,
                ClientRectangle.Width - SkinManager.FORM_PADDING * 3 - 8,
                hasHint && UseTallSize ? LINE_Y - (hintRect.Y + hintRect.Height) : LINE_Y);

            g.Clip = new Region(textRect);

            using (NativeTextRenderer NativeText = new NativeTextRenderer(g))
            {
                // Draw user text
                NativeText.DrawTransparentText(
                    Text,
                    SkinManager.GetLogFontByType(MaterialSkinManager.FontType.Subtitle1),
                    Enabled ? SkinManager.TextHighEmphasisColor : SkinManager.TextDisabledOrHintColor,
                    textRect.Location,
                    textRect.Size,
                    NativeTextRenderer.TextAlignFlags.Left | NativeTextRenderer.TextAlignFlags.Middle);
            }

            g.ResetClip();

            // Draw hint text
            if (hasHint && (UseTallSize || String.IsNullOrEmpty(Text)))
            {
                using (NativeTextRenderer NativeText = new NativeTextRenderer(g))
                {
                    NativeText.DrawTransparentText(
                    Hint,
                    SkinManager.GetTextBoxFontBySize(hintTextSize),
                    Enabled ? DroppedDown || Focused ? 
                    SelectedColor : // Focus 
                    SkinManager.TextMediumEmphasisColor : // not focused
                    SkinManager.TextDisabledOrHintColor, // Disabled
                    hintRect.Location,
                    hintRect.Size,
                    NativeTextRenderer.TextAlignFlags.Left | NativeTextRenderer.TextAlignFlags.Middle);
                }
            }
        }

        private void CustomMeasureItem(object? sender, MeasureItemEventArgs e)
        {
            e.ItemHeight = HEIGHT - 7;
        }

        private void CustomDrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index > Items.Count || !Focused)
            {
                return;
            }

            Graphics g = e.Graphics;

            // Draw the background of the item.
            g.FillRectangle(SkinManager.BackgroundBrush, e.Bounds);

            // Hover
            if (e.State.HasFlag(DrawItemState.Focus)) // Focus == hover
            {
                g.FillRectangle(SkinManager.BackgroundHoverBrush, e.Bounds);
            }

            string? Text = string.Empty;
            var currentItem = Items[e.Index];
            if (!string.IsNullOrWhiteSpace(DisplayMember) && currentItem != null)
            {
                if (!currentItem.GetType().Equals(typeof(DataRowView)))
                {
                    object? item = currentItem.GetType().GetProperty(DisplayMember)?.GetValue(currentItem);
                    Text = item?.ToString();
                }
                else
                {
                    object? item = currentItem.GetType().GetProperty("Row")?.GetValue(currentItem);
                    if (item is DataRow dataRow)
                    {
                        DataTable table = dataRow.Table;
                        Text = table.Rows[e.Index][DisplayMember].ToString();
                    }
                }
            }
            else
            {
                Text = currentItem?.ToString();
            }

            using (NativeTextRenderer NativeText = new NativeTextRenderer(g))
            {
                NativeText.DrawTransparentText(
                    Text ?? string.Empty,
                    SkinManager.GetFontByType(MaterialSkinManager.FontType.Subtitle1),
                    SkinManager.TextHighEmphasisNoAlphaColor,
                    new Point(e.Bounds.Location.X + SkinManager.FORM_PADDING, e.Bounds.Location.Y),
                    new Size(e.Bounds.Size.Width - SkinManager.FORM_PADDING * 2, e.Bounds.Size.Height),
                    NativeTextRenderer.TextAlignFlags.Left | NativeTextRenderer.TextAlignFlags.Middle);
            }
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            MouseState = MouseState.OUT;
            MeasureItem += CustomMeasureItem;
            DrawItem += CustomDrawItem;
            DropDownStyle = ComboBoxStyle.DropDownList;
            DrawMode = DrawMode.OwnerDrawVariable;
            RecalculateAutoSize();
            SetHeightVars();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            RecalculateAutoSize();
            SetHeightVars();
        }

        private void SetHeightVars()
        {
            HEIGHT = UseTallSize ? 50 : 36;
            Size = new Size(Size.Width, HEIGHT);
            LINE_Y = HEIGHT - BOTTOM_PADDING;
            ItemHeight = HEIGHT - 7;
            DropDownHeight = ItemHeight * MaxDropDownItems + 2;
        }

        public void RecalculateAutoSize()
        {
            if (!AutoResize)
            {
                return;
            }

            int w = DropDownWidth;
            int padding = SkinManager.FORM_PADDING * 3;
            int vertScrollBarWidth = (Items.Count > MaxDropDownItems) ? SystemInformation.VerticalScrollBarWidth : 0;

            Graphics g = CreateGraphics();
            using (NativeTextRenderer NativeText = new NativeTextRenderer(g))
            {
                IEnumerable<string?> itemsList = Items.Cast<object>().Select(item => item.ToString());
                foreach (string? s in itemsList)
                {
                    int newWidth = NativeText.MeasureLogString(s ?? string.Empty, SkinManager.GetLogFontByType(MaterialSkinManager.FontType.Subtitle1)).Width + vertScrollBarWidth + padding;
                    if (w < newWidth)
                    {
                        w = newWidth;
                    }
                }
            }

            if (Width != w)
            {
                DropDownWidth = w;
                Width = w;
            }
        }
    }
}
