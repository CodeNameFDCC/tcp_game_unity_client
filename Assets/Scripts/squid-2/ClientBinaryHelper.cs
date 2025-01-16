// ClientBinaryHelper.cs
using UnityEngine;
using System;
using System.Text;

public class BinaryHelper
{
    private byte[] buffer;
    private int offset;
    private const int INITIAL_SIZE = 1024;
    private const int INT32_SIZE = 4;
    private const int INT16_SIZE = 2;
    private const int INT8_SIZE = 1;
    private const int FLOAT_SIZE = 4;
    private const int BOOL_SIZE = 1;
    public BinaryHelper()
    {
        buffer = new byte[INITIAL_SIZE];
        offset = 0;
    }
    private void EnsureCapacity(int required)
    {
        if (offset + required > buffer.Length)
        {
            int newSize = Math.Max(buffer.Length * 2, offset + required);
            byte[] newBuffer = new byte[newSize];
            Array.Copy(buffer, newBuffer, offset);
            buffer = newBuffer;
        }
    }
    private void ValidateBufferSize(int requiredSize)
    {
        if (offset + requiredSize > buffer.Length)
        {
            throw new InvalidOperationException(
                $"Buffer overflow. Current offset: {offset}, Required size: {requiredSize}, Buffer length: {buffer.Length}");
        }
    }

    // Writing methods
    public void WriteString(string value)
    {
        byte[] stringBytes = Encoding.UTF8.GetBytes(value);
        EnsureCapacity(4 + stringBytes.Length); // 길이(4바이트) + 문자열 바이트
        WriteInt32(stringBytes.Length);
        Array.Copy(stringBytes, 0, buffer, offset, stringBytes.Length);
        offset += stringBytes.Length;
    }

    public void WriteFloat(float value)
    {
        EnsureCapacity(4);
        byte[] bytes = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        Array.Copy(bytes, 0, buffer, offset, 4);
        offset += 4;
    }

    public void WriteBool(bool value)
    {
        EnsureCapacity(1);
        buffer[offset] = (byte)(value ? 1 : 0);
        offset += 1;
    }

    public void WriteInt8(int value)
    {
        if (value < sbyte.MinValue || value > sbyte.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value),
                $"Value must be between {sbyte.MinValue} and {sbyte.MaxValue}");
        }

        EnsureCapacity(1);
        buffer[offset] = (byte)value;
        offset += 1;
    }
    public void WriteInt16(int value)
    {
        EnsureCapacity(2);
        if (value > short.MaxValue || value < short.MinValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Value must be between -32768 and 32767");
        }
        byte[] bytes = BitConverter.GetBytes((short)value);
        if (!BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        Array.Copy(bytes, 0, buffer, offset, 2);
        offset += 2;
    }
    public void WriteInt32(int value)
    {
        EnsureCapacity(sizeof(int));
        // 값을 로그로 출력
        Debug.Log($"Writing Int32 value: {value} (0x{value:X8})");

        byte[] bytes = BitConverter.GetBytes(value);
        // 엔디안 확인
        if (!BitConverter.IsLittleEndian)
            Array.Reverse(bytes);

        Array.Copy(bytes, 0, buffer, offset, sizeof(int));
        offset += sizeof(int);

        // 버퍼 상태 로그
        Debug.Log($"Buffer after write: {BitConverter.ToString(buffer, 0, offset)}");
    }

    public void WritePosition(Vector3 position)
    {
        WriteFloat(position.x);
        WriteFloat(position.y);
        WriteFloat(position.z);
    }

    public void WriteQuaternion(Quaternion quaternion)
    {
        WriteFloat(quaternion.x);
        WriteFloat(quaternion.y);
        WriteFloat(quaternion.z);
        WriteFloat(quaternion.w);
    }

    // Reading methods
    public sbyte ReadInt8()
    {
        ValidateBufferSize(INT8_SIZE);
        sbyte value = (sbyte)buffer[offset];
        offset += 1;
        return value;
    }
    public short ReadInt16()
    {
        byte[] bytes = new byte[2]; // 2바이트 크기의 배열 생성
        Array.Copy(buffer, offset, bytes, 0, 2); // 현재 오프셋에서 2바이트 복사
        if (BitConverter.IsLittleEndian == false)
            Array.Reverse(bytes); // 리틀 엔디안이 아닌 경우 바이트 순서 반전
        offset += 2; // 오프셋을 2 증가
        return BitConverter.ToInt16(bytes, 0); // 2바이트를 Int16으로 변환하여 반환
    }
    public int ReadInt32()
    {
        ValidateBufferSize(INT32_SIZE);
        byte[] bytes = new byte[INT32_SIZE];
        Array.Copy(buffer, offset, bytes, 0, INT32_SIZE);
        if (!BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        offset += INT32_SIZE;
        return BitConverter.ToInt32(bytes, 0);
    }

    public float ReadFloat()
    {
        byte[] bytes = new byte[4];
        Array.Copy(buffer, offset, bytes, 0, 4);
        if (BitConverter.IsLittleEndian == false)
            Array.Reverse(bytes);
        offset += 4;
        return BitConverter.ToSingle(bytes, 0);
    }

    public bool ReadBool()
    {
        bool value = buffer[offset] == 1;
        offset += 1;
        return value;
    }

    public string ReadString()
    {
        int length = ReadInt32();
        string value = Encoding.UTF8.GetString(buffer, offset, length);
        offset += length;
        return value;
    }

    public Vector3 ReadPosition()
    {
        return new Vector3(
            ReadFloat(),
            ReadFloat(),
            ReadFloat()
        );
    }

    public Quaternion ReadQuaternion()
    {
        return new Quaternion(
            ReadFloat(),
            ReadFloat(),
            ReadFloat(),
            ReadFloat()
        );
    }

    // Buffer management
    public byte[] GetBuffer()
    {
        byte[] result = new byte[offset];
        Array.Copy(buffer, result, offset);
        return result;
    }

    public void SetBuffer(byte[] newBuffer)
    {
        buffer = newBuffer;
        offset = 0;
    }

    public void Reset()
    {
        offset = 0;
        Array.Clear(buffer, 0, buffer.Length);
    }
}