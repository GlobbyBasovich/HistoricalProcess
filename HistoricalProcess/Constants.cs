using System.Collections.Generic;
using System.Drawing;

namespace HistoricalProcess
{
    public static class Constants
    {
        //Exact values section

        //In kilometers
        //Should be between mantle and sea level
        public const double DeepestPoint = -11;
        //Should be between sea level and stratosphere
        public const double HighestPoint = 9;

        //In degrees Celsius; should be above absolute zero
        public const double MinTemperature = -60;
        public const double MaxTemperature = 40;

        //As delimeter on (-1; 1) height scale
        public const double SeaLevel = 0.1;
        public const double VegetationLevel = 0.7;

        //As delimeter on (-1; 0) left half moisture scale
        public const double DesertLevel = -0.5;
        //As delimeter on (0; 1) right half moisture scale
        public const double LakeLevel = 0.5;

        //As delimeter on (-1; 0) left half temperature scale
        public const double SnowLevel = -0.6;

        //In degrees Celsius per kilometer above sea level
        public const double HeightCooling = 6.5;

        //In square kilometers
        public const double PolarPixelArea = 0.3551;

        public static Dictionary<ControlSituation, Color> ForegroundColor = new Dictionary<ControlSituation, Color>
        {
            [ControlSituation.Active] = Color.LightBlue,
            [ControlSituation.Inactive] = Color.LightGray,
            [ControlSituation.Hover] = Color.Red,
            [ControlSituation.Selected] = Color.LightGreen,
            [ControlSituation.SelectedHover] = Color.LawnGreen
        };
        public static Dictionary<ControlSituation, Color> BackgroundColor = new Dictionary<ControlSituation, Color>
        {
            [ControlSituation.Active] = Color.Blue,
            [ControlSituation.Inactive] = Color.Gray,
            [ControlSituation.Hover] = Color.DarkRed,
            [ControlSituation.Selected] = Color.DarkGreen,
            [ControlSituation.SelectedHover] = Color.Green
        };


        //Calculated values section

        //HPoint = segment of 0.1 length on [-1; 1] scale of height-related variables
        public const double KmPerHPoint = (HighestPoint - DeepestPoint) / 20;

        //TPoint = segment of 0.1 length on [-1; 1] scale of temperature-related variables
        public const double DegCPerTPoint = (MaxTemperature - MinTemperature) / 20;

        public const double HeightCoolingStep = HeightCooling * KmPerHPoint / DegCPerTPoint;
    }
}