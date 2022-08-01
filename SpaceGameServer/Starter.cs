using System;
using System.Text;
using System.Threading;


namespace SpaceGameServer
{
    class Starter
    {
        public const int TICKi = 100;
        public const float TICKf = 0.1f;

        public const int PASS_MIN_LENGHT = 8;
        public const int PASS_MAX_LENGHT = 16;
        public const int CHARNAME_MIN = 6;
        public const int CHARNAME_MAX = 16;
                
        public static string MysqlConnectionData_login;
        public readonly static string address_for_data_config = @"C:\android\space.config"; //@"C:\android\data"  @"/home/admin/space.config"
        

        static void Main(string[] args)
        {
            Data_config.Init_data_config();
            Thread.Sleep(2000);

            Server.Server_init();
        
            

            Console.WriteLine("ready to exit...");
            Console.ReadKey();
            return;
        }
    }
}
