using System;
using System.IO;
using System.Text;

/// <summary>
/// TCP 消息分帧：4 字节长度前缀 + UTF8 消息体，
/// 用于解决粘包 / 半包问题。
/// </summary>
public static class TcpFraming
{
    public const int LengthPrefixSize = 4;
    public const int MaxMessageSize = 64 * 1024;

    /// <summary>把字符串编码成一个完整数据帧（长度前缀 + UTF8 内容）。</summary>
    public static byte[] Encode(string text)
    {
        byte[] payload = Encoding.UTF8.GetBytes(text ?? string.Empty);
        if (payload.Length > MaxMessageSize)
        {
            throw new ArgumentException($"消息过大: {payload.Length} 字节，上限 {MaxMessageSize}");
        }

        byte[] frame = new byte[LengthPrefixSize + payload.Length];
        byte[] length = BitConverter.GetBytes(payload.Length);
        Array.Copy(length, 0, frame, 0, LengthPrefixSize);
        Array.Copy(payload, 0, frame, LengthPrefixSize, payload.Length);
        return frame;
    }

    /// <summary>读取一个完整消息帧；返回 null 表示对端已关闭。</summary>
    public static string ReadFrame(Stream stream, byte[] lengthBuffer, byte[] payloadBuffer)
    {
        if (!ReadExactly(stream, lengthBuffer, LengthPrefixSize))
        {
            return null; // 对端关闭
        }

        int length = BitConverter.ToInt32(lengthBuffer, 0);
        if (length < 0 || length > payloadBuffer.Length)
        {
            throw new IOException($"非法消息长度: {length}");
        }

        if (!ReadExactly(stream, payloadBuffer, length)) 
        {
            return null; // 读到一半对端关闭
        }

        return Encoding.UTF8.GetString(payloadBuffer, 0, length);
    }

    private static bool ReadExactly(Stream stream, byte[] buffer, int count)
    {
        int offset = 0;
        while (offset < count)
        {
            int read = stream.Read(buffer, offset, count - offset);
            if (read <= 0)
            {
                return false;
            }
            offset += read;
        }
        return true;
    }
}
