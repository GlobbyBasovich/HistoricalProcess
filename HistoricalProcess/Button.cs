using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace HistoricalProcess
{
    class Button : ICustomControl
    {
        public string Text { get; set; }
        public Bitmap Image;

        public int Width { get; set; }
        public int Height { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public bool Active { get; set; }
        public bool Selected;

        public Action Action;

        public bool IsHovered(int x, int y)
        {
            bool hMatch = X <= x && x < X + Width;
            bool vMatch = Y <= y && y < Y + Height;
            return hMatch && vMatch;
        }

        Font font;
        float stringYPos;
        public Bitmap Draw(ControlSituation situation)
        {
            Bitmap result = new Bitmap(Width, Height);
            Graphics canvas = Graphics.FromImage(result);
            canvas.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

            if (Image != null)
            {
                canvas.Clear(Constants.ForegroundColor[situation]);
                var rect = new Rectangle(5, 5, Width - 10, Height - 10);
                canvas.FillRectangle(new SolidBrush(Constants.BackgroundColor[situation]), rect);
                canvas.DrawImage(Image, rect);
            }
            else
            {
                canvas.Clear(Constants.ForegroundColor[situation]);
                canvas.FillRectangle(new SolidBrush(Constants.BackgroundColor[situation]), 5, 5, Width - 10, Height - 10);
                canvas.DrawString(Text, font, new SolidBrush(Constants.ForegroundColor[situation]), 10, stringYPos);
            }

            return result;
        }

        public Button(string text, int width, int height, Action action, bool active = true)
        {
            Text = text;
            Width = Math.Max(width, 30);
            Height = Math.Max(height, 30);
            Action = action;
            Active = active;

            Graphics canvas = Graphics.FromImage(new Bitmap(Width, Height));
            font = new Font(FontFamily.GenericSansSerif, 8);
            while (canvas.MeasureString(Text, font).Width < Width - 20)
            {
                font = new Font(FontFamily.GenericSansSerif, font.Size + 1);
            }
            font = new Font(FontFamily.GenericSansSerif, font.Size - 1);
            stringYPos = (Height - canvas.MeasureString(Text, font).Height) / 2;
        }

        public Button(Bitmap image, string label, int width, int height, Action action, bool active = true)
            : this(label, width, height, action, active)
        {
            Image = image;
        }
    }
}
