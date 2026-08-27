using ActionBuffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
namespace Proto
{
    public static class MessageHelper
    {


        const byte StartFlag = 0xfe;
        const byte EndFlag = 0xfd;
        const byte EscapeFlag = 0x7d;

        public static byte[] LegalBuffers(byte[] helpBytes, int size)
        {
            var len = helpBytes != null ? helpBytes.Length : 1024;
            if (len >= size)
                return helpBytes ?? new byte[len];
            while (len < size)
                len *= 2;
            helpBytes = new byte[len];
            return helpBytes;
        }
        public static byte[] OnRecMessage(byte[] buffers, ref int buffer_len, byte[] rec, int offset, int len)
        {
            var target_len = buffer_len + len;
            if (buffers == null)
                buffers = new byte[target_len];
            else if (target_len > buffers.Length)
            {
                var expandLen = buffers.Length * 2;
                while (expandLen < target_len)
                    expandLen *= 2;
                byte[] result = new byte[expandLen];
                Array.Copy(buffers, result, buffers.Length);
                buffers = result;
            }

            var index = buffer_len;
            for (int i = offset; i < len;)
            {
                var _buf = rec[i];
                if (_buf == EscapeFlag)
                {
                    i++;
                    target_len--;
                    _buf = rec[i];
                }
                buffers[index] = _buf;
                index++;
                i++;

            }
            buffer_len = target_len;
            return buffers;
        }
        static byte[] Escape(byte[] buffer, int offset, int len)
        {
            // 假设 StartFlag, EndFlag, EscapeFlag 是类的静态字段或常量
            const byte start = StartFlag;
            const byte end = EndFlag;
            const byte esc = EscapeFlag;

            int count = 0;
            for (int i = offset; i < len; i++)
            {
                byte b = buffer[i];
                if (b == start || b == end || b == esc)
                    count++;
            }

            byte[] result = new byte[len + count];
            int resultPos = 0;
            int last = 0; // 上一个已处理段的结束位置（起始索引）

            // 第二次遍历：批量复制普通字节块，遇到特殊字节时插入转义符
            for (int i = offset; i <= len; i++)
            {
                // 到达末尾或遇到需要转义的字节
                if (i == len || buffer[i] == start || buffer[i] == end || buffer[i] == esc)
                {
                    int blockLen = i - last;
                    if (blockLen > 0)
                    {
                        Buffer.BlockCopy(buffer, last, result, resultPos, blockLen);
                        resultPos += blockLen;
                    }
                    if (i < len) // 处理特殊字节：插入转义符 + 原字节
                    {
                        result[resultPos] = esc;
                        resultPos++;
                        result[resultPos] = buffer[i];
                        resultPos++;
                    }
                    last = i + 1;
                }
            }
            return result;
        }



        public static ArraySegment<byte> Unpack(byte[] buffer, ref int buffer_len, ref byte[] helpBytes)
        {
            Span<byte> span = buffer.AsSpan(0, buffer_len);
            int endIndex = Array.IndexOf(buffer, EndFlag, 0, buffer_len);
            if (endIndex == -1) return default;
            int startIndex = -1;
            for (int i = endIndex; i >= 0; i--)
            {
                if (buffer[i] == StartFlag)
                {
                    startIndex = i;
                    break;
                }
            }

            if (startIndex == -1)
            {
                // 有结束标志但无起始标志，丢弃结束标志之前的所有数据
                int discardLen = endIndex + 1;
                buffer_len -= discardLen;
                if (buffer_len > 0)
                    Array.Copy(buffer, discardLen, buffer, 0, buffer_len);
                return default;
            }
            int msgLen = endIndex - startIndex + 1;

            // 最小长度校验：至少包含 StartFlag(1) + TypeId(2) + EndFlag(1) = 4 字节
            // 若实际消息内容为空（没有 IMessage 数据）则视为无效包
            if (msgLen <= sizeof(ushort) + 2)   // 2字节ID + 2个标志位
            {
                // 丢弃整个无效包
                buffer_len -= (endIndex + 1);
                if (buffer_len > 0)
                    Array.Copy(buffer, endIndex + 1, buffer, 0, buffer_len);
                return default;
            }

            // 确保 helpBytes 容量足够
            helpBytes = LegalBuffers(helpBytes, msgLen);
            Array.Copy(buffer, startIndex, helpBytes, 0, msgLen);

            // 移除已解析的消息数据（包括结束标志及其之前的内容）
            int consumed = endIndex + 1;
            buffer_len -= consumed;
            if (buffer_len > 0)
                Array.Copy(buffer, consumed, buffer, 0, buffer_len);

            return new ArraySegment<byte>(helpBytes, 0, msgLen);
        }

        public static byte[] EncodeBytes(IMessage message)
        {
            var writer = WriteBytes(message);
            var result = Escape(writer.buffer, 0, writer.length);
            writer.Clear();
            BufferWriter.Back(writer);
            return result;
        }
        public static ArraySegment<byte> Encode(IMessage msg)
        {
            var result = MessageHelper.EncodeBytes(msg as IMessage);
            return new ArraySegment<byte>(result, 0, result.Length);
        }
        static BufferWriter WriteBytes(IMessage message)
        {
            var type = message.GetType();

            var writer = BufferWriter.Get();

            writer.WriteByte(StartFlag);

            writer.WriteUInt16(GetMessageCode(type));
            BuffSerializer.WriteObject(writer, message);
            writer.WriteByte(EndFlag);
            var length = writer.length;

            return writer;
        }

        public static bool FromBytes(byte[] bytes, out ushort id, out IMessage message)
        {
            id = 0;
            message = null;
            var reader = BufferReader.Get();
            reader.Init(bytes);
            var startByte = reader.ReadByte();
            id = reader.ReadUInt16();
            var type = GetMessageByCode(id);
            if (type == null)
                return false;
            try
            {
                message = BuffSerializer.ReadObject(reader, type) as IMessage;
                var endByte = reader.ReadByte();
                return message != null;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                BufferReader.Back(reader);
            }
        }


        private static Dictionary<ushort, Type> map2;
        private static Dictionary<Type, ushort> map;
        private static void Init()
        {
            if (map2 != null) return;
            var types = typeof(MessageHelper).Assembly.GetTypes().Where(x => !x.IsAbstract
              && x.GetInterface(nameof(IMessage)) != null);
            map2 = new Dictionary<ushort, Type>();
            map = new Dictionary<Type, ushort>();
            foreach (var type in types)
            {
                var attr = type.GetCustomAttribute<MessageCodeAttribute>();
                if (attr == null) continue;
                var code = ToUInt16(attr.main, attr.sub);
                map2[code] = type;
                map[type] = code;
            }
        }

        public static Type GetMessageByCode(ushort code)
        {
            Init();
            if (map2.TryGetValue(code, out var type)) return type;
            return null;
        }
        public static ushort GetMessageCode(Type type)
        {
            Init();
            if (map.TryGetValue(type, out var code)) return code;
            throw new Exception($"{type} need {nameof(MessageCodeAttribute)}");
        }


        public static void FromUInt16(ushort value, out byte high, out byte low)
        {
            high = (byte)(value >> 8);   // 高8位：0xFF (255)
            low = (byte)(value & 0xFF); // 低
        }
        public static ushort ToUInt16(byte high, byte low)
        {
            return (ushort)((high << 8) | low);
        }
    }

}
