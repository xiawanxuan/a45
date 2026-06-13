using UnityEngine;
using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Text;
using FluidVoxelSandbox.Core;
using FluidVoxelSandbox.Wind;

namespace FluidVoxelSandbox.IO
{
    public class SaveSystem
    {
        private const string FILE_EXTENSION = ".voxsave";
        private const int HEADER_MAGIC = 0x564F5853;
        private const int HEADER_VERSION = 2;

        public string SaveDirectory
        {
            get
            {
                string dir = Path.Combine(Application.persistentDataPath, "Saves");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                return dir;
            }
        }

        [Serializable]
        public class SaveHeader
        {
            public int magic;
            public int version;
            public int mapWidth;
            public int mapHeight;
            public long timestamp;
            public string saveName;
            public float windDirX;
            public float windDirY;
            public float windSpeed;
            public float turbulence;
        }

        public string Save(string saveName, VoxelMap map, WindField windField, WindController windController)
        {
            if (map == null) return null;

            string filePath = GetSaveFilePath(saveName);

            try
            {
                SaveHeader header = new SaveHeader();
                header.magic = HEADER_MAGIC;
                header.version = HEADER_VERSION;
                header.mapWidth = map.Width;
                header.mapHeight = map.Height;
                header.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                header.saveName = saveName;

                if (windField != null)
                {
                    header.windDirX = windField.globalDirection.x;
                    header.windDirY = windField.globalDirection.y;
                    header.windSpeed = windField.globalSpeed;
                    header.turbulence = windField.turbulenceStrength;
                }

                using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                using (GZipStream gzip = new GZipStream(fs, CompressionLevel.Optimal))
                using (BinaryWriter writer = new BinaryWriter(gzip, Encoding.UTF8))
                {
                    WriteHeader(writer, header);
                    byte[] voxelData = map.Serialize();
                    writer.Write(voxelData.Length);
                    writer.Write(voxelData);

                    if (windField != null)
                    {
                        byte[] windData = windField.Serialize();
                        writer.Write(windData.Length);
                        writer.Write(windData);
                    }
                    else
                    {
                        writer.Write(0);
                    }

                    if (windController != null)
                    {
                        byte[] controllerData = windController.Serialize();
                        writer.Write(controllerData.Length);
                        writer.Write(controllerData);
                    }
                    else
                    {
                        writer.Write(0);
                    }
                }

                return filePath;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save: {e.Message}");
                return null;
            }
        }

