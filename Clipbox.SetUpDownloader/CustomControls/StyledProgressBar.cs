using System;
using System.Drawing;
using System.Windows.Forms;

namespace Clipbpx.SetUpDownloader.CustomControls
{
    public class StyledProgressBar : ProgressBar
    {
        public StyledProgressBar()
        {
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.DoubleBuffer,
                true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Rectangle rec = e.ClipRectangle;

            rec.Width = (int)(rec.Width * ((double)Value / Maximum)) - 4;
            if (ProgressBarRenderer.IsSupported)
                ProgressBarRenderer.DrawHorizontalBar(e.Graphics, e.ClipRectangle);
            rec.Height = rec.Height - 4;

            e.Graphics.FillRectangle(new SolidBrush(BackColor), 0, 0, this.Width, this.Height);
            e.Graphics.FillRectangle(new SolidBrush(ForeColor), 2, 2, rec.Width, rec.Height);
        }
    }
}
