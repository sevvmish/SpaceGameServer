using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SpaceGameServer
{
    public class ServerLitenetlib
    {
        public static EventBasedNetListener listener;
        public static NetManager server;

        public static void Server()
        {
            listener = new EventBasedNetListener();
            server = new NetManager(listener);
            server.Start(SpaceGameServer.Server.PORT_UDP);

            listener.ConnectionRequestEvent += request =>
            {
                if (server.ConnectedPeersCount < 1000 /* max connections */)
                    request.Accept();
                else
                    request.Reject();
            };

            listener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine("We got connection: {0}", peer.EndPoint); // Show peer ip
                NetDataWriter writer = new NetDataWriter();                 // Create writer class
                writer.Put("Hello client!");                                // Put some string
                peer.Send(writer, DeliveryMethod.ReliableOrdered);             // Send with reliability
            };

            listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod) =>
            {
                Console.WriteLine("We got: {0}", dataReader.GetInt());
                dataReader.Recycle();
            };

            while (!Console.KeyAvailable)
            {
                server.PollEvents();
                Thread.Sleep(15);
            }
            //server.Stop();
        }



    }
}
