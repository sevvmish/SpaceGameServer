using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using System.Linq;


namespace SpaceGameServer
{
    class IncomingDataHadler
    {
        private const float TIME_FOR_BAD_DATA_TO_BECOME_BAN_SECONDS = 2f;
        private const float COUNT_FOR_BAD_DATA_TO_BECOME_BAN = 10f;
        private const int HOW_LONG_BASE_BAN_MILISEC = 2000;

        private static Dictionary<string, int> BadDataSuppliers = new Dictionary<string, int>();
        private static HashSet<string> BanListAddresses = new HashSet<string>();
        private static HashSet<string> BannedFirstRowHystoryLOG = new HashSet<string>();
        private static HashSet<string> BannedSecondRowHystoryLOG = new HashSet<string>();
        private static HashSet<string> BannedThirdRowHystoryLOG = new HashSet<string>();

        private static MemoryStream streamForIncomingPackets;

        public static void AddBadDataSupplier(IPEndPoint _address)
        {
            if (_address == null)
            {
                Console.WriteLine("ERROR inc data");
                return;
            }

            string IP = _address.ToString().Split(':')[0];
            if (!BadDataSuppliers.ContainsKey(IP))
            {
                BadDataSuppliers.TryAdd(IP, 1);
                CheckBadDataSupplierForBanOption(IP);
                Console.WriteLine(DateTime.Now + ": Added new bad data supplier with IP " + IP + " address from " + _address.ToString());
            }
            else
            {
                BadDataSuppliers[IP] += 1;
            }
        }

        private static async void CheckBadDataSupplierForBanOption(string IP)
        {
            await Task.Delay((int)(TIME_FOR_BAD_DATA_TO_BECOME_BAN_SECONDS * 1000));

            if (!BadDataSuppliers.ContainsKey(IP))
            {
                return;
            }

            if (BadDataSuppliers.ContainsKey(IP) && BadDataSuppliers[IP] > COUNT_FOR_BAD_DATA_TO_BECOME_BAN)
            {

                AddToBanList(IP);
            }
            else if (BadDataSuppliers.ContainsKey(IP) && BadDataSuppliers[IP] <= COUNT_FOR_BAD_DATA_TO_BECOME_BAN)
            {
                BadDataSuppliers.Remove(IP);
            }
        }

        private static async void AddToBanList(string IP)
        {
            if (BanListAddresses.Contains(IP))
            {
                return;
            }

            BanListAddresses.Add(IP);
            int koeff = 1;
            if (BannedFirstRowHystoryLOG.Contains(IP)) koeff = 5;
            if (BannedSecondRowHystoryLOG.Contains(IP)) koeff = 30;
            if (BannedThirdRowHystoryLOG.Contains(IP)) koeff = 1800;

            switch (koeff)
            {
                case 1:
                    Console.WriteLine(DateTime.Now + ": IP added to ban list " + IP);
                    break;
                case 5:
                    Console.WriteLine(DateTime.Now + ": IP added to ban list second time " + IP);
                    break;
                case 30:
                    Console.WriteLine(DateTime.Now + ": IP added to ban list third time " + IP);
                    break;
                case 1800:
                    Console.WriteLine(DateTime.Now + ": IP added to ban list forth or more times " + IP);
                    break;
            }

            await Task.Delay(HOW_LONG_BASE_BAN_MILISEC * koeff);

            if (!BannedFirstRowHystoryLOG.Contains(IP) && !BannedSecondRowHystoryLOG.Contains(IP) && !BannedThirdRowHystoryLOG.Contains(IP)) BannedFirstRowHystoryLOG.Add(IP);
            if (BannedFirstRowHystoryLOG.Contains(IP) && !BannedSecondRowHystoryLOG.Contains(IP) && !BannedThirdRowHystoryLOG.Contains(IP)) BannedSecondRowHystoryLOG.Add(IP);
            if (BannedFirstRowHystoryLOG.Contains(IP) && BannedSecondRowHystoryLOG.Contains(IP) && !BannedThirdRowHystoryLOG.Contains(IP)) BannedThirdRowHystoryLOG.Add(IP);

            if (BanListAddresses.Contains(IP)) BanListAddresses.Remove(IP);
            if (BadDataSuppliers.ContainsKey(IP)) BadDataSuppliers.Remove(IP);
        }

        public static void HandleIncomingTCP(int bytesRead, Socket handler, byte[] buffer)
        {
            try
            {
                if (BanListAddresses.Contains(handler.RemoteEndPoint.ToString().Split(':')[0]))
                {
                    Console.WriteLine(DateTime.Now + ": detected connection try from banned address " + handler.RemoteEndPoint);
                    return;
                }

                string PacketID = Encoding.UTF8.GetString(buffer, 0, 3);

                Console.WriteLine(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                
                if (!Server.IncomingPlayersNetworkSecurity.ContainsKey(PacketID))
                {                    
                    packet_analyzer.StartSessionTCPInput(Encoding.UTF8.GetString(buffer, 0, bytesRead), handler);
                    
                }
                else
                {
                   
                    byte[] d = Encryption.TakeSomeToArrayFromTo(buffer, 3, bytesRead);

                    Encryption.Decode(ref d, Server.IncomingPlayersNetworkSecurity[PacketID]);
                    Console.WriteLine(Encoding.UTF8.GetString(d));
                    
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
                AddBadDataSupplier((IPEndPoint)handler.RemoteEndPoint);
            }
        }



        public static void HandleIncomingUDP(EndPoint endpoint, byte[] buffer)
        {
            try
            {
                IPEndPoint IP = endpoint as IPEndPoint;

                if (BanListAddresses.Contains(IP.ToString()))
                {
                    Console.WriteLine(DateTime.Now + ": detected connection try from banned address " + endpoint);
                    return;
                }

                string _key = Encoding.UTF8.GetString(buffer, 0, 3);

                //Console.WriteLine(Encoding.UTF8.GetString(buffer, 0, buffer.Length));

                /*
                if (server.Sessions.ContainsKey(_key))
                {
                    encryption.Decode(ref buffer, server.Sessions[_key]);

                    if (buffer[5] == 0)
                    {
                        using (streamForIncomingPackets = new MemoryStream(encryption.TakeSomeToArrayFromNumber(buffer, 6)))
                        {
                            packetToSendMove = Serializer.Deserialize<PacketToSendMovement>(streamForIncomingPackets);
                            packetContainer = new PacketContainer(packetToSendMove);
                        }
                    }
                    else if (buffer[5] == 1)
                    {
                        using (streamForIncomingPackets = new MemoryStream(encryption.TakeSomeToArrayFromNumber(buffer, 6)))
                        {
                            packetToSendButtons = Serializer.Deserialize<PacketToSendButtons>(streamForIncomingPackets);
                            packetContainer = new PacketContainer(packetToSendButtons);
                        }
                    }


                    if (!server.UDPClientsENDPoints.Contains(endpoint))
                    {
                        packet_analyzer.ProcessUDPinitPlayer(endpoint, packetContainer.playerID, packetContainer.sessionID, server.Sessions[_key]);
                    }

                    packet_analyzer.ProcessUDPActivePacket(packetContainer);
                }
                else
                {
                    encryption.Decode(ref buffer, starter.secret_key_for_game_servers);
                    string data_result = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
                    string[] packet_data = data_result.Split('~');

                    if ((packet_data[0] + packet_data[1]) == "07")
                    {
                        packet_analyzer.ProcessPing(data_result, endpoint);
                    }
                }
                */

            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
                AddBadDataSupplier((IPEndPoint)endpoint);
            }


        }
    }

}
