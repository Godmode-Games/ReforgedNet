﻿using ReforgedNet.LL.Serialization;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ReforgedNet.LL
{
    public class RClientSocket : RSocket
    {
        public Action? Connected;
        public Action? Disconnected;
        public Action? ConnectFailed;

        private int? DiscoverTransaction = null;
        private int? DisconnectTransation = null;
        public bool IsConnected { get; private set; } = false;

        public RClientSocket(RSocketSettings settings, IPacketSerializer serializer, ILogger? logger)
            : base(settings, new IPEndPoint(IPAddress.Any, 0), serializer, logger) { }

        /// <summary>
        /// Connecting to specified endpoint
        /// </summary>
        public void Connect(IPEndPoint ep)
        {
            RemoteEndPoint = ep;
            CreateSocket();

            _sendTask = Task.Factory.StartNew(() => SendingTask(_cts.Token), _cts.Token);
            _sendTask.ConfigureAwait(false);

            RegisterReceiver(null, OnDiscoverMessage);
            SendHello();
        }

        /// <summary>
        /// Login failed
        /// </summary>
        /// <param name="tid"></param>
        private void OnDiscoverFailed(int tid)
        {
            if (tid == DiscoverTransaction)
            {
                IsConnected = false;
                ConnectFailed?.Invoke();
            }
        }

        /// <summary>
        /// Close connection
        /// </summary>
        public void DisconnectAsync()
        {
            SendDisconnect();
        }

        /// <summary>
        /// Sends Discover Message
        /// </summary>
        private void SendHello()
        {
            //Send discover message
            if (DiscoverTransaction == null)
            {
                DiscoverTransaction = RTransactionGenerator.GenerateId();
            }

            Error += OnDiscoverFailed;

            RNetMessage discover = new RNetMessage(null, Encoding.UTF8.GetBytes("discover"), DiscoverTransaction, RemoteEndPoint, RQoSType.Realiable);
            _outgoingMsgQueue.Enqueue(discover);
        }

        /// <summary>
        /// Discover-Answer arrived
        /// </summary>
        /// <param name="message"></param>
        private void OnDiscoverMessage(RNetMessage message)
        {
            string type = "discover";
            try
            {
                type = Encoding.UTF8.GetString(message.Data);
            }
            catch
            {
                //Encoding error
                return;
            }

            if (type.Equals("disconnect_response") && IsConnected && DisconnectTransation == message.TransactionId)
            {
                //Disconnect answer from server
                _logger?.WriteInfo(new LogInfo("Disconnect successful"));
                IsConnected = false;
                DisconnectTransation = null;

                Disconnected?.Invoke();
                Error -= OnDisconnectFailed; //Clear all discover-fails
            }
            else if (type.Equals("disconnect_request") && IsConnected)
            {
                //Server requests a disconnect -> anwser confirm
                RNetMessage answer = new RNetMessage(null, Encoding.UTF8.GetBytes("disconnect_response"), message.TransactionId, RemoteEndPoint, RQoSType.Realiable);
                _logger?.WriteInfo(new LogInfo("Disconnect by server"));

                IsConnected = false;
                DisconnectTransation = null;
                Disconnected?.Invoke();
            }
            else if (type.Equals("discover") && !IsConnected && DiscoverTransaction == message.TransactionId)
            {
                //Discover answer from server
                _logger?.WriteInfo(new LogInfo("Connection successful"));
                IsConnected = true;
                DiscoverTransaction = null;
                Error -= OnDiscoverFailed; //Clear all discover-fails

                Connected?.Invoke();
            }
        }

        /// <summary>
        /// Sends disconnect message
        /// </summary>
        private void SendDisconnect()
        {
            if (DisconnectTransation == null)
            {
                DisconnectTransation = RTransactionGenerator.GenerateId();
            }

            Error += OnDisconnectFailed;
            RNetMessage disc = new RNetMessage(null, Encoding.UTF8.GetBytes("disconnect_request"), DisconnectTransation, RemoteEndPoint, RQoSType.Realiable);
            _outgoingMsgQueue.Enqueue(disc);
        }

        /// <summary>
        /// Disconnect failed, never the less disconnect
        /// </summary>
        private void OnDisconnectFailed(int tid)
        {
            if (tid == DisconnectTransation)
            {
                _logger?.WriteInfo(new LogInfo("Disconnect failed, cancel connection anyways"));
                IsConnected = false;
                DisconnectTransation = null;

                Disconnected?.Invoke();
            }
        }
    }
}
