﻿using System.Collections.Generic;
using System.Text;

namespace HT.Framework
{
    /// <summary>
    /// 默认的网络消息
    /// 消息头：消息校验码4字节 + 消息体长度4字节 + 身份ID8字节 + 主命令4字节 + 子命令4字节 + 加密方式4字节 + 返回码4字节 = 32字节
    /// 消息体：消息1长度4字节 + 消息1 + 消息2长度4字节 + 消息2......（消息体的长度存储在消息头的4至8索引位置的字节里）
    /// </summary>
    public sealed class NetworkInfo : INetworkInfo
    {
        public int CheckCode;
        public int BodyLength;
        public long Sessionid;
        public int Command;
        public int Subcommand;
        public int Encrypt;
        public int ReturnCode;
        public List<string> Messages;

        /// <summary>
        /// 填充
        /// </summary>
        public void Fill(int crccode, long sessionid, int command, int subcommand, int encrypt, int returnCode, string[] messageBody)
        {
            CheckCode = crccode;
            BodyLength = 0;
            Sessionid = sessionid;
            Command = command;
            Subcommand = subcommand;
            Encrypt = encrypt;
            ReturnCode = returnCode;
            Messages = new List<string>(messageBody);
            for (int i = 0; i < Messages.Count; i++)
            {
                if (Messages[i] == "" || string.IsNullOrEmpty(Messages[i]))
                    break;
                BodyLength += Encoding.UTF8.GetBytes(Messages[i]).Length;
            }
            BodyLength += Messages.Count * 4;
        }
    }
}