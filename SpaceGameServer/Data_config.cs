using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;


namespace SpaceGameServer
{
    class Data_config
    {
        public static async void Init_data_config()
        {
            using (FileStream fs = new FileStream(Starter.address_for_data_config, FileMode.OpenOrCreate))
            {
                data_config_json Data = await JsonSerializer.DeserializeAsync<data_config_json>(fs);                
                Starter.MysqlConnectionData_login = Data.mysql_server_data;
            }
        }

        public static byte[] GetByteArrFromStringComma(string key_in_string)
        {
            List<byte> result = new List<byte>();

            string[] _data = key_in_string.Split(',');

            for (int i = 0; i < _data.Length; i++)
            {
                result.Add(Byte.Parse(_data[i]));
            }

            return result.ToArray();
        }



        struct data_config_json
        {
            public string mysql_server_data { get; set; }            
        }

    }
}
