namespace Core.Components;

using System;
using System.IO;
using System.Security.Cryptography;

public partial class CryptoUtils
{
    public static string? GetFileSHA256(string filePath)
    {
        return GetFileSHA256(SHA256.Create(), filePath);
    }

    public static string? GetFileSHA256(SHA256 sha256, string filePath)
    {
        using FileStream? stream = File.OpenRead(filePath);
        if (stream == null) return null;

        byte[] hash = sha256.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    public static string? GetByteArraySHA256(byte[] data)
    {
        return GetByteArraySHA256(SHA256.Create(), data);
    }

    public static string? GetByteArraySHA256(SHA256 sha256, byte[] data)
    {
        byte[] hash = sha256.ComputeHash(data);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}