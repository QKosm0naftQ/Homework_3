using System.Net.Sockets;
using System.Net;
using System.Text;

namespace Server
{
    class Program
    {

        static readonly object _lock = new object();
        static readonly Dictionary<int, TcpClient> list_clients = new Dictionary<int, TcpClient>();
        static async Task Main(string[] args)
        {
            int countClient = 1;

            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
            var hostName = Dns.GetHostName();
            Console.WriteLine($"Мій хост {hostName}");
            var localhost = await Dns.GetHostEntryAsync(hostName);

            int count = localhost.AddressList.Length;
            var ip = localhost.AddressList[count - 1];
            int i = 0;
            Console.WriteLine("Вкажіть IP адресу:");
            foreach (var item in localhost.AddressList)
            {
                Console.WriteLine($"{++i}.{item}");
            }
            Console.Write($"({ip})->_");
            var temp = Console.ReadLine();
            //if (!string.IsNullOrEmpty(temp))
                ip = IPAddress.Parse(temp);
            int port = 4512;
            Console.Title = $"Ваш IP {ip}:{port} :)";
            TcpListener serverSocket = new TcpListener(ip, port);
            serverSocket.Start();
            Console.WriteLine($"Run Server {ip}:{port}");

            while (true)
            {
                TcpClient client = serverSocket.AcceptTcpClient();
                lock (_lock)
                {
                    list_clients.Add(countClient, client);
                }
                Console.WriteLine($"На сервер додався клієнте {client.Client.RemoteEndPoint}");
                Thread t = new Thread(handle_clients);
                t.Start(countClient); //
                countClient++;
            }
        }
        public static void handle_clients(object c)
        {
            int id = (int)c;
            TcpClient client;
            lock (_lock)
            {
                client = list_clients[id];
            }
            try
            {
                while (true)
                {
                    NetworkStream strm = client.GetStream();
                    byte[] buffer = new byte[10240];
                    int byte_count = strm.Read(buffer);
                    if (byte_count == 0)
                        break;
                    string data = Encoding.UTF8.GetString(buffer, 0, byte_count);
                    Console.WriteLine($"Client message {data}");
                    brodcast(data);
                }
            }
            catch { }

            lock (_lock)
            {
                Console.WriteLine($"Чат покинув клієнт {client.Client.RemoteEndPoint}");
                list_clients.Remove(id);
            }
            client.Client.Shutdown(SocketShutdown.Both);
            client.Close();
        }
        public static void brodcast(string data)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(data);
            lock (_lock)
            {
                try
                {
                    foreach (var c in list_clients.Values)
                    {
                        NetworkStream stream = c.GetStream();
                        stream.Write(buffer); 
                    }
                }
                catch { }
            }
        }
    }
}
