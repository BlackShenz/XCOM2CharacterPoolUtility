using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CPoolUtil.Core
{
    public sealed class Parser
    {
        private IOutputter _outputter;
        private byte[] _rawBytes;
        private int curFilePosition = 0; // Keep track of our own progress each time we read bytes

        public IReadOnlyList<byte> RawBytes => _rawBytes;

        public Parser(IOutputter outputter)
        {
            _outputter = outputter;
        }

        public Parser(string fileLocation, IOutputter outputter) : this(outputter)
        {
            _outputter.Write($"Loading character pool from {fileLocation}... ");
            _rawBytes = File.ReadAllBytes(fileLocation);
            _outputter.WriteLine("Success!");
        }

        #region Read Methods

        public byte[] GetBytes(int length)
        {
            return ReadBytes(length);
        }

        public int GetInt()
        {
            var subArray = ReadBytes(4);

            // If our current environment is not Little Endian, reverse the array before converting
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(subArray);

            return BitConverter.ToInt32(subArray);
        }

        public string GetString(int length)
        {
            Console.WriteLine($"GetString 开始 - 请求长度: {length}, 当前位置: {curFilePosition:X4}, 文件总长度: {_rawBytes.Length:X4}");

            if (length == 0) return "";
            
            if (curFilePosition + length > _rawBytes.Length)
            {
                Console.WriteLine($"GetString 错误 - 请求 {length} 字节但剩余只有 {_rawBytes.Length - curFilePosition} 字节");
                throw new Exception($"GetString: 请求 {length} 字节但剩余只有 {_rawBytes.Length - curFilePosition} 字节");
            }
            
            var subArray = ReadBytes(length);
            var encoding = Encoding.UTF8;
            
            Console.WriteLine($"GetString 读取的原始字节: {BitConverter.ToString(subArray)}");

            // 移除尾部的\0，但保留字符串内容
            int actualLength = length - 1;

            byte[] trimmedArray = new byte[actualLength];
            Array.Copy(subArray, trimmedArray, actualLength);

            string result = encoding.GetString(trimmedArray).Replace("\r", Environment.NewLine);
            Console.WriteLine($"GetString 结果: '{result}' (长度: {result.Length})");
            return result;
        }

        /// <summary>
        /// 获取带有特殊标识头的中文字符串（如 feffffff 或 fdffffff 前缀）
        /// </summary>
        public string GetSpecialUnicodeString(int length)
        {
            Console.WriteLine($"GetSpecialUnicodeString 开始 - 当前位置: {curFilePosition:X4}");

            if (length == 0) return "";

            int expectedCharLength = Math.Abs(length) * 2; // 字符长度 = abs(size) * 2
            Console.WriteLine($"预期字符长度: {expectedCharLength}");
            
            var subArray = ReadBytes(expectedCharLength);
            var encoding = Encoding.Unicode; // 使用UTF-16LE编码用于中文字符
            
            Console.WriteLine($"GetString 读取的原始字节: {BitConverter.ToString(subArray)}");

            // 移除尾部的\0，但保留字符串内容
            int actualLength = expectedCharLength - 2;

            byte[] trimmedArray = new byte[actualLength];
            Array.Copy(subArray, trimmedArray, actualLength);

            string result = encoding.GetString(trimmedArray).Replace("\r", Environment.NewLine);
            Console.WriteLine($"GetSpecialUnicodeString 结果: '{result}' (长度: {result.Length}, 预期: {expectedCharLength})");
            
            return result;
        }

        /// <summary>
        /// 解析StrProperty，支持特殊Unicode和UTF-8两种格式
        /// </summary>
        /// <returns>解析后的字符串</returns>
        public string ParseStrProperty()
        {
            Console.WriteLine($"ParseStrProperty 开始 - 当前位置: {curFilePosition:X4}");
            
            // 检查接下来的4字节来确定格式类型
            if (curFilePosition + 4 <= _rawBytes.Length)
            {
                int sizeField = GetInt();
                bool isSpecialHeader = sizeField < 0;
                
                Console.WriteLine($"标识头数值: 0x{sizeField:X8} (十进制: {sizeField}), 是否小于0: {isSpecialHeader}");
                
                if (isSpecialHeader)
                {
                    Console.WriteLine("检测到特殊Unicode标识头，使用特殊Unicode解析...");
                    try
                    {
                        string result = GetSpecialUnicodeString(sizeField);
                        Console.WriteLine($"Parser.StrProperty 特殊Unicode解析成功: '{result}'");
                        return result;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"特殊Unicode解析失败: {ex.Message}");
                        return "";
                    }
                }
                else
                {
                    Console.WriteLine("检测到普通UTF-8格式，使用UTF-8解析...");
                    Console.WriteLine($"StrProperty 大小字段: {sizeField}");
                    string result = GetString(sizeField);
                    Console.WriteLine($"Parser.StrProperty UTF-8解析结果: '{result}'");
                    return result;
                }
            }
            else
            {
                Console.WriteLine("文件剩余字节不足4字节，无法检查标识头");
                return "";
            }
        }

        public CProperty GetProperty()
        {
            // Check if we have enough bytes remaining
            if (curFilePosition >= _rawBytes.Length) return null;
            // First, read the length and value of the following property name string
            int propNameLength = GetInt();
            string propName = GetString(propNameLength);
            SkipPadding();

            // If this is a "None" ending type property, just return - nothing else will follow this
            if (propName == "None") return null;

            // Get the property type string
            int typeNameLength = GetInt();
            string typeName = GetString(typeNameLength);
            SkipPadding();

            if (typeName == "None") return new NoneProperty(propName);

            // The rest of the structure SHOULD be as follows: [int (length of final pieces)]-[padding]-[int (length of data)]-[data]
            int dataLength = GetInt();
            SkipPadding();

            // Finally, read the data based on the property type
            CProperty returnProperty;
            switch (typeName)
            {
                case "ByteProperty":
                    returnProperty = new ByteProperty(propName);
                    break;
                case "IntProperty":
                    returnProperty = new IntProperty(propName);
                    break;

                case "StrProperty":
                    returnProperty = new StrProperty(propName);
                    break;

                case "NameProperty":
                    returnProperty = new NameProperty(propName);
                    break;

                case "ArrayProperty":
                    if (propName == "CharacterPool")
                        returnProperty = new CharacterArray(propName);
                    else
                        returnProperty = new ArrayProperty(propName);
                    break;

                case "StructProperty":
                    if (propName == "kAppearance")
                        returnProperty = new Appearance();
                    else
                        returnProperty = new StructProperty(propName);
                    break;

                case "BoolProperty":
                    returnProperty = new BoolProperty(propName);
                    break;

                default:
                    throw new Exception($"Unexpected property type: '{typeName}'");
            }

            returnProperty.ParseData(this);
            return returnProperty;
        }

        private byte[] ReadBytes(int length)
        {
            // Check if we have enough bytes remaining
            if (curFilePosition + length > _rawBytes.Length)
            {
                int remaining = _rawBytes.Length - curFilePosition;
                throw new Exception($"Requesting {length} bytes but only {remaining} bytes remaining at position {curFilePosition}");
            }

            // Copy a section of our raw bytes to a subarray, then increment out current file position
            byte[] subArray = new byte[length];
            Array.Copy(_rawBytes, curFilePosition, subArray, 0, length);
            curFilePosition += length;

            return subArray;
        }

        public void SkipPadding()
        {
            curFilePosition += 4;
        }

        #endregion

        #region Static Write Methods

        public static byte[] WriteInt(int value)
        {
            var bytes = BitConverter.GetBytes(value);

            // Correct endianness if necessary
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            return bytes;
        }

        public static byte[] WriteString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return [];

            // Replace CRLF with CR, add a null terminating byte
            var formatted = value.Replace(Environment.NewLine, "\r").Append('\0').ToArray();
            var encoding = Encoding.UTF8; // 使用UTF-8编码用于属性名、类型名等
            return encoding.GetBytes(formatted);
        }

        /// <summary>
        /// 写入StrProperty数据，根据文本内容自动选择编码格式
        /// </summary>
        /// <param name="value">要写入的字符串</param>
        /// <returns>格式化的字节数组</returns>
        public static byte[] WriteStrPropertyData(string value)
        {
            if (string.IsNullOrEmpty(value))
                return [0x00, 0x00, 0x00, 0x00];

            // 格式化字符串：替换换行符，添加null终止符
            var formatted = value.Replace(Environment.NewLine, "\r").Append('\0').ToArray();

            // 检查是否包含非ASCII字符（需要Unicode编码）
            bool containsNonAscii = false;
            foreach (char c in value)
            {
                if (c > 127) // 超出ASCII范围
                {
                    containsNonAscii = true;
                    break;
                }
            }

            if (containsNonAscii)
            {
                // 使用特殊Unicode格式（用于包含非ASCII字符的文本）
                // 转换为UTF-16LE字节
                var unicodeBytes = Encoding.Unicode.GetBytes(formatted);
                // 创建格式：长度前缀 + UTF-8数据
                var lengthBytes = WriteInt(-formatted.Length);
                return [..lengthBytes, ..unicodeBytes];
            }
            else
            {
                // 使用UTF-8格式（用于纯ASCII文本）
                var utf8Bytes = Encoding.UTF8.GetBytes(formatted);
                // 创建格式：长度前缀 + UTF-8数据
                var lengthBytes = WriteInt(formatted.Length);
                return [..lengthBytes, ..utf8Bytes];
            }
        }

        public static byte[] WriteProperty(CProperty property)
        {
            var bytes = new List<byte>();

            // Write length and name of property
            var formattedName = WriteString(property?.Name ?? "None");
            bytes.AddRange(WriteInt(formattedName.Length));
            bytes.AddRange(formattedName);
            bytes.AddRange(WritePadding());

            // If this was a "None" property, nothing else is needed
            if (property == null) return [.. bytes];

            // Write property type string
            formattedName = WriteString(property.GetPropertyType());
            bytes.AddRange(WriteInt(formattedName.Length));
            bytes.AddRange(formattedName);
            bytes.AddRange(WritePadding());

            // Size of data and data itself will be written by the derived types
            bytes.AddRange(property.WriteSizeAndData());

            return [.. bytes];
        }

        public static byte[] WritePadding()
        {
            return new byte[4];
        }

        public static byte[] WriteNone()
        {
            var bytes = new List<byte>();
            bytes.AddRange(WriteInt(5));
            bytes.AddRange(WriteString("None"));
            bytes.AddRange(WritePadding());
            return [.. bytes];
        }

        #endregion
    }
}
