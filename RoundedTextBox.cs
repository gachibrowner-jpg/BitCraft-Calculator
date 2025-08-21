using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BitCraftTimer
{
    [ToolboxItem(true)]
    [DesignerCategory("Component")]
    [DefaultProperty("Text")]
    [DefaultBindingProperty("Text")]
    public class RoundedTextBox : TextBox
    {
        private Color _borderColor = Color.Gray;
        private int _borderRadius = 10;

        [Category("Appearance")]
        [Description("The color of the textbox's border.")]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(typeof(Color), "Gray")]
        public Color BorderColor
        {
            get { return _borderColor; }
            set
            {
                _borderColor = value;
                this.Invalidate();
            }
        }

        [Category("Appearance")]
        [Description("The radius of the textbox's corners.")]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(10)]
        public int BorderRadius
        {
            get { return _borderRadius; }
            set
            {
                _borderRadius = value;
                this.Invalidate();
            }
        }

        public RoundedTextBox()
        {
            this.BorderStyle = BorderStyle.None;
            this.Padding = new Padding(10);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = new GraphicsPath())
            using (Pen pen = new Pen(_borderColor, 1))
            {
                path.AddArc(new Rectangle(0, 0, _borderRadius, _borderRadius), 180, 90);
                path.AddArc(new Rectangle(this.Width - _borderRadius - 1, 0, _borderRadius, _borderRadius), -90, 90);
                path.AddArc(new Rectangle(this.Width - _borderRadius - 1, this.Height - _borderRadius - 1, _borderRadius, _borderRadius), 0, 90);
                path.AddArc(new Rectangle(0, this.Height - _borderRadius - 1, _borderRadius, _borderRadius), 90, 90);
                path.CloseAllFigures();

                this.Region = new Region(path);
                g.DrawPath(pen, path);
            }
        }
    }
}
