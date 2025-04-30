namespace MaterialSkin.Controls
{
    using System.ComponentModel;
    using System.Windows.Forms;
    using static System.Windows.Forms.TabControl;

    public class MaterialTabControl : TabControl, IMaterialControl
    {
        public MaterialTabControl()
        {
            Multiline = true;
            tabCollection = new MaterialTabPageCollection(this);
        }

        private readonly MaterialTabPageCollection tabCollection;

        public new MaterialTabPageCollection TabPages => tabCollection;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int Depth { get; set; }

        [Browsable(false)]
        public MaterialSkinManager SkinManager => MaterialSkinManager.Instance;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public MouseState MouseState { get; set; }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x1328 && !DesignMode)
            {
                m.Result = 1;
            }
            else
            {
                base.WndProc(ref m);
            }
        }
        
        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            if (e.Control != null)
            {
                e.Control.BackColor = Color.White;
            }
        }
    }

    public class MaterialTabPageCollection(TabControl owner) : TabPageCollection(owner) { }
}
