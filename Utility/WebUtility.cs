using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Utility
{
    public static class WebUtility
    {
        public static int GetNextAvailableUDPPortNumber()
        {
            int startingAtPort = 1025;
            int maxNumberOfPortsToCheck = 500;
            var range = Enumerable.Range(startingAtPort, maxNumberOfPortsToCheck);
            var portsInUse = from p in range
                                join used in System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners()
                                on p equals used.Port
                                select p;

            return range.Except(portsInUse).FirstOrDefault();

        }

        public static bool IsPortOpen(int port)
        {

            if (port < 1025 || port > 65536)
                return false;

            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] tcpEndPoints = properties.GetActiveTcpListeners();
            List<int> usedPorts = tcpEndPoints.Select(p => p.Port).ToList<int>();

            if (usedPorts.Contains(port))
                return false;
            else
                return true;
        }

        public static IPAddress GetLocalIPAddress()
        {
            IPHostEntry iphostentry = Dns.GetHostByName(Dns.GetHostName());
            return iphostentry.AddressList.FirstOrDefault<IPAddress>();
        }

        public static void BroadcastMessage(int port, string message)
        {
            IPEndPoint endpoint = new IPEndPoint(100, 0);
            BroadcastMessage(port, message, Encoding.UTF8, ref endpoint);
        }

        public static void BroadcastMessage(int port, string message, ref IPEndPoint endPoint)
        {
            BroadcastMessage(port, message, Encoding.UTF8, ref endPoint);
        }

        public static void BroadcastMessage(int port, string message, Encoding encoding, ref IPEndPoint returnEndPoint)
        {
            try
            {
                UdpClient client = new UdpClient();
                IPEndPoint ip = new IPEndPoint(IPAddress.Broadcast, port);
                byte[] bytes = encoding.GetBytes(message);
                client.Send(bytes, bytes.Length, ip);
                client.Close();
            }
            catch(Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }

        public class MessageReceivedEventArgs : EventArgs
        {
            public IPEndPoint endpoint;
            public string message;
        }

        public delegate void MessageReceivedEventHandler(Object sender, MessageReceivedEventArgs e);

        public class UdpConnector
        {
            
            private Thread receiverThread;

            public IPEndPoint ReceiverEP { get; private set; }
            public IPEndPoint SenderEP { get; private set; }

            private UdpClient receiverClient;
            private UdpClient senderClient;

            public MessageReceivedEventHandler MessageReceived;

            private Encoding _encoding;
            public Encoding Encoding
            {
                get
                {
                    return _encoding;
                }
                set
                {
                    if (receiverThread != null && receiverThread.ThreadState == ThreadState.Running)
                        return;
                    this._encoding = value;
                }
            }

            private bool _listen;
            public bool Listen
            {
                get
                {
                    return this._listen;
                }
                set
                {
                    if (_listen != value && value == true)
                        StartReceiving();
                    else if (_listen != value && value == false)
                        StopReceiving();

                    this._listen = value;
                }
            }

            private class IncomingMessage
            {
                public IPEndPoint endpoint;
                public Byte[] data;

                public IncomingMessage(IPEndPoint endpoint, Byte[] data)
                {
                    this.endpoint = new IPEndPoint(endpoint.Address, endpoint.Port);
                    this.data = data;
                }
            }

            public UdpConnector()
            {
                _encoding = System.Text.Encoding.UTF8;
                _listen = false;

                receiverClient = null;
                senderClient = null;

                ReceiverEP = null;
                SenderEP = null;

            }

            private void InitializeSender()
            {
                if(senderClient == null)
                {
                    SenderEP = new IPEndPoint(IPAddress.Any, GetNextAvailableUDPPortNumber());
                    senderClient = new UdpClient();
                    senderClient.Client.Bind(SenderEP);
                }
            }

            private void InitializeReceiver()
            {
                if(receiverClient == null)
                {
                    ReceiverEP = new IPEndPoint(IPAddress.Any, GetNextAvailableUDPPortNumber());
                    receiverClient = new UdpClient();
                    receiverClient.Client.Bind(ReceiverEP);
                }
            }

            private void CloseSender()
            {
                if (senderClient != null)
                    senderClient.Close();

                senderClient = null;
                SenderEP = null;
            }

            private void CloseReceiver()
            {
                if (receiverClient != null)
                    receiverClient.Close();

                receiverClient = null;
                ReceiverEP = null;
            }

            public void Close()
            {
                StopReceiving();
            }

            private void StartReceiving()
            {
                if (receiverThread != null && receiverThread.ThreadState == ThreadState.Running)
                {
                    StopReceiving();
                }
                InitializeReceiver();
                receiverThread = new Thread(ReceiveMessage);
                receiverThread.Start();
            }

            private void StopReceiving()
            {
                if (receiverThread.ThreadState == ThreadState.Running)
                {
                    receiverThread.Abort();
                    CloseReceiver();
                    receiverThread.Join();
                }
            }

            private void ReceiveMessage()
            {
                try
                {
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

                    while (true)
                    {
                        byte[] data = receiverClient.Receive(ref remoteEP);

                        if (data == null || data.Length == 0)
                            break;

                        IncomingMessage message = new IncomingMessage(remoteEP, data);
                        new Thread(ReceivedMessage).Start(message);
                    }

                }
                catch (ThreadAbortException)
                {
                    Console.WriteLine("Listener closed");
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                }
            }

            private void ReceivedMessage(object input)
            {
                byte[] data = ((IncomingMessage)(input)).data;
                string message = _encoding.GetString(data, 0, data.Length);

                if(MessageReceived != null)
                {
                    MessageReceivedEventArgs args = new MessageReceivedEventArgs();
                    args.endpoint = ((IncomingMessage)(input)).endpoint;
                    args.message = message;
                    MessageReceived(this, args);
                }
            }
        }
    }

}




