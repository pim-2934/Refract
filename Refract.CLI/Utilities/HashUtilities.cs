namespace Refract.CLI.Utilities;

public static class HashUtilities
{
    private const uint Polynomial = 0xEDB88320;

    public static uint CalculateCrc32(byte[] data)
    {
        var crc = 0xFFFFFFFF;

        foreach (var b in data)
        {
            crc ^= b;
            for (var i = 0; i < 8; i++)
            {
                crc = (crc & 1) == 1 ? (crc >> 1) ^ Polynomial : crc >> 1;
            }
        }

        return ~crc;
    }
}