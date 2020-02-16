using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace HistoricalProcess
{
    public enum ChunkType
    {
        Sea,
        Vegetation,
        Rock,
        Desert,
        Lake,
        Snow,
        SnowCappedPeak
    }

    [Serializable]
    public class Chunk
    {
        public ChunkType Type {
            get
            {
                if (height > Constants.VegetationLevel)
                {
                    if (temperature < Constants.SnowLevel) return ChunkType.SnowCappedPeak;
                    return ChunkType.Rock;
                }
                else if (height > Constants.SeaLevel)
                {
                    if (temperature < Constants.SnowLevel) return ChunkType.Snow;
                    if (moisture > Constants.LakeLevel && height > Constants.SeaLevel + 1 / Constants.KmPerHPoint / 20)
                        return ChunkType.Lake;
                    if (moisture < Constants.DesertLevel)
                        return ChunkType.Desert;
                    return ChunkType.Vegetation;
                }
                else return ChunkType.Sea;
            }
        }

        public Color DrawColor
        {
            get
            {
                byte r = 0, g = 0, b = 0;

                if (height > Constants.VegetationLevel)
                {
                    r += (byte)(42 + (height - Constants.VegetationLevel) / (1 - Constants.VegetationLevel) * 85);
                    g += (byte)(42 + (height - Constants.VegetationLevel) / (1 - Constants.VegetationLevel) * 85);
                    b += (byte)(42 + (height - Constants.VegetationLevel) / (1 - Constants.VegetationLevel) * 85);

                    if (temperature < Constants.SnowLevel)
                    {
                        r = (byte)(170 + (height - Constants.SeaLevel) / (1 - Constants.SeaLevel) * 85);
                        g = (byte)(170 + (height - Constants.SeaLevel) / (1 - Constants.SeaLevel) * 85);
                        b = (byte)(170 + (height - Constants.SeaLevel) / (1 - Constants.SeaLevel) * 85);
                    }
                }
                else if (height > Constants.SeaLevel)
                {
                    r += (byte)((1 - (height - Constants.SeaLevel) / (1 - Constants.SeaLevel)) * 42);
                    g += (byte)(42 + (1 - (height - Constants.SeaLevel) / (1 - Constants.SeaLevel)) * 42);
                    b += (byte)((1 - (height - Constants.SeaLevel) / (1 - Constants.SeaLevel)) * 0);

                    if (moisture > Constants.LakeLevel && height > Constants.SeaLevel + 1 / Constants.KmPerHPoint / 20)
                    {
                        r = 42;
                        g = (byte)(42 + (1 + (height - Constants.SeaLevel) / (1 + Constants.SeaLevel)) * 85);
                        b = (byte)(85 + (1 + (height - Constants.SeaLevel) / (1 + Constants.SeaLevel)) * 85);
                    }
                    else if (moisture > 0)
                    {
                        b += (byte)(moisture * 85);
                    }
                    else if (moisture > Constants.DesertLevel)
                    {
                        r += (byte)(-moisture * 85);
                        g += (byte)(-moisture * 42);
                    }
                    else
                    {
                        r = (byte)(170 + (height - Constants.SeaLevel) / (1 - Constants.SeaLevel) * 106);
                        g = (byte)(170 + (height - Constants.SeaLevel) / (1 - Constants.SeaLevel) * 42);
                        b = 85;
                    }

                    if (temperature > 0)
                    {
                        if ((moisture <= Constants.LakeLevel || height <= Constants.SeaLevel + 1 / Constants.KmPerHPoint / 20)
                            && moisture > Constants.DesertLevel)
                        {
                            r += (byte)(temperature * 85);
                            g += (byte)(temperature * 42);
                        }
                    }
                    else if (temperature < Constants.SnowLevel)
                    {
                        r = (byte)(170 + (height - Constants.SeaLevel) / (1 - Constants.SeaLevel) * 85);
                        g = (byte)(170 + (height - Constants.SeaLevel) / (1 - Constants.SeaLevel) * 85);
                        b = (byte)(170 + (height - Constants.SeaLevel) / (1 - Constants.SeaLevel) * 85);
                    }
                }
                else
                {
                    r = (byte)((1 + (height - Constants.SeaLevel) / (1 + Constants.SeaLevel)) * 42);
                    g = (byte)((1 + (height - Constants.SeaLevel) / (1 + Constants.SeaLevel)) * 42);
                    b = (byte)(85 + (1 + (height - Constants.SeaLevel) / (1 + Constants.SeaLevel)) * 85);
                }

                return Color.FromArgb(r, g, b);
            }
        }

        public double Area
        {
            get
            {
                var map = Program.AppForm.Terrain;
                double w = map.Length, h = map[0].Length;
                return Constants.PolarPixelArea * (w - Math.Abs(h / 2 - Y) / (h / 2 - 1) * (w - 1));
            }
        }

        public string OwnerName;

        public int X, Y;
        public double height, temperature, moisture;

        public Chunk(int x, int y, double height, double temperature, double moisture)
        {
            X = x;
            Y = y;
            this.height = Math.Max(Math.Min(height, 1), -1);
            this.temperature = Math.Max(Math.Min(temperature, 1), -1);
            this.moisture = Math.Max(Math.Min(moisture, 1), -1);
        }

        public Chunk() { } //For BoisSerializer
    }
}
