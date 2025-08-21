using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BitCraftTimer
{
    [ToolboxItem(true)]
    [DesignerCategory("Component")]
    public class GradientPanel : Panel
    {
        private Color gradientStartColor = Color.FromArgb(40, 43, 52);
        private Color gradientEndColor = Color.FromArgb(32, 34, 42);
        private LinearGradientMode gradientDirection = LinearGradientMode.Vertical;
        private int cornerRadius = 10;
        private bool enableShadow = true;

        [Category("Appearance")]
        [Description("The starting color of the gradient.")]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(typeof(Color), "40, 43, 52")]
        public Color GradientStartColor
        {
            get { return gradientStartColor; }
            set { gradientStartColor = value; this.Invalidate(); }
        }

        [Category("Appearance")]
        [Description("The ending color of the gradient.")]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(typeof(Color), "32, 34, 42")]
        public Color GradientEndColor
        {
            get { return gradientEndColor; }
            set { gradientEndColor = value; this.Invalidate(); }
        }

        [Category("Appearance")]
        [Description("The direction of the gradient.")]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(LinearGradientMode.Vertical)]
        public LinearGradientMode GradientDirection
        {
            get { return gradientDirection; }
            set { gradientDirection = value; this.Invalidate(); }
        }

        [Category("Appearance")]
        [Description("The radius of the rounded corners.")]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(10)]
        public int CornerRadius
        {
            get { return cornerRadius; }
            set { cornerRadius = value; this.Invalidate(); }
        }

        [Category("Appearance")]
        [Description("Whether to enable the drop shadow effect.")]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(true)]
        public bool EnableShadow
        {
            get { return enableShadow; }
            set { enableShadow = value; this.Invalidate(); }
        }

        public GradientPanel()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | 
                         ControlStyles.UserPaint | 
                         ControlStyles.DoubleBuffer | 
                         ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(0, 0, this.Width, this.Height);
            
            // Draw shadow if enabled
            if (enableShadow)
            {
                Rectangle shadowRect = new Rectangle(rect.X + 3, rect.Y + 3, rect.Width, rect.Height);
                using (GraphicsPath shadowPath = GetRoundedPath(shadowRect, cornerRadius))
                using (PathGradientBrush shadowBrush = new PathGradientBrush(shadowPath))
                {
                    shadowBrush.CenterColor = Color.FromArgb(50, 0, 0, 0);
                    shadowBrush.SurroundColors = new Color[] { Color.FromArgb(0, 0, 0, 0) };
                    g.FillPath(shadowBrush, shadowPath);
                }
            }

            // Draw gradient background
            using (GraphicsPath path = GetRoundedPath(rect, cornerRadius))
            using (LinearGradientBrush brush = new LinearGradientBrush(rect, gradientStartColor, gradientEndColor, gradientDirection))
            {
                g.FillPath(brush, path);
            }

            // Draw border
            using (GraphicsPath borderPath = GetRoundedPath(rect, cornerRadius))
            using (Pen borderPen = new Pen(Color.FromArgb(60, 64, 72), 1))
            {
                g.DrawPath(borderPen, borderPath);
            }
        }

        private GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(rect.Location, size);

            // Top left arc
            path.AddArc(arc, 180, 90);

            // Top right arc
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);

            // Bottom right arc
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // Bottom left arc
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }
    }
}
