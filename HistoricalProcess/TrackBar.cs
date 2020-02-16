using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace HistoricalProcess
{
    class TrackBar : ICustomControl
    {
        public string Text { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public bool Active { get; set; }

        public int Radius;
        public int HLim;
        public int Position;

        public double Value
        {
            get
            {
                return (double)Position / HLim;
            }
            set
            {
                Position = (int)(value * HLim);
            }
        }

        public bool IsHovered(int x, int y)
        {
            int cx = X + 5 + Radius + Position;
            int cy = Y + 5 + stringHeight + 5 + Radius;
            return (cx - x) * (cx - x) + (cy - y) * (cy - y) <= Radius * Radius;
        }

        Font font;
        int stringHeight;
        public Bitmap Draw(ControlSituation situation)
        {
            Bitmap result = new Bitmap(Width, Height);
            Graphics canvas = Graphics.FromImage(result);

            canvas.Clear(Constants.BackgroundColor[ControlSituation.Inactive]);

            var brush = new SolidBrush(Constants.ForegroundColor[ControlSituation.Inactive]);
            canvas.DrawString(Text, font, brush, 10, 5);

            int y = 5 + stringHeight + 5;
            canvas.FillRectangle(brush, 5 + Radius, y + Radius - Height / 20, HLim, Height / 10);

            brush = new SolidBrush(Constants.ForegroundColor[situation]);
            canvas.FillEllipse(brush, 5 + Position, y, 2 * Radius, 2 * Radius);
            brush = new SolidBrush(Constants.BackgroundColor[situation]);
            canvas.FillEllipse(brush, 5 + Position + 2, y + 2, 2 * Radius - 4, 2 * Radius - 4);

            return result;
        }

        public TrackBar(string text, int width, int height, bool active = true)
        {
            Text = text;
            Width = Math.Max(width, 30);
            Height = Math.Max(height, Width / 10 + 10);
            Active = active;
            Radius = Width / 20;
            HLim = Width - 2 * (5 + Radius);

            Graphics canvas = Graphics.FromImage(new Bitmap(Width, Height));
            font = new Font(FontFamily.GenericSansSerif, 8);
            while (canvas.MeasureString(Text, font).Height < Height - 2 * (5 + Radius) - 5)
            {
                font = new Font(FontFamily.GenericSansSerif, font.Size + 1);
            }
            font = new Font(FontFamily.GenericSansSerif, font.Size - 1);
            stringHeight = (int)canvas.MeasureString(Text, font).Height;
        }
    }
}
