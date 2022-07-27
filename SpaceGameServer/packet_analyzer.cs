using System.Net;
using System.Net.Sockets;
using System.Text;


namespace SpaceGameServer
{
    class packet_analyzer
    {
        private static Dictionary<string, Encryption> TemporarySessionCreator = new Dictionary<string, Encryption>();
        

        public static void StartSessionTCPInput(string data, Socket player_socket)
        {
            try
            {
                string[] packet_data = data.Split('~');

                if (packet_data.Length >= 3 && (packet_data[0] + packet_data[1] + packet_data[2]) == "060")
                {
                    string code = Encryption.get_random_set_of_symb(5);
                    Encryption session_Encryption = new Encryption();
                    Console.WriteLine(DateTime.Now + ": user requested Encryption from - " + player_socket.RemoteEndPoint.ToString());
                    Server.SendDataTCP(player_socket, $"0~6~0~{code}~{session_Encryption.publicKeyInString}");

                    if (!TemporarySessionCreator.ContainsKey(code))
                        TemporarySessionCreator.Add(code, session_Encryption);
                    Task.Run(() => CleanTempSession(code));

                    return;                    
                }

                if (packet_data.Length >= 3 && (packet_data[0] + packet_data[1] + packet_data[2]) == "061" && TemporarySessionCreator.ContainsKey(packet_data[3]))
                {
                    byte[] secret_key = TemporarySessionCreator[packet_data[3]].GetSecretKey(packet_data[4]);
                    if (secret_key == null)
                    {
                        Console.WriteLine("secret key is null!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                    }
                    Server.Sessions.Add(packet_data[3], secret_key);
                    TemporarySessionCreator[packet_data[3]].Dispose();
                    TemporarySessionCreator.Remove(packet_data[3]);
                    Console.WriteLine(DateTime.Now + ": user received Encryption and accepted - " + player_socket.RemoteEndPoint.ToString());
                    Server.SendDataTCP(player_socket, $"0~6~1~ok");

                    return;
                }

                if (packet_data.Length == 4 && (packet_data[0] + packet_data[1] + packet_data[2]) == "062" && Server.Sessions.ContainsKey(packet_data[3]))
                {
                    Server.Sessions.Remove(packet_data[3]);
                    Console.WriteLine(DateTime.Now + ": user removed from current Encryption - " + player_socket.RemoteEndPoint.ToString());
                    Server.SendDataTCP(player_socket, $"0~6~2~ok");
                    return;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + data + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
                IncomingDataHadler.AddBadDataSupplier((IPEndPoint)player_socket.RemoteEndPoint);
            }
        }

        public static string ProcessTCPInput(string data, string endpoint_address)
        {
            try
            {
                string[] packet_data = data.Split('~');
             
                //create new user sending login and password   0~0~login~pass
                if (packet_data.Length == 4 && (packet_data[0] + packet_data[1]) == "00")
                {
                    if (!StringChecker(packet_data[2]) || !StringChecker(packet_data[3]))
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 0~0~wds to user from - " + endpoint_address);
                        return $"0~0~wds"; //wrong digits or signs                    
                    }

                    if (packet_data[2].Length < Starter.PASS_MIN_LENGHT || packet_data[2].Length > Starter.PASS_MAX_LENGHT)
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 0~0~wll to user from - " + endpoint_address);
                        return $"0~0~wll";
                    }

                    if (packet_data[3].Length < Starter.PASS_MIN_LENGHT || packet_data[3].Length > Starter.PASS_MAX_LENGHT)
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 0~0~wlp to user from - " + endpoint_address);
                        return $"0~0~wlp";
                    }

                    string[,] check_twin_name = Mysql.GetMysqlSelect($"SELECT `login` FROM `users` WHERE `login`= '{packet_data[2]}'").Result;
                    if (check_twin_name.GetLength(0) != 0)
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 0~0~uae to user from - " + endpoint_address);
                        return $"0~0~uae";
                    }

                    //creating ticket and hash for pass
                    string user_ticket_d = Encryption.get_random_set_of_symb(10);
                    byte[] res = Encryption.GetHash384(packet_data[3]);

                    bool creating_result = Mysql.ExecuteSQLInstruction($"INSERT INTO `users`(`login`, `password`, `ticket_id`, `region_id`, `last_enter_date`) VALUES ('{packet_data[2]}','{FromByteToString(res)}','{user_ticket_d}','0', '{DateTime.Now.ToShortDateString()}')").Result;
                    if (!creating_result)
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 0~0~ecu to user from - " + endpoint_address);
                        return $"0~0~ecu";
                    }
                    else
                    {
                        Console.WriteLine(DateTime.Now + $": new user created with login {packet_data[2]}, ticket {user_ticket_d} from {endpoint_address}");
                        return $"0~0~uc";
                    }

                }
                //========================================================================

