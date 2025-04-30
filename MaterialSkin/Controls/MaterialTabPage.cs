namespace MaterialSkin.Controls
{
    using System.ComponentModel;
    using System.Windows.Forms;

    public class MaterialTabPage : TabPage, IMaterialControl
    {
        private bool _drawIconSilhouette = true;

        [Category("Misc")]
        public bool DrawIconSilhouette
        {
            get
            {
                return _drawIconSilhouette;
            }
            set
            {
                if (_drawIconSilhouette != value)
                {
                    _drawIconSilhouette = value;
                }
            }
        }

        [Browsable(false)]
        public int Depth { get; set; }

        [Browsable(false)]
        public MaterialSkinManager SkinManager => MaterialSkinManager.Instance;

        [Browsable(false)]
        public MouseState MouseState { get; set; }
    }
}
