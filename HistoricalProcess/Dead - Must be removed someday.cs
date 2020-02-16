using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HistoricalProcess;

namespace Graveyard
{
    class Dead
    {
        Chunk[,] terrain;

        private Bitmap FormMain_TerrainGen(int width, int height)
        {
            terrain = new Chunk[width, height];

            Bitmap bmp = new Bitmap(width, height);
            Graphics canvas = Graphics.FromImage(bmp);

            Dictionary<ChunkType, Color> colors = new Dictionary<ChunkType, Color>();
            colors[ChunkType.Sea] = Color.Navy;
            colors[ChunkType.Vegetation] = Color.LawnGreen;
            colors[ChunkType.Desert] = Color.Beige;
            colors[ChunkType.Rock] = Color.Gray;
            colors[ChunkType.Snow] = Color.White;

            Dictionary<ChunkType, Pen> pens = new Dictionary<ChunkType, Pen>();
            for (ChunkType i = ChunkType.Sea; i <= ChunkType.Rock; i++)
            {
                pens[i] = new Pen(colors[i]);
            }

            Perlin2D perlin = new Perlin2D(DateTime.Now.GetHashCode());

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    float xDiff = Math.Abs(i - width / 2.0f);
                    float yDiff = Math.Abs(j - height / 2.0f);
                    float sideAspect = 0;
                    if (xDiff > 0.4f * width)
                        sideAspect = (xDiff - 0.4f * width) / (0.2f * width);
                    if (yDiff > 0.4f * height)
                        sideAspect = Math.Max(sideAspect, (yDiff - 0.4f * height) / (0.2f * height));
                    float val = perlin.Noise((float)i * 5 / width, (float)j * 5 / height, 8);
                    terrain[i, j] = new Chunk(i, j, Math.Max(val - sideAspect, -0.9f), 0, 0);
                }
            }

            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                {
                    Color color = colors[terrain[i, j].Type];
                    float adjust = (float)Math.Sqrt(Math.Abs(terrain[i, j].height));
                    if (terrain[i, j].Type != ChunkType.Snow)
                        color = ControlPaint.Dark(color, adjust);
                    canvas.DrawRectangle(new Pen(color), i, j, 1, 1);
                }

            return bmp;
        }

        private Bitmap FormMain_TerrainGen2(int width, int height)
        {
            terrain = new Chunk[width, height];

            Bitmap bmp = new Bitmap(width, height);
            Graphics canvas = Graphics.FromImage(bmp);

            Dictionary<ChunkType, Color> colors = new Dictionary<ChunkType, Color>();
            colors[ChunkType.Sea] = Color.Navy;
            //colors[ChunkType.Beach] = Color.SandyBrown;
            colors[ChunkType.Vegetation] = Color.LawnGreen;
            colors[ChunkType.Rock] = Color.Gray;
            colors[ChunkType.Desert] = Color.Beige;
            colors[ChunkType.Lake] = Color.MediumAquamarine;
            colors[ChunkType.Snow] = Color.White;
            colors[ChunkType.SnowCappedPeak] = Color.WhiteSmoke;

            var brushes = new Dictionary<ChunkType, SolidBrush>();
            for (ChunkType i = ChunkType.Sea; i <= ChunkType.SnowCappedPeak; i++)
            {
                brushes[i] = new SolidBrush(colors[i]);
            }

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

                    //Генерация по карте высот
                    var earthBitmap = new Bitmap(1, 1);
                    var earthBitmap2 = new Bitmap(1, 1);
                    {
                        var c = earthBitmap.GetPixel(i, j);
                        var c2 = earthBitmap2.GetPixel(i, j);

                        if (c2.R < 19)
                        {
                            if (c.B > 100 || c.G < 64 && c.R < 64)
                                chunkHeight = (c.G / 255.0 + c.B / 255.0) / 2 * (1 + Constants.SeaLevel) - 1;
                            else
                            {
                                double k = Constants.VegetationLevel - Constants.SeaLevel;
                                chunkHeight = Constants.SeaLevel + (c.R / 255.0 * (k + 0.2) / 2 + (255 - c.G) / 255.0) / 2 * k;
                            }
                        }
                        else chunkHeight = Constants.SeaLevel + (c2.R - 18 + chunkHeight) / 223.0 * (1 - Constants.SeaLevel);
                    }

                    terrain[i, j] = new Chunk(i, j, chunkHeight, temperature, moisture);

                    canvas.FillRectangle(brushes[terrain[i, j].Type], i, j, 1, 1);
                }

            return bmp;
        }

        class Perlin2D
        {
            byte[] permutationTable;

            public Perlin2D(int seed = 0)
            {
                var rand = new Random(seed);
                permutationTable = new byte[1024];
                rand.NextBytes(permutationTable);
            }

            private float[] GetPseudoRandomGradientVector(int x, int y)
            {
                int v = (int)(((x * 1836311903) ^ (y * 2971215073) + 4807526976) & 1023);
                v = permutationTable[v] & 3;

                switch (v)
                {
                    case 0: return new float[] { 1, 0 };
                    case 1: return new float[] { -1, 0 };
                    case 2: return new float[] { 0, 1 };
                    default: return new float[] { 0, -1 };
                }
            }

            static float QunticCurve(float t)
            {
                return t * t * t * (t * (t * 6 - 15) + 10);
            }

            static float Lerp(float a, float b, float t)
            {
                return a + (b - a) * t;
            }

            static float Dot(float[] a, float[] b)
            {
                return a[0] * b[0] + a[1] * b[1];
            }

            public float Noise(float fx, float fy)
            {
                int left = (int)Math.Floor(fx);
                int top = (int)Math.Floor(fy);
                float pointInQuadX = fx - left;
                float pointInQuadY = fy - top;

                float[] topLeftGradient = GetPseudoRandomGradientVector(left, top);
                float[] topRightGradient = GetPseudoRandomGradientVector(left + 1, top);
                float[] bottomLeftGradient = GetPseudoRandomGradientVector(left, top + 1);
                float[] bottomRightGradient = GetPseudoRandomGradientVector(left + 1, top + 1);

                float[] distanceToTopLeft = new float[] { pointInQuadX, pointInQuadY };
                float[] distanceToTopRight = new float[] { pointInQuadX - 1, pointInQuadY };
                float[] distanceToBottomLeft = new float[] { pointInQuadX, pointInQuadY - 1 };
                float[] distanceToBottomRight = new float[] { pointInQuadX - 1, pointInQuadY - 1 };

                float tx1 = Dot(distanceToTopLeft, topLeftGradient);
                float tx2 = Dot(distanceToTopRight, topRightGradient);
                float bx1 = Dot(distanceToBottomLeft, bottomLeftGradient);
                float bx2 = Dot(distanceToBottomRight, bottomRightGradient);

                pointInQuadX = QunticCurve(pointInQuadX);
                pointInQuadY = QunticCurve(pointInQuadY);

                float tx = Lerp(tx1, tx2, pointInQuadX);
                float bx = Lerp(bx1, bx2, pointInQuadX);
                float tb = Lerp(tx, bx, pointInQuadY);

                return tb;
            }

            public float Noise(float fx, float fy, int octaves, float persistence = 0.5f)
            {
                float amplitude = 1;
                float max = 0;
                float result = 0;

                while (octaves-- > 0)
                {
                    max += amplitude;
                    result += Noise(fx, fy) * amplitude;
                    amplitude *= persistence;
                    fx *= 2;
                    fy *= 2;
                }

                return result / max;
            }
        }
    }
}
