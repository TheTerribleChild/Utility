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
        public static int GetNextAvailablePortNumber()
        {
            int PortStartIndex = 1025;
            int PortEndIndex = 65536;
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] tcpEndPoints = properties.GetActiveTcpListeners();

            List<int> usedPorts = tcpEndPoints.Select(p => p.Port).ToList<int>();
            int unusedPort = 0;

            for (int port = PortStartIndex; port < PortEndIndex; port++)
            {
                if (!usedPorts.Contains(port))
                {
                    unusedPort = port;
                    break;
                }
            }
            return unusedPort;
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

        public static void BroadcastMessage(int port, string message)
        {
            BroadcastMessage(port, message, Encoding.UTF8);
        }

        public static void BroadcastMessage(int port, string message, Encoding encoding)
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

        public class BroadcastReceivedEventArgs : EventArgs
        {
            public IPEndPoint endpoint;
            public string message;
        }

        public delegate void BroadcastReceivedEventHandler(Object sender, BroadcastReceivedEventArgs e);

        public class BroadcastListener
        {
            private UdpClient broadcastClient;
            private IPEndPoint broadcastListenerGroupEP;
            private Thread broadcastListenThread;

            public BroadcastReceivedEventHandler BroadcastReceived;

            private int _port;
            public int Port
            {
                get
                {
                    return _port;
                }
                set
                {
                    if (broadcastListenThread != null && broadcastListenThread.ThreadState == ThreadState.Running)
                        return;
                    this._port = value;
                }
            }

            private Encoding _encoding;
            public Encoding Encoding
            {
                get
                {
                    return _encoding;
                }
                set
                {
                    if (broadcastListenThread != null && broadcastListenThread.ThreadState == ThreadState.Running)
                        return;
                    this._encoding = value;
                }
            }

            private class BroadcastMessage
            {
                public IPEndPoint endpoint;
                public Byte[] data;

                public BroadcastMessage(IPEndPoint endpoint, Byte[] data)
                {
                    this.endpoint = new IPEndPoint(endpoint.Address, endpoint.Port);
                    this.data = data;
                }
            }

            public BroadcastListener()
            {
                _port = -1;
                _encoding = System.Text.Encoding.UTF8;
            }

            public void Start()
            {
                if (broadcastListenThread != null && broadcastListenThread.ThreadState == ThreadState.Running)
                {
                    Stop();
                }
                broadcastListenThread = new Thread(ListenToBroadcast);
                broadcastListenThread.Start();
            }

            public void Stop()
            {
                if (broadcastListenThread.ThreadState == ThreadState.Running)
                {
                    broadcastListenThread.Abort();
                    broadcastClient.Close();
                    broadcastListenThread.Join();
                }
            }

            private void ListenToBroadcast()
            {
                try
                {
                    broadcastClient = new UdpClient();
                    if(_port == -1)
                        _port = GetNextAvailablePortNumber();
                    broadcastListenerGroupEP = new IPEndPoint(IPAddress.Any, _port);

                    broadcastClient.Client.Bind(broadcastListenerGroupEP);
                    while (true)
                    {
                        byte[] data = broadcastClient.Receive(ref broadcastListenerGroupEP);

                        if (data == null || data.Length == 0)
                            break;

                        BroadcastMessage message = new BroadcastMessage(broadcastListenerGroupEP, data);
                        new Thread(ReceivedMessage).Start(message);
                    }

                }
                catch (ThreadAbortException)
                {
                    _port = -1;
                    broadcastClient.Close();
                    broadcastListenerGroupEP = null;
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                }
            }

            private void ReceivedMessage(object input)
            {
                byte[] data = ((BroadcastMessage)(input)).data;
                string message = _encoding.GetString(data, 0, data.Length);

                if(BroadcastReceived != null)
                {
                    BroadcastReceivedEventArgs args = new BroadcastReceivedEventArgs();
                    args.endpoint = ((BroadcastMessage)(input)).endpoint;
                    args.message = message;
                    BroadcastReceived(this, args);
                }
            }
        }
    }

}




