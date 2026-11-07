using System;
using System.IO;

public static class AudioDurationReader
{
    public static float GetDuration(string path)
    {
        if (!File.Exists(path)) return 0f;
        string ext = Path.GetExtension(path).ToLowerInvariant();

        try
        {
            return ext switch
            {
                ".wav" => GetWavDuration(path),
                ".mp3" => GetMp3Duration(path),
                _ => 0f
            };
        }
        catch
        {
            return 0f;
        }
    }

    private static float GetWavDuration(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        using var br = new BinaryReader(fs);

        if (new string(br.ReadChars(4)) != "RIFF") return 0f;
        br.ReadInt32();
        if (new string(br.ReadChars(4)) != "WAVE") return 0f;

        short channels = 0, bits = 0;
        int sampleRate = 0, dataSize = 0;

        while (fs.Position < fs.Length - 8)
        {
            string id = new string(br.ReadChars(4));
            int size = br.ReadInt32();

            if (id == "fmt ")
            {
                br.ReadInt16();
                channels = br.ReadInt16();
                sampleRate = br.ReadInt32();
                br.ReadInt32();
                br.ReadInt16();
                bits = br.ReadInt16();
                if (size > 16) br.ReadBytes(size - 16);
            }
            else if (id == "data")
            {
                dataSize = size;
                break;
            }
            else br.ReadBytes(size);
        }

        if (sampleRate == 0 || channels == 0 || bits == 0) return 0f;
        return (float)dataSize / (sampleRate * channels * (bits / 8));
    }

    private static float GetMp3Duration(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        using var br = new BinaryReader(fs);

        // ID3 überspringen
        if (fs.Length > 10 && new string(br.ReadChars(3)) == "ID3")
        {
            br.ReadBytes(3);
            int size = (br.ReadByte() & 0x7F) << 21 |
                       (br.ReadByte() & 0x7F) << 14 |
                       (br.ReadByte() & 0x7F) << 7 |
                       (br.ReadByte() & 0x7F);
            fs.Position = 10 + size;
        }
        else fs.Position = 0;

        while (fs.Position < fs.Length - 4)
        {
            if (br.ReadByte() != 0xFF) continue;
            byte b = br.ReadByte();
            if ((b & 0xE0) != 0xE0) continue;

            fs.Position -= 2;
            byte[] header = br.ReadBytes(4);
            int bitrate = GetBitrate(header);
            if (bitrate > 0)
            {
                long audioBytes = fs.Length - fs.Position + 4;
                return (float)(audioBytes * 8) / (bitrate * 1000);
            }
        }
        return 0f;
    }

    private static int GetBitrate(byte[] h)
    {
        int index = (h[2] >> 4) & 0xF;
        int[] rates = { 0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 0 };
        return index < rates.Length ? rates[index] : 0;
    }
}