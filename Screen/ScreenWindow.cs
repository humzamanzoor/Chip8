using System.Drawing.Drawing2D;

namespace Chip8.Screen
{
    public class ScreenWindow : PictureBox
    {
        public InterpolationMode InterpolationMode { get; set; }

		protected override void OnPaint(PaintEventArgs paintEventArgs)
		{
			paintEventArgs.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
			paintEventArgs.Graphics.InterpolationMode = InterpolationMode;
			base.OnPaint(paintEventArgs);
		}
    }
}