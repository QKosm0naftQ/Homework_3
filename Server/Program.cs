using System.Net.Sockets;
using System.Net;
using System.Text;

namespace Server
{
    class Program
    {

        static readonly object _lock = new object(); // для зручного користування передачі данних в потоках
        static readonly Dictionary<int, TcpClient> list_clients = new Dictionary<int, TcpClient>(); // Для збереження людини і її номер 
        static async Task Main(string[] args)
        {
            int countClient = 1;

            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
            var hostName = Dns.GetHostName();
            Console.WriteLine($"Мій хост {hostName}");
            var localhost = await Dns.GetHostEntryAsync(hostName); // дістаємо ip адреса сервера

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
            TcpListener serverSocket = new TcpListener(ip, port); // створюємо сервер - ip+port
            serverSocket.Start();// запуск сервера
            Console.WriteLine($"Run Server {ip}:{port}");

            while (true)
            {
                TcpClient client = serverSocket.AcceptTcpClient(); // очікуємо конекту клієнта 
                lock (_lock)
                {
                    list_clients.Add(countClient, client); // коли є контакт додаємо його в список
                }
                Console.WriteLine($"На сервер додався клієнте {client.Client.RemoteEndPoint}");
                Thread t = new Thread(handle_clients); // відкриваємо потік для клієнта - тобто розмову з ним 
                t.Start(countClient); // запускаємо розмову
                countClient++;
            }
        }
        public static void handle_clients(object c)
        {
            int id = (int)c;
            TcpClient client; // створюємо клієнта 
            lock (_lock)
            {
                client = list_clients[id]; // передаємо його з списку по id 
            }
            try
            {
                while (true) // поки є клієнт цикл нескінченний 
                {
                    NetworkStream strm = client.GetStream(); // отримуємо все що надсилає кліент 
                    byte[] buffer = new byte[10240]; 
                    int byte_count = strm.Read(buffer); // зчитуємо посилання
                    if (byte_count == 0) // якщо нічого то клієнт вийшов з розмови
                        break;
                    string data = Encoding.UTF8.GetString(buffer, 0, byte_count); 
                    Console.WriteLine($"Client message {data}");
                    brodcast(data); // пересилаємо повідомлення всі хто є на сервері
                }
            }
            catch { }

            lock (_lock)
            {
                Console.WriteLine($"Чат покинув клієнт {client.Client.RemoteEndPoint}");
                list_clients.Remove(id); 
            }
            client.Client.Shutdown(SocketShutdown.Both); // виганяємо клієнта з сервера
            client.Close(); // відключаємо його повністю
        }
        public static void brodcast(string data)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(data);
            lock (_lock) // щоб не було проблем використовуємо лок
            {
                try
                {
                    foreach (var c in list_clients.Values) // отримуємо всіх клієнтів і пишемо до всіх повідомлення
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
