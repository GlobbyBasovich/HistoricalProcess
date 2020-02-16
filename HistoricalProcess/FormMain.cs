using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Serialization.Formatters.Binary;
using HistoricalProcess.Properties;
using Salar.Bois;

namespace HistoricalProcess
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
        }

        private BufferedGraphics graphics;
        public Dictionary<string, Bitmap> graphicsCache = new Dictionary<string, Bitmap>();
        BinaryFormatter dotnetSerializer = new BinaryFormatter();

        public Dictionary<string, object> cache = new Dictionary<string, object>();

        public Chunk[][] Terrain;
        private int hOffset = 0, vOffset = 0, scaleQ = 1;
        public Dictionary<string, Civilization> Mankind = new Dictionary<string, Civilization>();
        public Civilization currentCivilization;

        private List<ICustomControl> controls = new List<ICustomControl>();

        private Order order = Order.None;

        private void FormMain_Load(object sender, EventArgs e)
        {
            Width = Screen.PrimaryScreen.Bounds.Width;
            Height = Screen.PrimaryScreen.Bounds.Height;
            graphics = BufferedGraphicsManager.Current.Allocate(CreateGraphics(), ClientRectangle);
            graphics.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            graphics.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            int buttonWidth = (int)(Width * 0.15);
            int buttonHeight = Height / 12;
            controls.Add(new Button("Меню", buttonWidth, buttonHeight, ShowMenu));
            controls.Add(new Button("Экономика", buttonWidth, buttonHeight, EconomicsScreenToggle, false));
            controls.Add(new Button("Внутренняя политика", buttonWidth, buttonHeight, DomesticPolicyScreenToggle, false));
            controls.Add(new Button("Внешняя политика", buttonWidth, buttonHeight, ForeignPolicyScreenToggle, false));
            controls.Add(new Button("Культура", buttonWidth, buttonHeight, CultureScreenToggle, false));
            controls.Add(new Button("Технологии", buttonWidth, buttonHeight, TechScreenToggle, false));
            cache["controlsScreensEnd"] = controls.Count;

            for (int i = 1; i < controls.Count; i++)
            {
                controls[i].Y = controls[i - 1].Y + controls[i - 1].Height;
            }

            int columnCount = 4;
            int cs = (int)(Width * 0.15 / columnCount);

            controls.Add(new TrackBar("Радиус действия", (int)(Width * 0.15), cs, false));
            (controls.Last() as TrackBar).Value = 0.5;
            cache["controlsTrackbarsEnd"] = controls.Count;

            for (int i = (int)cache["controlsScreensEnd"]; i < controls.Count; i++)
            {
                controls[i].Y = controls[i - 1].Y + controls[i - 1].Height;
            }

            controls.Add(new Button(Resources.Image_HeightTool, " Изменить высоту", cs, cs, HeightToolToggle));
            controls.Add(new Button(Resources.Image_MoistureTool, " Изменить влажность", cs, cs, MoistureToolToggle));
            controls.Add(new Button(Resources.Image_TemperatureTool, " Изменить температуру", cs, cs, TemperatureToolToggle));
            controls.Add(new Button(Resources.Image_CivManageTool, " Создать/уничтожить цивилизацию", cs, cs, CivManageToolToggle));
            controls.Add(new Button(Resources.Image_CivSelectTool, " Выбрать цивилизацию", cs, cs, CivSelectToolToggle));
            controls.Add(new Button(Resources.Image_CivAreaTool, " Изменить территорию", cs, cs, CivAreaToolToggle, false));
            cache["controlsToolsEnd"] = controls.Count;

            for (int i = (int)cache["controlsTrackbarsEnd"]; i < controls.Count; i++)
            {
                controls[i].X = controls[i - 1].X + controls[i - 1].Width;
                controls[i].Y = controls[i - 1].Y;
                if ((i - (int)cache["controlsTrackbarsEnd"]) % columnCount == 0)
                {
                    controls[i].X = 0;
                    controls[i].Y += controls[i - 1].Height;
                }
            }

            BoisSerializer.Initialize(typeof(Chunk), typeof(Civilization));

            Frame.Start();
        }

        private void FormMain_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.R) CreateMap();
            if (e.KeyCode == Keys.Escape && order > Order.Menu)
            {
                var button = (Button)controls.Find(x => x is Button
                    && (x as Button).Selected == true);
                ScreenToggle(order, button);
            }
        }

        private void FormMain_MouseDown(object sender, MouseEventArgs e)
        {
            fixedX = MousePosition.X;
            fixedY = MousePosition.Y;
        }

        private void FormMain_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var buttons = controls.Where(c => c is Button);
                Button button = null;
                foreach (var button0 in buttons)
                {
                    if (button0.Active && button0.IsHovered(e.X, e.Y))
                    {
                        button = (Button)button0;
                        break;
                    }
                }
                button?.Action();
            }
            fixedX = -1;
            fixedY = -1;
        }

        private void FormMain_MouseWheel(object sender, MouseEventArgs e)
        {
            int tmp = Math.Min(Math.Max((int)(scaleQ * Math.Pow(2, Math.Sign(e.Delta))), 1), 16);
            if (order < Order.Menu && scaleQ != tmp)
            {
                bool shouldAdjust = e.X >= (int)(Width * 0.15);
                shouldAdjust = shouldAdjust && scaleQ * Terrain.Length >= Width - (int)(Width * 0.15);
                shouldAdjust = shouldAdjust && scaleQ * Terrain[0].Length >= Height;
                shouldAdjust = shouldAdjust && tmp * Terrain.Length >= Width - (int)(Width * 0.15);
                shouldAdjust = shouldAdjust && tmp * Terrain[0].Length >= Height;

                int sX = ((e.X - (int)(Width * 0.15)) / scaleQ + hOffset) % Terrain.Length;
                int sY = e.Y / scaleQ + vOffset;
                scaleQ = tmp;

                if (shouldAdjust)
                {
                    hOffset = sX - (e.X - (int)(Width * 0.15)) / scaleQ;
                    if (hOffset >= Terrain.Length) hOffset -= Terrain.Length;
                    else if (hOffset < 0) hOffset += Terrain.Length;
                    vOffset = sY - e.Y / scaleQ;
                }

                vOffset = Math.Max(Math.Min(vOffset, (scaleQ * Terrain[0].Length - Height) / scaleQ), 0);
            }
        }

        private void ShowMenu()
        {
            cache["orderMenu"] = order;
            order = Order.Menu;
            int buttonWidth = (int)(Width * 0.15);
            int buttonHeight = Height / 12;

            for (int i = 0; i < controls.Count; i++)
            {
                cache[$"controlActiveMenu{i}"] = controls[i].Active;
                controls[i].Active = false;
            }
            cache[$"controlCount"] = controls.Count;

            controls.Add(new Button("Продолжить", buttonWidth, buttonHeight, HideMenu));
            controls.Add(new Button("Создать карту", buttonWidth, buttonHeight, CreateMap));
            controls.Add(new Button("Сохранить", buttonWidth, buttonHeight, DataSave));
            controls.Add(new Button("Загрузить", buttonWidth, buttonHeight, DataLoad));
            controls.Add(new Button("Выйти", buttonWidth, buttonHeight, AppExit));

            int x = (Width - buttonWidth) / 2;
            int y = (Height - buttonHeight * (controls.Count - (int)cache[$"controlCount"])) / 2;
            for (int i = (int)cache[$"controlCount"]; i < controls.Count; i++)
            {
                controls[i].X = x;
                controls[i].Y = y + (i - (int)cache[$"controlCount"]) * controls[i].Height;
            }
        }

        private void HideMenu()
        {
            order = (Order)cache["orderMenu"];
            controls.RemoveRange((int)cache[$"controlCount"],
                controls.Count - (int)cache[$"controlCount"]);

            for (int i = 0; i < controls.Count; i++)
            {
                controls[i].Active = (bool)cache[$"controlActiveMenu{i}"];
            }

            Frame_Tick(null, null);
        }

        private void ResetMap()
        {
            hOffset = 0;
            vOffset = 0;
            scaleQ = 1;
            currentCivilization = null;
            Mankind.Clear();
        }

        private void CreateMap()
        {
            Frame.Stop();

            var createMapDialog = new CreateMapDialog();
            int min = (int)createMapDialog.numericUpDownWidth.Minimum;
            int max = (int)createMapDialog.numericUpDownWidth.Maximum;
            createMapDialog.numericUpDownWidth.Value = Math.Min(Math.Max((int)(Width * 0.85), min), max);
            min = (int)createMapDialog.numericUpDownHeight.Minimum;
            max = (int)createMapDialog.numericUpDownHeight.Maximum;
            createMapDialog.numericUpDownHeight.Value = Math.Min(Math.Max(Height, min), max);

            if (createMapDialog.ShowDialog() == DialogResult.OK)
            {
                HideMenu();
                DrawLoading("Создание карты...");

                ResetMap();
                int w = (int)createMapDialog.numericUpDownWidth.Value;
                int h = (int)createMapDialog.numericUpDownHeight.Value;
                graphicsCache["terrainBitmap"] = TerrainGen(w, h);
                graphicsCache["civBitmap"] = new Bitmap(w, h);
            }
            createMapDialog.Dispose();

            Frame.Start();
        }

        private Bitmap TerrainGen(int width, int height)
        {
            Terrain = new Chunk[width][];

            Bitmap bmp = new Bitmap(width, height);
            Graphics canvas = Graphics.FromImage(bmp);

            SimplexNoise heightNoise = new SimplexNoise(DateTime.Now.GetHashCode());
            SimplexNoise temperatureNoise = new SimplexNoise(DateTime.Now.GetHashCode() + 1);
            SimplexNoise moistureNoise = new SimplexNoise(DateTime.Now.GetHashCode() + 2);

            var heightMap = new double[width, height];
            var temperatureMap = new double[width, height];
            var moistureMap = new double[width, height];
            double heightMax = 0.0, temperatureMax = 0.0, moistureMax = 0.0;
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    double s = (double)i / width;

                    double nx = Math.Cos(s * 2 * Math.PI) / (2 * Math.PI);
                    double ny = Math.Sin(s * 2 * Math.PI) / (2 * Math.PI);
                    double nz = (double)j / height;

                    heightMap[i, j] = heightNoise.Multi(8, nx * 3, ny * 3, nz * 3);
                    heightMax = Math.Max(heightMax, heightMap[i, j]);
                    temperatureMap[i, j] = temperatureNoise.Multi(8, nx * 3, ny * 3, nz * 3);
                    temperatureMax = Math.Max(temperatureMax, temperatureMap[i, j]);
                    moistureMap[i, j] = moistureNoise.Multi(8, nx * 3, ny * 3, nz * 3);
                    moistureMax = Math.Max(moistureMax, moistureMap[i, j]);
                }
            }

            for (int i = 0; i < width; i++)
            {
                Terrain[i] = new Chunk[height];
                for (int j = 0; j < height; j++)
                {
                    double chunkHeight = heightMap[i, j] / heightMax;

                    double r = temperatureMap[i, j] / temperatureMax;
                    double m = (height / 2.0 - Math.Abs(height / 2.0 - j)) / height * 4 - 1;
                    double temperature = 0.8 * m + 0.2 * r;
                    if (chunkHeight > Constants.SeaLevel)
                    {
                        double h = 0.5 * (m + 1) * Constants.HeightCoolingStep * (chunkHeight - Constants.SeaLevel);
                        temperature -= h;
                    }
                    double moisture = moistureMap[i, j] / moistureMax;

                    Terrain[i][j] = new Chunk(i, j, chunkHeight, temperature, moisture);

                    canvas.FillRectangle(new SolidBrush(Terrain[i][j].DrawColor), i, j, 1, 1);
                }
            }

            return bmp;
        }

        private void DataSave()
        {
            Frame.Stop();

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                HideMenu();
                DrawLoading("Сохранение...");

                var file = new FileStream(saveFileDialog.FileName, FileMode.Create);
                var arc = new ZipArchive(file, ZipArchiveMode.Create);

                var entry = arc.CreateEntry("graphicsCache.dat").Open();
                dotnetSerializer.Serialize(entry, graphicsCache);
                entry.Close();

                var boisSerializer = new BoisSerializer();

                entry = arc.CreateEntry("terrain.dat").Open();
                boisSerializer.Serialize(Terrain, entry);
                entry.Close();

                entry = arc.CreateEntry("mankind.dat").Open();
                boisSerializer.Serialize(Mankind, entry);
                entry.Close();

                arc.Dispose();
                file.Close();
            }

            Frame.Start();
        }

        private void DataLoad()
        {
            Frame.Stop();

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                HideMenu();
                DrawLoading("Загрузка...");

                ResetMap();

                var file = new FileStream(openFileDialog.FileName, FileMode.Open);
                var arc = new ZipArchive(file, ZipArchiveMode.Read);

                var entry = arc.GetEntry("graphicsCache.dat").Open();
                graphicsCache = (Dictionary<string, Bitmap>)dotnetSerializer.Deserialize(entry);
                entry.Close();

                var boisSerializer = new BoisSerializer();

                entry = arc.GetEntry("terrain.dat").Open();
                Terrain = boisSerializer.Deserialize<Chunk[][]>(entry);
                entry.Close();

                entry = arc.GetEntry("mankind.dat").Open();
                Mankind = boisSerializer.Deserialize<Dictionary<string, Civilization>>(entry);
                entry.Close();

                arc.Dispose();
                file.Close();
            }

            Frame.Start();
        }

        private void AppExit()
        {
            Frame.Stop();

            string text = "Вы действительно хотите выйти?";
            var tParam2 = MessageBoxButtons.YesNo;
            var tParam3 = MessageBoxIcon.Question;
            var result = MessageBox.Show(text, "Выход", tParam2, tParam3);

            if (result == DialogResult.Yes) Close();
            
            Frame.Start();
        }

        private void ScreenToggle(Order screen, Button button)
        {
            if (order < Order.Menu) cache["order"] = order;

            var block = controls.Take((int)cache["controlsTrackbarsEnd"]).ToList();
            if (button.Selected)
            {
                for (int i = (int)cache["controlsScreensEnd"]; i < (int)cache["controlsToolsEnd"]; i++)
                {
                    controls[i].Active = (bool)cache[$"controlActive{i}"];
                }
                order = (Order)cache["order"];
                button.Selected = false;
            }
            else
            {
                foreach (var b in block.Where(x => x is Button))
                {
                    (b as Button).Selected = false;
                }
                if (order < Order.Menu)
                    for (int i = (int)cache["controlsScreensEnd"]; i < (int)cache["controlsToolsEnd"]; i++)
                    {
                        cache[$"controlActive{i}"] = controls[i].Active;
                        controls[i].Active = false;
                    }
                order = screen;
                button.Selected = true;
            }
            controls.RemoveRange((int)cache["controlsToolsEnd"], controls.Count - (int)cache["controlsToolsEnd"]);
        }

        private void EconomicsScreenToggle()
        {
            var b = (Button)controls.Where(c => c is Button)
                .Where(c => ((Button)c).Action == EconomicsScreenToggle).First();
            ScreenToggle(Order.EconomicsScreen, b);
        }

        private void DomesticPolicyScreenToggle()
        {
            var b = (Button)controls.Where(c => c is Button)
                   .Where(c => ((Button)c).Action == DomesticPolicyScreenToggle).First();
            ScreenToggle(Order.DomesticPolicyScreen, b);
        }

        private void ForeignPolicyScreenToggle()
        {
            var b = (Button)controls.Where(c => c is Button)
                   .Where(c => ((Button)c).Action == ForeignPolicyScreenToggle).First();
            ScreenToggle(Order.ForeignPolicyScreen, b);

            if (order == Order.ForeignPolicyScreen)
            {
                int buttonWidth = (int)(Width * 0.1);
                int buttonHeight = Height / 18;

                controls.Add(new Button("Переименовать страну", buttonWidth, buttonHeight, CivRename));
                controls.Add(new Button("Изменить цвет", buttonWidth, buttonHeight, CivRecolor));

                controls[(int)cache["controlsToolsEnd"]].X = (int)(Width * 0.15) + 5;
                controls[(int)cache["controlsToolsEnd"]].Y = 5;
                for (int i = (int)cache["controlsToolsEnd"] + 1; i < controls.Count; i++)
                {
                    controls[i].X = controls[i - 1].X;
                    controls[i].Y = controls[i - 1].Y + controls[i - 1].Height + 5;
                    if (controls[i].Y + controls[i].Height > Height)
                    {
                        controls[i].X += controls[i - 1].Width + 5;
                        controls[i].Y = 5;
                    }
                }
            }
        }

        private void CultureScreenToggle()
        {
            var b = (Button)controls.Where(c => c is Button)
                   .Where(c => ((Button)c).Action == CultureScreenToggle).First();
            ScreenToggle(Order.CultureScreen, b);
        }

        private void TechScreenToggle()
        {
            var b = (Button)controls.Where(c => c is Button)
                   .Where(c => ((Button)c).Action == TechScreenToggle).First();
            ScreenToggle(Order.TechScreen, b);
        }

        private void ToolToggle(string tool)
        {
            Action action;
            Order toolOrder;
            bool radiusNeeded = true;
            switch (tool)
            {
                case "Height":
                    action = HeightToolToggle;
                    toolOrder = Order.HeightTool;
                    break;
                case "Moisture":
                    action = MoistureToolToggle;
                    toolOrder = Order.MoistureTool;
                    break;
                case "Temperature":
                    action = TemperatureToolToggle;
                    toolOrder = Order.TemperatureTool;
                    break;
                case "CivManage":
                    action = CivManageToolToggle;
                    toolOrder = Order.CivManageTool;
                    radiusNeeded = false;
                    break;
                case "CivSelect":
                    action = CivSelectToolToggle;
                    toolOrder = Order.CivSelectTool;
                    radiusNeeded = false;
                    break;
                case "CivArea":
                    action = CivAreaToolToggle;
                    toolOrder = Order.CivAreaTool;
                    break;
                default:
                    throw new ArgumentException("Invalid ToolToggle call", "tool");
            }
            var block = controls.Skip((int)cache["controlsTrackbarsEnd"]).
                Take((int)cache["controlsToolsEnd"] - (int)cache["controlsTrackbarsEnd"]).ToList();
            var button = (Button)block.Find(x => x is Button && (x as Button).Action == action);
            if (button.Selected)
            {
                order = Order.None;
                button.Selected = false;
                (controls.Where(c => c is TrackBar).ToArray()[0] as TrackBar).Active = false;
            }
            else
            {
                order = toolOrder;
                foreach (var b in block.Where(x => x is Button))
                {
                    (b as Button).Selected = false;
                }
                button.Selected = true;
                (controls.Where(c => c is TrackBar).ToArray()[0] as TrackBar).Active = radiusNeeded;
            }
        }

        private void HeightToolToggle()
        {
            ToolToggle("Height");
        }

        private void MoistureToolToggle()
        {
            ToolToggle("Moisture");
        }

        private void TemperatureToolToggle()
        {
            ToolToggle("Temperature");
        }

        private void CivManageToolToggle()
        {
            ToolToggle("CivManage");
        }

        private void CivSelectToolToggle()
        {
            ToolToggle("CivSelect");
        }

        private void CivAreaToolToggle()
        {
            ToolToggle("CivArea");
        }

        private void ApplyTool(int x, int y, double power)
        {
            var civControls = controls.Where(c => c is Button)
                .Where(c => ((Button)c).Action == CivAreaToolToggle ||
                    ((Button)c).Action == ForeignPolicyScreenToggle ||
                    ((Button)c).Action == EconomicsScreenToggle);
            switch (order)
            {
                case Order.HeightTool:
                    {
                        var canvas = Graphics.FromImage(graphicsCache["terrainBitmap"]);
                        int r = (int)(50 * (controls.Where(c => c is TrackBar).ToArray()[0] as TrackBar).Value);
                        CircleProcess(x, y, r, HeightChange, new object[] { canvas, power });
                    }
                    break;
                case Order.MoistureTool:
                    {
                        var canvas = Graphics.FromImage(graphicsCache["terrainBitmap"]);
                        int r = (int)(50 * (controls.Where(c => c is TrackBar).ToArray()[0] as TrackBar).Value);
                        CircleProcess(x, y, r, MoistureChange, new object[] { canvas, power });
                    }
                    break;
                case Order.TemperatureTool:
                    {
                        var canvas = Graphics.FromImage(graphicsCache["terrainBitmap"]);
                        int r = (int)(50 * (controls.Where(c => c is TrackBar).ToArray()[0] as TrackBar).Value);
                        CircleProcess(x, y, r, TemperatureChange, new object[] { canvas, power });
                    }
                    break;
                case Order.CivManageTool:
                    {
                        if (fixedX == MousePosition.X && fixedY == MousePosition.Y)
                        {
                            string ownerName = Terrain[x][y].OwnerName;
                            if (power > 0 && ownerName == null && Terrain[x][y].Type == ChunkType.Vegetation)
                            {
                                var random = new Random(x * Height + y);
                                int tmpR = random.Next(0, 255);
                                int tmpG = random.Next(0, 255);
                                int tmpB = random.Next(0, 255);
                                string name = $"Civ on ({x};{y})";
                                for (int i = 0; Mankind.ContainsKey(name); i++)
                                {
                                    name = $"Civ on ({x};{y}) {i}";
                                    tmpR = random.Next(0, 255);
                                    tmpG = random.Next(0, 255);
                                    tmpB = random.Next(0, 255);
                                }
                                Color color = Color.FromArgb(0x66, tmpR, tmpG, tmpB);
                                var civ = new Civilization(name, color);
                                Mankind[civ.Name] = civ;

                                Graphics canvas = Graphics.FromImage(Program.AppForm.graphicsCache["civBitmap"]);
                                canvas.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                                CircleProcess(x, y, 5, civ.TakeChunk, new object[] { canvas });
                            }
                            else if (power < 0 && ownerName != null)
                            {
                                if (currentCivilization?.Name == ownerName)
                                {
                                    currentCivilization = null;
                                    foreach (var control in civControls)
                                    {
                                        control.Active = false;
                                    }
                                }
                                Mankind[ownerName].Remove();
                                Mankind.Remove(ownerName);
                            }
                        }
                    }
                    break;
                case Order.CivSelectTool:
                    {
                        if (fixedX == MousePosition.X && fixedY == MousePosition.Y)
                        {
                            string ownerName = Terrain[x][y].OwnerName;
                            if (power > 0 && ownerName != null)
                            {
                                currentCivilization = Mankind[ownerName];
                                foreach (var control in civControls)
                                {
                                    control.Active = true;
                                }
                            }
                            else if (power < 0)
                            {
                                currentCivilization = null;
                                foreach (var control in civControls)
                                {
                                    control.Active = false;
                                }
                            }
                        }
                    }
                    break;
                case Order.CivAreaTool:
                    {
                        var canvas = Graphics.FromImage(graphicsCache["civBitmap"]);
                        canvas.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                        int r = (int)(50 * (controls.Where(c => c is TrackBar).ToArray()[0] as TrackBar).Value);

                        if (power > 0)
                            CircleProcess(x, y, r, currentCivilization.TakeChunk, new object[] { canvas });
                        else if (power < 0)
                            CircleProcess(x, y, r, currentCivilization.LoseChunk, new object[] { canvas });
                    }
                    break;
            }
        }

        public void CircleProcess(int x, int y, int radius, Action<object[]> process, object[] data)
        {
            for (int i = -radius; i <= radius; i++)
                for (int j = -radius; j <= radius; j++)
                {
                    int cx = x + i;
                    if (cx < 0) cx += Terrain.Length;
                    else if (cx >= Terrain.Length) cx -= Terrain.Length;

                    if (y + j >= 0 && y + j < Terrain[0].Length && (i * i + j * j) <= radius * radius)
                        process(new object[] { data, cx, y + j, i, j, radius });
                }
        }

        private void HeightChange(object[] data)
        {
            var data0 = (object[])data[0];
            var canvas = (Graphics)data0[0];
            var power = (double)data0[1];
            int x = (int)data[1];
            int y = (int)data[2];
            int i = (int)data[3];
            int j = (int)data[4];
            int r = (int)data[5];

            double step = power * (1 - (i * i + j * j) / (r * r + 1.0));
            Terrain[x][y].height = Math.Max(Math.Min(Terrain[x][y].height + step, 1), -1);
            canvas.FillRectangle(new SolidBrush(Terrain[x][y].DrawColor), x, y, 1, 1);
        }

        private void MoistureChange(object[] data)
        {
            var data0 = (object[])data[0];
            var canvas = (Graphics)data0[0];
            var power = (double)data0[1];
            int x = (int)data[1];
            int y = (int)data[2];
            int i = (int)data[3];
            int j = (int)data[4];
            int r = (int)data[5];

            double step = power * (1 - (i * i + j * j) / (r * r + 1.0));
            Terrain[x][y].moisture = Math.Max(Math.Min(Terrain[x][y].moisture + step, 1), -1);
            canvas.FillRectangle(new SolidBrush(Terrain[x][y].DrawColor), x, y, 1, 1);
        }

        private void TemperatureChange(object[] data)
        {
            var data0 = (object[])data[0];
            var canvas = (Graphics)data0[0];
            var power = (double)data0[1];
            int x = (int)data[1];
            int y = (int)data[2];
            int i = (int)data[3];
            int j = (int)data[4];
            int r = (int)data[5];

            double step = power * (1 - (i * i + j * j) / (r * r + 1.0));
            Terrain[x][y].temperature = Math.Max(Math.Min(Terrain[x][y].temperature + step, 1), -1);
            canvas.FillRectangle(new SolidBrush(Terrain[x][y].DrawColor), x, y, 1, 1);
        }

        private void CivRename()
        {
            var civRenameDialog = new CivRenameDialog();
            civRenameDialog.textBox.Text = currentCivilization.Name;

            if (civRenameDialog.ShowDialog() == DialogResult.OK)
            {
                Mankind.Remove(currentCivilization.Name);
                Mankind[civRenameDialog.textBox.Text] = currentCivilization;
                currentCivilization.Rename(civRenameDialog.textBox.Text);
            }
            civRenameDialog.Dispose();
        }

        private void CivRecolor()
        {
            colorDialog.CustomColors = new[] { ColorTranslator.ToOle(currentCivilization.Color) };
            colorDialog.Color = currentCivilization.Color;

            if (colorDialog.ShowDialog() == DialogResult.OK)
                currentCivilization.Recolor(colorDialog.Color);
        }

        private void DrawMap()
        {
            var srcRect0 = new Rectangle(hOffset, vOffset, Terrain.Length - hOffset, Terrain[0].Length - vOffset);
            var srcRect1 = new Rectangle(0, vOffset, hOffset, Terrain[0].Length - vOffset);
            int x0 = (int)(Width * 0.15), x1 = x0 + scaleQ * (Terrain.Length - hOffset), y = 0;
            if (scaleQ * Terrain.Length < Width - (int)(Width * 0.15))
            {
                x0 += (Width - (int)(Width * 0.15) - scaleQ * Terrain.Length) / 2;
                x1 += (Width - (int)(Width * 0.15) - scaleQ * Terrain.Length) / 2;
            }

            if (scaleQ * Terrain[0].Length < Height)
                y += (Height - scaleQ * Terrain[0].Length) / 2;

            var size = new Size(scaleQ * (Terrain.Length - hOffset), scaleQ * (Terrain[0].Length - vOffset));
            var destRect0 = new Rectangle(new Point(x0, y), size);
            size = new Size(scaleQ * hOffset, scaleQ * (Terrain[0].Length - vOffset));
            var destRect1 = new Rectangle(new Point(x1, y), size);

            graphics.Graphics.DrawImage(graphicsCache["terrainBitmap"], destRect0, srcRect0, GraphicsUnit.Pixel);
            graphics.Graphics.DrawImage(graphicsCache["terrainBitmap"], destRect1, srcRect1, GraphicsUnit.Pixel);

            graphics.Graphics.DrawImage(graphicsCache["civBitmap"], destRect0, srcRect0, GraphicsUnit.Pixel);
            graphics.Graphics.DrawImage(graphicsCache["civBitmap"], destRect1, srcRect1, GraphicsUnit.Pixel);
        }

        private void DrawLoading(string text)
        {
            Font font = new Font(FontFamily.GenericSansSerif, 24);

            int w = (int)graphics.Graphics.MeasureString(text, font).Width + 10;
            int h = (int)graphics.Graphics.MeasureString(text, font).Height + 10;
            var rect = new Rectangle((Width - w) / 2, (Height - h) / 2, w, h);

            var brush = new SolidBrush(Constants.BackgroundColor[ControlSituation.Inactive]);
            graphics.Graphics.FillRectangle(brush, rect);

            brush = new SolidBrush(Constants.ForegroundColor[ControlSituation.Inactive]);
            graphics.Graphics.DrawString(text, font, brush, rect.X + 5, rect.Y + 5);

            graphics.Render();
        }

        private void DrawGUI(string label = null)
        {
            if (order > Order.Menu || order == Order.Menu && (Order)cache["orderMenu"] > Order.Menu)
            {
                var brush = new SolidBrush(Constants.BackgroundColor[ControlSituation.Inactive]);
                var rect = new Rectangle((int)(Width * 0.15), 0, (int)(Width * 0.85), Height);
                graphics.Graphics.FillRectangle(brush, rect);
            }

            for (int i = 0; i < controls.Count; i++)
            {
                ControlSituation situation;
                if (controls[i].Active)
                {
                    if (controls[i].IsHovered(MousePosition.X, MousePosition.Y))
                    {
                        if (controls[i] is Button && (controls[i] as Button).Selected)
                            situation = ControlSituation.SelectedHover;
                        else situation = ControlSituation.Hover;
                        if (controls[i] is Button && (controls[i] as Button).Image != null)
                            label = controls[i].Text;
                    }
                    else if (controls[i] is Button && (controls[i] as Button).Selected)
                        situation = ControlSituation.Selected;
                    else situation = ControlSituation.Active;
                }
                else situation = ControlSituation.Inactive;

                graphics.Graphics.DrawImage(controls[i].Draw(situation), controls[i].X, controls[i].Y);
            }

            if (label != null && label != "")
            {
                Font font = new Font(FontFamily.GenericSansSerif, 14);

                var brushB = new SolidBrush(Constants.BackgroundColor[ControlSituation.Inactive]);
                var brushF = new SolidBrush(Constants.ForegroundColor[ControlSituation.Inactive]);
                switch (label[0])
                {
                    case 's':
                        brushB.Color = Constants.BackgroundColor[ControlSituation.Selected];
                        brushF.Color = Constants.ForegroundColor[ControlSituation.Selected];
                        break;
                }
                label = label.Substring(1);

                int w = (int)graphics.Graphics.MeasureString(label, font).Width + 10;
                int h = (int)graphics.Graphics.MeasureString(label, font).Height + 10;
                int x = Math.Min(MousePosition.X, Width - w);
                int y = Math.Min(MousePosition.Y, Height - h);
                var rect = new Rectangle(x, y, w, h);

                graphics.Graphics.FillRectangle(brushB, rect);
                graphics.Graphics.DrawString(label, font, brushF, x + 5, y + 5);

                if (Cursor.Tag == null)
                {
                    Cursor.Tag = true;
                    Cursor.Hide();
                }
            }
            else if (Cursor.Tag != null)
            {
                Cursor.Tag = null;
                Cursor.Show();
            }
        }

        int fixedX = -1;
        int fixedY = -1;
        private void Frame_Tick(object sender, EventArgs e)
        {
            graphics.Graphics.Clear(Color.Black);

            string label = null;
            if (Terrain != null)
            {
                if (order < Order.Menu)
                {
                    int rX = (int)(Width * 0.15), rY = 0;
                    if (scaleQ * Terrain.Length < Width - (int)(Width * 0.15))
                        rX += (Width - (int)(Width * 0.15) - scaleQ * Terrain.Length) / 2;
                    if (scaleQ * Terrain[0].Length < Height)
                        rY += (Height - scaleQ * Terrain[0].Length) / 2;
                    bool inBounds = MousePosition.X >= rX && MousePosition.X < rX + scaleQ * Terrain.Length;
                    inBounds = inBounds && MousePosition.Y >= rY && MousePosition.Y < rY + scaleQ * Terrain[0].Length;
                    if (inBounds)
                    {
                        int sX = ((MousePosition.X - rX) / scaleQ + hOffset) % Terrain.Length;
                        int sY = (MousePosition.Y - rY) / scaleQ + vOffset;
                        if (fixedX > (int)(Width * 0.15))
                        {
                            if (MouseButtons == MouseButtons.Left) ApplyTool(sX, sY, 0.01);
                            else if (MouseButtons == MouseButtons.Right) ApplyTool(sX, sY, -0.01);
                        }
                        if (Terrain[sX][sY].OwnerName != null)
                        {
                            if (currentCivilization?.Name == Terrain[sX][sY].OwnerName)
                                label = "s";
                            else label = " ";
                            var civ = Mankind[Terrain[sX][sY].OwnerName];
                            label += $"{civ.Name}\nПлощадь: {civ.Area:0} км²";
                        }
                    }

                    int sensitivity = Math.Min(Width, Height) / 72;
                    int step = 16 / scaleQ;
                    int x = MousePosition.X;
                    int y = MousePosition.Y;
                    int w = Width;
                    int h = Height;

                    int vLim = (scaleQ * Terrain[0].Length - h) / scaleQ;

                    if (x < sensitivity)
                    {
                        hOffset -= step * (sensitivity - x) / sensitivity;
                        if (hOffset < 0) hOffset += Terrain.Length;
                    }
                    else if (x > w - sensitivity)
                    {
                        hOffset += step * (x + 1 - w + sensitivity) / sensitivity;
                        if (hOffset >= Terrain.Length) hOffset -= Terrain.Length;
                    }

                    if (y < sensitivity && vOffset > 0)
                        vOffset -= Math.Min(step * (sensitivity - y) / sensitivity, vOffset);
                    else if (y > h - sensitivity && vOffset < vLim)
                        vOffset += Math.Min(step * (y + 1 - h + sensitivity) / sensitivity, vLim - vOffset);

                    DrawMap();
                }
                else if (order == Order.Menu) DrawMap();
            }

            if (MouseButtons == MouseButtons.Left)
                foreach (var tb0 in controls.Where(c => c is TrackBar))
                {
                    var tb = (TrackBar)tb0;
                    if (tb.Active && tb.IsHovered(fixedX, fixedY))
                    {
                        int mx = MousePosition.X;
                        tb.Position = Math.Max(Math.Min(mx - (tb.X + 5 + tb.Radius), tb.HLim), 0);
                        fixedX = Math.Max(Math.Min(mx, tb.X + 5 + tb.Radius + tb.HLim), tb.X + 5 + tb.Radius);
                    }
                }

            DrawGUI(label);
            graphics.Render();
        }
    }
}
