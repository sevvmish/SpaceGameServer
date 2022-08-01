using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceGameServer
{
    internal class test
    {
        public static async void sender()
        {
            await Task.Delay(1000);

            while (true)
            {
                if (Server.d.Count>0)
                {
                    foreach (var item in Server.d.Keys)
                    {
                        await item.SendAsync(Encoding.UTF8.GetBytes("give back"), System.Net.Sockets.SocketFlags.None);
                        await Task.Delay(500);
                    }
                }
            }

        }

    }

    [ProtoContract]
    public class PacketToReceive
    {
        [ProtoMember(1)]
        public float x { get; set; }

        [ProtoMember(2)]
        public float z { get; set; }
    }
}
