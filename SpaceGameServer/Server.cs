using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using NetCoreServer;
using System.Collections.Concurrent;
using LiteNetLib;
using LiteNetLib.Utils;

namespace SpaceGameServer
{
    public class Server
    {
        public static Dictionary<string, byte[]> IncomingPlayersNetworkSecurity = new Dictionary<string, byte[]>();
        public static ConcurrentDictionary<Socket, int> d = new ConcurrentDictionary<Socket, int>();

        //UDP     
        public const int PORT_UDP = 2301;
        public static UDPServerConnector ServerUDP;
        public static TCPServer ServerTCP;

        //TCP
        public static ManualResetEvent allDone = new ManualResetEvent(false);
        private static IPAddress ipaddress_tcp;
        private static IPEndPoint localendpoint_tcp;
        private static Socket socket_tcp;
        public const int PORT_TCP = 2300;        
       

        //General
        public static HashSet<EndPoint> UDPClientsENDPoints = new HashSet<EndPoint>();
        private static byte[] buffer_send_tcp = new byte[2048];

        //INITIAL STARTER FOR TCP AND UDP
        public static void Server_init()
        {
            
            //init UDP
            Task.Run(() =>
            {
                ServerUDP = new UDPServerConnector(IPAddress.Any, PORT_UDP);
                ServerUDP.Start();
            });
            

            //init TCP
            Task.Run(() =>
            {
                ServerTCP = new TCPServer(IPAddress.Any, PORT_TCP);
                ServerTCP.Start();
            });

        }

  
        public static Task SendDataUDP(EndPoint ipEnd, string data)
        {
            try
            {
                ServerUDP.Send(ipEnd, data);
            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
            }
            return Task.CompletedTask;
        }

        public static Task SendDataUDP(EndPoint ipEnd, byte[] data)
        {
            try
            {
                ServerUDP.Send(ipEnd, data);
            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
            }
            return Task.CompletedTask;
        }

        public async static void SendDataUDPTwice(EndPoint ipEnd, byte[] data)
        {
            try
            {
                ServerUDP.Send(ipEnd, data);
                await Task.Delay(Starter.TICKi);
                ServerUDP.Send(ipEnd, data);
                await Task.Delay(Starter.TICKi);
                ServerUDP.Send(ipEnd, data);
            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
            }            
        }


        public static Task SendDataTCP(Socket handler, String data)
        {
            try
            {
                handler.SendAsync(Encoding.UTF8.GetBytes(data), SocketFlags.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
            }
            return Task.CompletedTask;
        }

        public static Task SendDataTCP(Socket handler, byte[] data)
        {
            try
            {
                handler.SendAsync(data, SocketFlags.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
            }
            return Task.CompletedTask;
        }

    }



    public class UDPServerConnector : UdpServer
    {

        public UDPServerConnector(IPAddress address, int port) : base(address, port) { }


        protected override void OnStarted()
        {
            // Start receive datagrams
            try
            {
                Console.WriteLine(DateTime.Now + ": " + "game server UDP initiated");
                ReceiveAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
            }

        }

       

        protected override void OnReceived(EndPoint endpoint, byte[] buffer, long offset, long size)
        {
            EventBasedNetListener netListener = new EventBasedNetListener();
            NetManager m = new NetManager(netListener);
            NetPacketProcessor netPacketProcessor = new NetPacketProcessor();

            netListener.PeerConnectedEvent += (server) => {
                Console.WriteLine($"Connected to server: {server}");
            };

            netListener.NetworkReceiveEvent += (server, reader, deliveryMethod) => {
                byte[] t = new byte[] { };
                reader.GetBytes(t, 0, reader.AvailableBytes);
                Console.WriteLine(Encoding.UTF8.GetString(t));
            };

         

            if (size != 0)
            {
                //Console.WriteLine(Encoding.UTF8.GetString(buffer, 0, (int)size));

                byte[] t = new byte[(int)size];

                for (int i = 0; i < (int)size; i++)
                {             
                    t[i] = buffer[i];
                }
                
                IncomingDataHadler.HandleIncomingUDP(endpoint, t);

            }
            
            ReceiveAsync();
        }


        protected override void OnError(SocketError error)
        {
            Console.WriteLine("==============ERROR================\n" + error + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
        }
    }


    public class TCPSession : TcpSession
    {
        public TCPSession(TcpServer server) : base(server) { }

        protected override void OnConnected()
        {
            Console.WriteLine($"{Id} connected!");
            //SpaceGameServer.Server.d.TryAdd(Socket, 1);
        }

        protected override void OnDisconnected()
        {
            Console.WriteLine($"{Id} disconnected!");
            //int x = 0;
            //SpaceGameServer.Server.d.TryRemove(Socket, out x);
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            IncomingDataHadler.HandleIncomingTCP((int)size, Socket, buffer);
            
            
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine("==============ERROR================\n" + error + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
        }
    }

    public class TCPServer : TcpServer
    {
        public TCPServer(IPAddress address, int port) : base(address, port) { }

        protected override TcpSession CreateSession() { return new TCPSession(this); }

        protected override void OnStarted()
        {
            // Start receive datagrams
            try
            {
                Console.WriteLine(DateTime.Now + ": " + "server TCP initiated");
            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
            }

        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine("==============ERROR================\n" + error + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
        }
    }

}
