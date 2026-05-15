using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace spacedefence_editor2.Models
{
    public class LevelData
    {
        [JsonPropertyName("Waves")]
        public List<WaveData> Waves { get; set; } = new();

        [JsonPropertyName("Track")]
        public TrackData Track { get; set; } = new();

        [JsonExtensionData]
        public Dictionary<string, object> AdditionalData { get; set; } = new();
    }

    public class WaveData
    {
        [JsonPropertyName("Aliens")]
        public List<AlienData> Aliens { get; set; } = new();

        [JsonExtensionData]
        public Dictionary<string, object> AdditionalData { get; set; } = new();
    }

    public class AlienData
    {
        public string Color { get; set; }
        public float Hitpoints { get; set; }
        public string Type { get; set; }
        public float Armor { get; set; }
        public double Offset { get; set; }
        public float Speed { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> AdditionalData { get; set; } = new();
    }

    public class TrackData
    {
        public string ThemeColor { get; set; }
        public List<List<int>> Path { get; set; } = new();
        public List<TrackBitData> TrackBits { get; set; } = new();
        public int Width { get; set; }
        public int Height { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> AdditionalData { get; set; } = new();
    }

    public class TrackBitData
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string Direction { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> AdditionalData { get; set; } = new();
    }
}
