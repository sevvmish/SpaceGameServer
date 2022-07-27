using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using NetCoreServer;


namespace SpaceGameServer
{
    class Server
    {
        public static Dictionary<string, byte[]> Sessions = new Dictionary<string, byte[]>();
        //UDP     
        private const int PORT_UDP = 2325;
        public static UDPServerConnector ServerUDP;

        //TCP
        public static ManualResetEvent allDone = new ManualResetEvent(false);
        private static IPAddress ipaddress_tcp;
        private static IPEndPoint localendpoint_tcp;
        private static Socket socket_tcp;
        public const int PORT_TCP = 2328;        
        private const int max_connections = 10000;

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
            Server_init_TCP();

        }

        //START FOR TCP
        public static void Server_init_TCP()
        {
            //TCP config===================================
            ipaddress_tcp = IPAddress.Any;
            localendpoint_tcp = new IPEndPoint(ipaddress_tcp, PORT_TCP);
            socket_tcp = new Socket(ipaddress_tcp.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            Console.WriteLine(DateTime.Now + ": " + "game server TCP initiated");

            try
            {
                socket_tcp.Bind(localendpoint_tcp);
                socket_tcp.Listen(max_connections);

                while (true)
                {
                    // Set the event to nonsignaled state.  
                    allDone.Reset();

                    socket_tcp.BeginAccept(new AsyncCallback(AcceptCallbackTCP), socket_tcp);

                    allDone.WaitOne();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
            }
            //TCP config===================================
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
            //return Task.CompletedTask;
        }



        public static void AcceptCallbackTCP(IAsyncResult ar)
        {
            try
            {
                // Signal the main thread to continue.  
                allDone.Set();
                Socket listener = (Socket)ar.AsyncState;
                Socket handler = listener.EndAccept(ar);

                // Create the state object.  
                StateObject state = new StateObject();
                state.workSocket = handler;
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallbackTCP), state);
            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
            }
        }

        public static void ReadCallbackTCP(IAsyncResult ar)
        {
            try
            {
                //raw_data_received_tcp.Clear();

                StateObject state = (StateObject)ar.AsyncState;
                Socket handler = state.workSocket;
                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {

                    IncomingDataHadler.HandleIncomingTCP(bytesRead, handler, state.buffer);

                }
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

                buffer_send_tcp = Encoding.UTF8.GetBytes(data);

                // Begin sending the data to the remote device.  
                handler.BeginSend(buffer_send_tcp, 0, buffer_send_tcp.Length, 0, new AsyncCallback(SendCallback), handler);
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

                buffer_send_tcp = data;

                // Begin sending the data to the remote device.  
                handler.BeginSend(buffer_send_tcp, 0, buffer_send_tcp.Length, 0, new AsyncCallback(SendCallback), handler);
            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
            }
            return Task.CompletedTask;
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = handler.EndSend(ar);
                //Console.WriteLine("Sent {0} bytes to client.", bytesSent);

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
            }
        }
        
    }

    // State object for reading client data asynchronously  
    public class StateObject
    {
        // Size of receive buffer.  
        public const int BufferSize = 2048;

        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];

        // Received data string.
        public StringBuilder sb = new StringBuilder();

        // Client socket.
        public Socket workSocket = null;
    }


    class UDPServerConnector : UdpServer
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

            if (size != 0)
            {                
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
            Console.WriteLine($"Server caught an error with code {error} ");
        }

    }

}
