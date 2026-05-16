using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace spacedefence_editor2.Models
{
    public static class LevelService
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        public static LevelData LoadLevel(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Level file not found", filePath);

            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<LevelData>(json, _options) ?? new LevelData();
        }

        public static void SaveLevel(string filePath, LevelData levelData)
        {
            // Create backup if file exists
            if (File.Exists(filePath))
            {
                string backupPath = filePath + ".bak";
                File.Copy(filePath, backupPath, true);
            }

            string json = JsonSerializer.Serialize(levelData, _options);
            File.WriteAllText(filePath, json);
        }

        public static LevelData CreateNewLevel()
        {
            var level = new LevelData
            {
                Track = new TrackData
                {
                    ThemeColor = "NeonPurple",
                    Width = 15,
                    Height = 14
                }
            };
            return level;
        }

    }
}
