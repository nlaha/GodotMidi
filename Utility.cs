using Godot;
using System;

public class GodotMidiUtils
{
    /// <summary>
    /// Converts the big endian byte representation of
    /// a 32-bit signed integer to an integer.
    /// </summary>
    /// <param name="buf"></param>
    /// <param name="i"></param>
    /// <returns></returns>
    public static int ToInt32BigEndian(byte[] buf, int i)
    {
        return (buf[i]<<24) | (buf[i+1]<<16) | (buf[i+2]<<8) | buf[i+3];
    }
    
    /// <summary>
    /// Converts the big endian byte representation of
    /// a 16-bit signed integer to an integer.
    /// </summary>
    /// <param name="buf"></param>
    /// <param name="i"></param>
    /// <returns></returns>
    public static int ToInt16BigEndian(byte[] buf, int i)
    {
        return (buf[i]<<8) | buf[i+1];
    }
    
    /// <summary>
    /// Converts the big endian byte representation
    /// of a VarInt (MIDI specification) to an integer.
    /// </summary>
    /// <param name="buf"></param>
    /// <param name="i"></param>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static int ToVarIntBigEndian(byte[] buf, int i, out int bytes)
    {
        int value = 0;
        bytes = 0;
        while (true)
        {
            value = (value << 7) | (buf[i] & 0x7F);
            bytes++;
            if ((buf[i] & 0x80) == 0)
                break;
            i++;
        }
        return value;
    }
    
    /// <summary>
    /// Converts the big endian byte representation of
    /// a 24-bit signed integer to an integer.
    /// </summary>
    /// <param name="buf"></param>
    /// <param name="i"></param>
    /// <returns></returns>
    public static int ToInt24BigEndian(byte[] buf, int i)
    {
        return (buf[i]<<16) | (buf[i+1]<<8) | buf[i+2];
    }
}