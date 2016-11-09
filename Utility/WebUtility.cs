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

            byte[] randomKey = Encoding.UTF8.GetBytes(new Random().Next().ToString());
            int port = GetNextAvailableUDPPortNumber();
            UdpClient client = new UdpClient(new IPEndPoint(IPAddress.Any, port));
            client.Send(randomKey, randomKey.Length, new IPEndPoint(IPAddress.Broadcast, port));
            IPHostEntry iphostentry = Dns.GetHostByName(Dns.GetHostName());
            IPEndPoint incomeEP = null;

            while(true)
            {
                byte[] incomingKey = client.Receive(ref incomeEP);
                if (incomeEP.Port == port && iphostentry.AddressList.Contains(incomeEP.Address))
                {
                    //Console.WriteLine(incomeEP.Address + " " + randomKey + " " + incomingKey);
                    break;
                }
                    
            }

            

            return incomeEP.Address;
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
            public IPEndPoint RemoteEndpoint { get; private set; }
            public IPEndPoint LocalEndpoint { get; private set; }
            public string Message { get; private set; }

            public MessageReceivedEventArgs(IPEndPoint remoteEndpoint, IPEndPoint localEndpoint, string message)
            {
                this.RemoteEndpoint = remoteEndpoint;
                this.LocalEndpoint = localEndpoint;
                this.Message = message;
            }
        }

        public delegate void MessageReceivedEventHandler(Object sender, MessageReceivedEventArgs e);

        internal class IncomingMessage
        {
            public IPEndPoint endpoint;
            public Byte[] data;

            public IncomingMessage(IPEndPoint endpoint, Byte[] data)
            {
                this.endpoint = new IPEndPoint(endpoint.Address, endpoint.Port);
                this.data = data;
            }
        }

        public class UdpConnector
        {

            private Thread receiverThread;

            public IPEndPoint UdpEP { get; private set; }

            private UdpClient udpClient;

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

            public UdpConnector(IPEndPoint endpoint = null)
            {
                _encoding = System.Text.Encoding.UTF8;
                _listen = false;

                udpClient = null;
                UdpEP = endpoint;

            }

            public void SendMessage(string message, IPEndPoint destination)
            {
                if (udpClient == null)
                    Initialize();

                byte[] encodedMessage = _encoding.GetBytes(message);

                lock(udpClient)
                {
                    udpClient.Send(encodedMessage, encodedMessage.Length, destination);
                }
            }

            private void Initialize()
            {
                if (udpClient == null)
                {
                    if(UdpEP == null)
                        UdpEP = new IPEndPoint(IPAddress.Any, GetNextAvailableUDPPortNumber());
                    udpClient = new UdpClient();
                    udpClient.Client.Bind(UdpEP);
                }
            }


            private void CloseReceiver()
            {
                if (udpClient != null)
                    udpClient.Close();

                udpClient = null;
                UdpEP = null;
            }

            public void Close()
            {
                if (receiverThread.ThreadState == ThreadState.Running)
                    StopReceiving();
                else
                    CloseReceiver();
                
            }

            private void StartReceiving()
            {
                if (receiverThread != null && receiverThread.ThreadState == ThreadState.Running)
                {
                    StopReceiving();
                }
                Initialize();
                receiverThread = new Thread(ReceiveMessage);
                receiverThread.Start();
            }

            private void StopReceiving()
            {
                receiverThread.Abort();
                CloseReceiver();
                receiverThread.Join();
            }

            private void ReceiveMessage()
            {
                try
                {
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

                    while (true)
                    {
                        byte[] data = udpClient.Receive(ref remoteEP);
                        Console.WriteLine(udpClient.Client.LocalEndPoint);
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
                Console.WriteLine("Thread ID" + Thread.CurrentThread.ManagedThreadId);
                if (MessageReceived != null)
                {
                    MessageReceivedEventArgs args = new MessageReceivedEventArgs(((IncomingMessage)(input)).endpoint, UdpEP, message);
                    MessageReceived(this, args);
                }
            }
        }
    }

}