                //logging existing user sending login and password   0~1~login~pass
                if (packet_data.Length == 4 && (packet_data[0] + packet_data[1]) == "01")
                {
                    if (!StringChecker(packet_data[2]) || !NumericsChecker(packet_data[3]))
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 0~1~wds to user from - " + endpoint_address);
                        return $"0~1~wds"; //wrong digits or signs                    
                    }

                    if (packet_data[2].Length < Starter.PASS_MIN_LENGHT || packet_data[2].Length > Starter.PASS_MAX_LENGHT)
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 0~1~wll to user from - " + endpoint_address);
                        return $"0~1~wll";
                    }


                    string[,] check_twin_name = Mysql.GetMysqlSelect($"SELECT `login`,`user_id` FROM `users` WHERE `login`= '{packet_data[2]}'").Result;

                    if (check_twin_name.GetLength(0) == 0)
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 0~1~ude to user from - " + endpoint_address);
                        return $"0~1~ude";
                    }

                    //checking password
                    string[,] check_pass = Mysql.GetMysqlSelect($"SELECT `password` FROM `users` WHERE `login`= '{packet_data[2]}'").Result;
                    if (check_pass[0, 0] != packet_data[3])
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 0~1~wp to user from - " + endpoint_address);
                        return $"0~1~wp";
                    }

                    //getting ticket
                    string user_ticket_d = Encryption.get_random_set_of_symb(10);
                    bool setting_ticket = Mysql.ExecuteSQLInstruction($"UPDATE `users` SET `ticket_id`='{user_ticket_d}', `last_enter_date`='{DateTime.Now}' WHERE `login`='{packet_data[2]}' ").Result;

                    if (!setting_ticket)
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 0~1~egt to user from - " + endpoint_address);
                        return $"0~1~egt";
                    }
                    else
                    {
                        //analytics entere game===========================
                        bool OKtest = Mysql.ExecuteSQLInstruction($"INSERT INTO `events`(`user_id`, `event_type_id`, `datetime`) VALUES ('{check_twin_name[0, 1]}','4','{DateTime.Now}')").Result;
                        //===================================

                        Console.WriteLine(DateTime.Now + ": new user logged in with login - " + packet_data[2] + " and ticket: " + user_ticket_d + " from " + endpoint_address);
                        return $"0~1~{user_ticket_d}";
                    }
                }
                //========================================================================

                //creating login and password for guest log
                if (packet_data.Length == 2 && (packet_data[0] + packet_data[1]) == "02")
                {
                    string user_ticket_d = Encryption.get_random_set_of_symb(10);
                    string login = Encryption.get_random_set_of_symb(10);
                    string password = Encryption.get_random_set_of_symb(10);
                    byte[] res = Encryption.GetHash384(password);

                    bool creating_result = Mysql.ExecuteSQLInstruction($"INSERT INTO `users`(`login`, `password`, `ticket_id`, `region_id`, `last_enter_date`) VALUES ('{login}','{FromByteToString(res)}','{user_ticket_d}','0', '{DateTime.Now.ToShortDateString()}')").Result;

                    if (creating_result)
                    {
                        Console.WriteLine(DateTime.Now + ": new user created as guest with login - " + login + ", ticket " + user_ticket_d + " from " + endpoint_address);
                        return $"0~2~{login}~{password}";
                    }
                    else
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 0~2~err to user from - " + endpoint_address);
                        return $"0~2~err";
                    }

                }

                //========================================================================

                //setting region 0~31~ticket~number of server~country code
                if (packet_data.Length == 5 && (packet_data[0] + packet_data[1]) == "031")
                {
                    if (!StringChecker(packet_data[2]) || !NumericsChecker(packet_data[3]) || !StringChecker(packet_data[4]))
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 0~31~wds to user from - " + endpoint_address);
                        return $"0~31~wds"; //wrong digits or signs                    
                    }

                    string[,] ID_check = Mysql.GetMysqlSelect($"SELECT `region_id` FROM `users` WHERE `ticket_id`='{packet_data[2]}'").Result;

                    if (ID_check.GetLength(0) == 0)
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 0~31~nst to user from - " + endpoint_address);
                        return $"0~31~nst";
                    }

                    bool result = Mysql.ExecuteSQLInstruction($"UPDATE `users` SET `region_id`='{packet_data[3]}',`country_code`='{packet_data[4]}' WHERE `ticket_id`='{packet_data[2]}'").Result;

                    if (result)
                    {
                        Console.WriteLine(DateTime.Now + $": OK region has been set to {packet_data[3]} for user - " + endpoint_address);
                        return $"0~31~ok";
                    }
                    else
                    {
                        Console.WriteLine(DateTime.Now + ": error setting new region ID for " + endpoint_address);
                        return $"0~31~err";
                    }
                }
                //=======================================================================

                //getting regionID 0~30~ticket
                if (packet_data.Length == 3 && (packet_data[0] + packet_data[1]) == "030")
                {
                    if (!StringChecker(packet_data[2]))
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 0~30~wds to user from - " + endpoint_address);
                        return $"0~30~wds"; //wrong digits or signs                    
                    }

                    string[,] result = Mysql.GetMysqlSelect($"SELECT `region_id` FROM `users` WHERE `ticket_id`='{packet_data[2]}'").Result;

                    if (result.GetLength(0) == 0)
                    {
                        Console.WriteLine(DateTime.Now + ": error trying to get regionID for " + endpoint_address);
                        return $"0~30~err";
                    }
                    else
                    {
                        Console.WriteLine(DateTime.Now + ": OK, regionID sent to user " + endpoint_address);
                        return $"0~30~{result[0, 0]}";
                    }
                }
                //=======================================================================


                //setting language0~32~ticket~data
                if (packet_data.Length == 4 && (packet_data[0] + packet_data[1]) == "032")
                {
                    if (!StringChecker(packet_data[2]) || !StringChecker(packet_data[3]))
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 0~30~wds to user from - " + endpoint_address);
                        return $"0~32~wds"; //wrong digits or signs                    
                    }

                    bool result = Mysql.ExecuteSQLInstruction($"UPDATE `users` SET `language`='{packet_data[3]}' WHERE `ticket_id`='{packet_data[2]}'").Result;


                    if (result)
                    {
                        Console.WriteLine(DateTime.Now + ": data about language added to " + endpoint_address);
                        return $"0~32~ok";
                    }
                    else
                    {
                        Console.WriteLine(DateTime.Now + ": error adding data about language to " + endpoint_address);
                        return $"0~32~err";
                    }
                }
                //=======================================================================

                //analytics = what char taken 0~12~ticket~char type
                if (packet_data.Length == 4 && (packet_data[0] + packet_data[1]) == "012")
                {
                    if (!StringChecker(packet_data[2]) || !NumericsChecker(packet_data[3]))
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 0~12~wds to user from - " + endpoint_address);
                        return $"0~12~wds"; //wrong digits or signs                    
                    }


                    string[,] userID = Mysql.GetMysqlSelect($"SELECT `user_id` FROM `users` WHERE `ticket_id`='{packet_data[2]}'").Result;

                    bool result = Mysql.ExecuteSQLInstruction($"INSERT INTO `events`(`user_id`, `event_type_id`, `datetime`, `data`) VALUES ('{userID[0, 0]}','5','{DateTime.Now}','{packet_data[3]}')").Result;


                    if (result)
                    {
                        Console.WriteLine(DateTime.Now + ": analytic data -what char taken- added to " + endpoint_address);
                        return $"0~12~ok";
                    }
                    else
                    {
                        Console.WriteLine(DateTime.Now + ": analytic data -what char taken- NOT added to " + endpoint_address);
                        return $"0~12~err";
                    }
                }
                //=======================================================================

                //working with tickets and character choosing
                if (packet_data.Length == 3 && (packet_data[0] + packet_data[1]) == "10")
                {
                    if (!StringChecker(packet_data[2]))
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 1~0~nst to user from - " + endpoint_address);
                        return $"1~0~nst";
                    }

                    //check if ticket exists
                    string[,] check_pass = Mysql.GetMysqlSelect($"SELECT `ticket_id` FROM `users` WHERE `ticket_id`= '{packet_data[2]}'").Result;

                    if (check_pass.GetLength(0) == 0)
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 1~0~nst to user from - " + endpoint_address);
                        return $"1~0~nst";
                    }

                    //get chars
                    string[,] get_chars = Mysql.GetMysqlSelect($"SELECT `character_name`,`character_type` FROM `characters`,`users` WHERE characters.user_id = users.user_id AND users.ticket_id='{packet_data[2]}' ").Result;
                    int how_many_chars = get_chars.GetLength(0);

                    if (how_many_chars == 0)
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 1~0~nc to user from - " + endpoint_address);
                        return $"1~0~nc";
                    }

                    string enum_result = "";

                    for (int i = 0; i < how_many_chars; i++)
                    {
                        enum_result = enum_result + get_chars[i, 0] + "~" + get_chars[i, 1] + "~";
                    }

                    Console.WriteLine(DateTime.Now + ": chars description send to - " + packet_data[2] + " - " + endpoint_address);
                    return $"1~0~{how_many_chars}~{enum_result}";

                }
                //=========================================================

                //creating new char 1~1~ticket~char name~ char type
                if (packet_data.Length == 5 && (packet_data[0] + packet_data[1]) == "11")
                {
                    if (!StringChecker(packet_data[2]))
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 1~1~nst to user from - " + endpoint_address);
                        return $"1~1~nst";
                    }

                    if (!StringChecker(packet_data[3]) || packet_data[3].Length < Starter.CHARNAME_MIN || packet_data[3].Length > Starter.CHARNAME_MAX)
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 1~1~wcn to user from - " + endpoint_address);
                        return $"1~1~wcn";
                    }

                    if (!NumericsChecker(packet_data[4]))
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 1~1~err to user from - " + endpoint_address);
                        return $"1~1~err";
                    }

                    //check char name
                    string[,] temp = Mysql.GetMysqlSelect($"SELECT `character_name` FROM `characters` WHERE `character_name`='{packet_data[3]}'").Result;
                    if (temp.GetLength(0) != 0)
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 1~1~cae to user from - " + endpoint_address);
                        return $"1~1~cae";
                    }

                    //check char type for allready exist
                    temp = Mysql.GetMysqlSelect($"SELECT `character_type` FROM `characters`,`users` WHERE characters.user_id = users.user_id AND users.ticket_id='{packet_data[2]}' AND characters.character_type = '{packet_data[4]}' ").Result;
                    if (temp.GetLength(0) != 0)
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 1~1~tae to user from - " + endpoint_address);
                        return $"1~1~tae";
                    }

                    //adding char
                    //get userID
                    temp = Mysql.GetMysqlSelect($"SELECT `user_id` FROM `users` WHERE ticket_id='{packet_data[2]}' ").Result;
                    string userID = temp[0, 0];
                    //add
                    bool res = Mysql.ExecuteSQLInstruction($"INSERT INTO `characters`(`user_id`, `character_type`, `character_name`) VALUES ('{userID}','{packet_data[4]}','{packet_data[3]}')").Result;

                    if (!res)
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 1~1~err to user from - " + endpoint_address);
                        return $"1~1~err";
                    }

                    //string [,] get_type_data = Mysql.GetMysqlSelect($"SELECT `speed`, `health`, `health_regen`, `energy_regen`, `weapon_attack`, `hit_power`, `armor`, `shield_block`, `magic_resistance`, `dodge`, `cast_speed`, `melee_crit`, `magic_crit`, `spell_power`, `spell1`, `spell2`, `spell3`, `spell4`, `spell5`, `spell6`, `spell_book`, `talents` FROM `character_types` WHERE `character_type`='{packet_data[4]}'  ").Result;
                    string[,] get_char_id = Mysql.GetMysqlSelect($"SELECT characters.character_id FROM characters WHERE characters.character_name='{packet_data[3]}' ").Result;

                    //Characters newChar = new Characters();
                    //string charData = newChar.getDefaultPlayerCharacteristicsInSQLReadyStringFormatINSERT(int.Parse(packet_data[4]));

                    //bool add_char_property = Mysql.ExecuteSQLInstruction($"INSERT INTO `character_property`(`character_id`, `speed`, `health`, `health_regen`, `energy_regen`, `weapon_attack`, `hit_power`, `armor`, `shield_block`, `magic_resistance`, `dodge`, `cast_speed`, `melee_crit`, `magic_crit`, `spell_power`, `spell1`, `spell2`, `spell3`, `spell4`, `spell5`, `spell6`, `hidden_conds`, `spell_book`, `talents`) VALUES('{get_char_id[0, 0]}', {charData}) ").Result;
                    bool add_user_to_raiting = Mysql.ExecuteSQLInstruction($"INSERT INTO `character_raiting`(`character_id`, `pvp_raiting`, `pve_raiting`, `xp_points`) VALUES ('{get_char_id[0, 0]}', 0, 0, 0)").Result;
                    
                    /*
                    if (add_user_to_raiting && add_char_property)
                    {
                        //analytics============
                        bool OKtest = Mysql.ExecuteSQLInstruction($"INSERT INTO `events`(`user_id`, `event_type_id`, `datetime`, `data`) VALUES ('{userID}','3','{DateTime.Now}', {int.Parse(packet_data[4])})").Result;
                        //=====================

                        Console.WriteLine(DateTime.Now + ": new character " + packet_data[3] + ":" + packet_data[4] + " created. Ticket " + packet_data[2] + " from " + endpoint_address);
                        return $"1~1~ok";
                    }
                    else
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 1~1~err to user from - " + endpoint_address);
                        return $"1~1~err";
                    }*/
                }



            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + data + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
                IncomingDataHadler.AddBadDataSupplier(IPEndPoint.Parse(endpoint_address));
            }

            return "0~0~err";

        }


        private static async void CleanTempSession(string index)
        {
            await Task.Delay(60000);
            if (TemporarySessionCreator.ContainsKey(index))
            {
                TemporarySessionCreator.Remove(index);
            }
        }

        public static bool StringChecker(string data_to_check)
        {
            if (data_to_check.All(char.IsLetterOrDigit))
            {
                return true;
            }

            return false;
        }

        public static bool NumericsChecker(string data_to_check)
        {
            if (data_to_check.All(char.IsDigit))
            {
                return true;
            }

            return false;
        }

        public static string FromByteToString(byte[] data)
        {
            StringBuilder d = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                d.Append(data[i]);
            }

            return d.ToString();
        }

    }
}
