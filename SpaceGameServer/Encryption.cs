using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;


namespace SpaceGameServer
{
    public class Encryption : IDisposable
    {
        private RSAParameters privateKey, publicKey;
        public string publicKeyInString;
        private RSACryptoServiceProvider csp;

        public Encryption()
        {

            //create keys
            csp = new RSACryptoServiceProvider(2048);
            privateKey = csp.ExportParameters(true);
            publicKey = csp.ExportParameters(false);

            //conver key into string
            var sw = new System.IO.StringWriter();
            var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
            xs.Serialize(sw, publicKey);
            publicKeyInString = sw.ToString();

        }

        public byte[] GetSecretKey(string encoded_bytes)
        {
            //getting back real public key by public key string
            var sr = new System.IO.StringReader(encoded_bytes);
            var xs = new System.Xml.Serialization.XmlSerializer(typeof(byte[]));
            byte[] preres = (byte[])xs.Deserialize(sr);

            byte[] res = csp.Decrypt(preres, false);
            //Console.WriteLine(DateTime.Now + ": " + "secret key is generated;");
            return res;
        }


        public void Dispose()
        {
            csp.Dispose();
        }

        public byte[] GetByteArrFromCharByChar(string key_in_string)
        {
            List<byte> result = new List<byte>();

            for (int i = 0; i < key_in_string.Length; i++)
            {
                result.Add(Byte.Parse(key_in_string.Substring(i, 1)));
            }

            return result.ToArray();
        }

        public static void Encode(ref byte[] source, byte[] key)
        {
            int index = 0;

            for (int i = 6; i < source.Length; i++)
            {
                source[i] = (byte)(source[i] + key[index]);

                if ((index + 1) == key.Length)
                {
                    index = 0;
                }
                else
                {
                    index++;
                }
            }
        }

        public static void Decode(ref byte[] source, byte[] key)
        {
            int index = 0;

            for (int i = 6; i < source.Length; i++)
            {
                source[i] = (byte)(source[i] - key[index]);

                if ((index + 1) == key.Length)
                {
                    index = 0;
                }
                else
                {
                    index++;
                }
            }


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

        public static byte[] GetHash384(string data)
        {
            SHA384 create_hash = SHA384.Create();
            return create_hash.ComputeHash(Encoding.UTF8.GetBytes(data));
        }

        public static string get_random_set_of_symb(int nub_of_symb)
        {
            string[] arr_name = { "q", "w", "e", "r", "t", "y", "u", "i", "o", "p", "a", "s", "d", "f", "g", "h", "j", "k", "l", "z", "x", "c", "v", "b", "n", "m", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "A", "S", "D", "F", "G", "H", "J", "K", "L", "Z", "X", "C", "V", "B", "N", "M", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
            string result = "";
            Random rnd = new Random();
            for (int i = 0; i < nub_of_symb; i++)
            {
                result = result + arr_name[rnd.Next(0, arr_name.Length - 1)];
            }

            return result;
        }

    }
}