        public bool Load(string saveName, VoxelMap map, WindField windField, WindController windController)
        {
            if (map == null) return false;

            string filePath = GetSaveFilePath(saveName);
            if (!File.Exists(filePath))
            {
                return false;
            }

            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                using (GZipStream gzip = new GZipStream(fs, CompressionMode.Decompress))
                using (BinaryReader reader = new BinaryReader(gzip, Encoding.UTF8))
                {
                    SaveHeader header = ReadHeader(reader);

                    if (header.magic != HEADER_MAGIC)
                    {
                        Debug.LogError("Invalid save file: bad magic number");
                        return false;
                    }

                    if (header.mapWidth != map.Width || header.mapHeight != map.Height)
                    {
                        Debug.LogWarning($"Map size mismatch: saved {header.mapWidth}x{header.mapHeight} vs current {map.Width}x{map.Height}");
                        map.Resize(header.mapWidth, header.mapHeight);
                    }

                    int voxelDataLen = reader.ReadInt32();
                    byte[] voxelData = reader.ReadBytes(voxelDataLen);
                    map.Deserialize(voxelData);

                    int windDataLen = reader.ReadInt32();
                    if (windDataLen > 0 && windField != null)
                    {
                        byte[] windData = reader.ReadBytes(windDataLen);
                        windField.Deserialize(windData);
                    }
                    else if (windField != null)
                    {
                        windField.globalDirection = new Vector2(header.windDirX, header.windDirY);
                        windField.globalSpeed = header.windSpeed;
                        windField.turbulenceStrength = header.turbulence;
                    }

                    int controllerDataLen = reader.ReadInt32();
                    if (controllerDataLen > 0 && windController != null)
                    {
                        byte[] controllerData = reader.ReadBytes(controllerDataLen);
                        windController.Deserialize(controllerData);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load: {e.Message}");
                return false;
            }
        }

        private void WriteHeader(BinaryWriter writer, SaveHeader header)
        {
            writer.Write(header.magic);
            writer.Write(header.version);
            writer.Write(header.mapWidth);
            writer.Write(header.mapHeight);
            writer.Write(header.timestamp);

            byte[] nameBytes = Encoding.UTF8.GetBytes(header.saveName);
            writer.Write(nameBytes.Length);
            writer.Write(nameBytes);

            writer.Write(header.windDirX);
            writer.Write(header.windDirY);
            writer.Write(header.windSpeed);
            writer.Write(header.turbulence);
        }

        private SaveHeader ReadHeader(BinaryReader reader)
        {
            SaveHeader header = new SaveHeader();
            header.magic = reader.ReadInt32();
            header.version = reader.ReadInt32();
            header.mapWidth = reader.ReadInt32();
            header.mapHeight = reader.ReadInt32();
            header.timestamp = reader.ReadInt64();

            int nameLen = reader.ReadInt32();
            byte[] nameBytes = reader.ReadBytes(nameLen);
            header.saveName = Encoding.UTF8.GetString(nameBytes);

            header.windDirX = reader.ReadSingle();
            header.windDirY = reader.ReadSingle();
            header.windSpeed = reader.ReadSingle();
            header.turbulence = reader.ReadSingle();

            return header;
        }

        public string[] GetSaveList()
        {
            List<string> saves = new List<string>();

            try
            {
                if (!Directory.Exists(SaveDirectory)) return saves.ToArray();

                string[] files = Directory.GetFiles(SaveDirectory, "*" + FILE_EXTENSION);
                foreach (string file in files)
                {
                    saves.Add(Path.GetFileNameWithoutExtension(file));
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to list saves: {e.Message}");
            }

            return saves.ToArray();
        }

        public SaveHeader GetSaveInfo(string saveName)
        {
            string filePath = GetSaveFilePath(saveName);
            if (!File.Exists(filePath))
            {
                return null;
            }

            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                using (GZipStream gzip = new GZipStream(fs, CompressionMode.Decompress))
                using (BinaryReader reader = new BinaryReader(gzip, Encoding.UTF8))
                {
                    return ReadHeader(reader);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to read save info: {e.Message}");
                return null;
            }
        }

        public bool DeleteSave(string saveName)
        {
            try
            {
                string filePath = GetSaveFilePath(saveName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete save: {e.Message}");
                return false;
            }
        }

        public bool SaveExists(string saveName)
        {
            return File.Exists(GetSaveFilePath(saveName));
        }

        public string GetSaveFilePath(string saveName)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string safeName = string.Join("_", saveName.Split(invalidChars));
            return Path.Combine(SaveDirectory, safeName + FILE_EXTENSION);
        }

        public string ExportVoxelsToText(string saveName, VoxelMap map)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"# Voxel Export: {saveName}");
            sb.AppendLine($"# Size: {map.Width}x{map.Height}");
            sb.AppendLine($"# Date: {DateTime.Now}");
            sb.AppendLine("X,Y,Type,Category");

            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    Voxel v = map.GetVoxel(x, y);
                    if (!v.IsEmpty)
                    {
                        sb.AppendLine($"{x},{y},{v.Type},{v.Category}");
                    }
                }
            }

            string exportPath = Path.Combine(SaveDirectory, saveName + "_export.csv");
            File.WriteAllText(exportPath, sb.ToString());
            return exportPath;
        }

        public string ExportStatistics(VoxelMap map)
        {
            Dictionary<VoxelType, int> typeCounts = new Dictionary<VoxelType, int>();

            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    Voxel v = map.GetVoxel(x, y);
                    if (!v.IsEmpty)
                    {
                        if (!typeCounts.ContainsKey(v.Type))
                        {
                            typeCounts[v.Type] = 0;
                        }
                        typeCounts[v.Type]++;
                    }
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== Voxel Statistics ===");
            sb.AppendLine($"Total size: {map.Width} x {map.Height}");
            sb.AppendLine($"Total cells: {map.Width * map.Height}");
            sb.AppendLine("");

            foreach (var kvp in typeCounts)
            {
                sb.AppendLine($"{kvp.Key}: {kvp.Value} ({(float)kvp.Value / (map.Width * map.Height) * 100:F1}%)");
            }

            return sb.ToString();
        }
    }
}
