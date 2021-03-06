﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace ReforgedNet.LL
{
    /// <summary>
    /// Network message to tell notify remote endpoint of received messages.
    /// </summary>
    public class RReliableNetMessageACK
    {
        public readonly int? MessageId;
        public readonly long TransactionId;
        public readonly EndPoint RemoteEndPoint;

        public RReliableNetMessageACK(int? messageId, long transactionId, EndPoint remoteEndPoint)
        {
            MessageId = messageId;
            TransactionId = transactionId;
            RemoteEndPoint = remoteEndPoint;
        }
    }
}
