using System.Drawing;

namespace HistoricalProcess
{
    public enum ControlSituation
    {
        Active,
        Inactive,
        Hover,
        Selected,
        SelectedHover
    }

    interface ICustomControl
    {
        string Text { get; set; }
        int X { get; set; }
        int Y { get; set; }
        int Width { get; set; }
        int Height { get; set; }
        bool Active { get; set; }

        bool IsHovered(int x, int y);

        Bitmap Draw(ControlSituation situation);
    }
}
