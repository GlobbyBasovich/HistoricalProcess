using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace HistoricalProcess
{
    [Serializable]
    public class Civilization
    {
        public string Name { get; private set; }

        public HashSet<Point> chunksCoords = new HashSet<Point>();

        public Color Color;

        public double Area { get; private set; } = 0;

        public Civilization(string name, Color color)
        {
            Name = name;
            Color = color;
        }

        public void Rename(string name)
        {
            Name = name;
            foreach (var coords in chunksCoords)
            {
                Program.AppForm.Terrain[coords.X][coords.Y].OwnerName = Name;
            }
        }

        public void Recolor(Color color)
        {
            Color = Color.FromArgb(0x66, color);

            Graphics canvas = Graphics.FromImage(Program.AppForm.graphicsCache["civBitmap"]);
            canvas.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            var brush = new SolidBrush(Color);
            foreach (var coords in chunksCoords)
            {
                canvas.FillRectangle(brush, coords.X, coords.Y, 1, 1);
            }
        }

        public void TakeChunk(object[] data)
        {
            var data0 = (object[])data[0];
            var canvas = (Graphics)data0[0];
            int x = (int)data[1];
            int y = (int)data[2];
            var brush = new SolidBrush(Color);

            if (Program.AppForm.Terrain[x][y].OwnerName == null)
            {
                chunksCoords.Add(new Point(x, y));
                Area += Program.AppForm.Terrain[x][y].Area;
                Program.AppForm.Terrain[x][y].OwnerName = Name;
                canvas.FillRectangle(brush, x, y, 1, 1);
            }
        }

        public void LoseChunk(object[] data)
        {
            var data0 = (object[])data[0];
            var canvas = (Graphics)data0[0];
            int x = (int)data[1];
            int y = (int)data[2];
            var brush = new SolidBrush(Color.FromArgb(0, Color.Black));

            if (Program.AppForm.Terrain[x][y].OwnerName == Name && chunksCoords.Count > 1)
            {
                chunksCoords.Remove(new Point(x, y));
                Area -= Program.AppForm.Terrain[x][y].Area;
                Program.AppForm.Terrain[x][y].OwnerName = null;
                canvas.FillRectangle(brush, x, y, 1, 1);
            }
        }

        public void Remove()
        {
            Graphics canvas = Graphics.FromImage(Program.AppForm.graphicsCache["civBitmap"]);
            canvas.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            var brush = new SolidBrush(Color.FromArgb(0, Color.Black));

            foreach (var coords in chunksCoords)
            {
                canvas.FillRectangle(brush, coords.X, coords.Y, 1, 1);
                Program.AppForm.Terrain[coords.X][coords.Y].OwnerName = null;
            }
        }

        public Civilization() { } //For BoisSerializer
    }
}
