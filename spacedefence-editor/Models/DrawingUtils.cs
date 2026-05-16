using Avalonia.Media;
using System;

namespace spacedefence_editor2.Models
{
    public static class DrawingUtils
    {
        public static Color GetColorByName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return Color.FromRgb(180, 64, 255); // NeonPurple (Default)

            return name.ToLower() switch
            {
                "neonpink" or "pink" => Color.FromRgb(255, 64, 180),
                "neonpurple" or "purple" => Color.FromRgb(180, 64, 255),
                "neoncyan" or "cyan" => Color.FromRgb(0, 255, 255),
                "neonyellow" or "yellow" => Color.FromRgb(255, 230, 50),
                "neongold" or "gold" => Color.FromRgb(255, 215, 0),
                "neonsilver" or "silver" => Color.FromRgb(200, 200, 220),
                _ => Color.FromRgb(180, 64, 255) // Default to NeonPurple
            };
        }

        public static IBrush GetBrushByName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return new SolidColorBrush(GetColorByName(name));

            return name.ToLower() switch
            {
                "red" => Brushes.Red,
                "green" => Brushes.Green,
                "blue" => Brushes.Blue,
                "white" => Brushes.White,
                "black" => Brushes.Black,
                "orange" => Brushes.Orange,
                _ => new SolidColorBrush(GetColorByName(name))
            };
        }
        
        public static IBrush GetBrushByName(string name, double opacity)
        {
            return new SolidColorBrush(GetColorByName(name), opacity);
        }
    }
}
