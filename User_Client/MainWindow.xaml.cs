using _5.WPF_Client;
using Microsoft.Win32;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace User_Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public class UploadImage
    {
        public string Image { get; set; } = string.Empty;
    }

    public partial class MainWindow : Window
    {
        private string _serverUrl = "https://kukumber.itstep.click"; // сервер для збереження фото 
        private string _userImage = string.Empty;
        private ChatMessage _message = new ChatMessage(); // Структура користувача
        private TcpClient _tcpClient = new TcpClient(); // сам користувач - ми
        private NetworkStream _ns; // передача данних по мережі
        private Thread _thread;  // потік
        public MainWindow()
        {
            InitializeComponent();
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _message.Text = "Покинув чат";
            var buffer = _message.Serialize(); 
            _ns.Write(buffer); // відправляємо повідомлення на сервер 

            _tcpClient.Client.Shutdown(SocketShutdown.Both); // відключаємося від серверу
            _tcpClient.Close();
        }

        private void btnPhotoSelect_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog(); // знаходимо фото для аватарки
            dlg.ShowDialog();
            var filePath = dlg.FileName;
            var bytes = File.ReadAllBytes(filePath); // зчитуємо байти фото 
            var base64 = Convert.ToBase64String(bytes); // конвертиму байти до base64
            string json = JsonConvert.SerializeObject(new
            {
                photo = base64,
            }); // тут зберігаємо
            bytes = Encoding.UTF8.GetBytes(json);
            WebRequest request = WebRequest.Create($"{_serverUrl}/api/galleries/upload");  // створюємо запит на сервер для фото
            request.Method = "POST";
            request.ContentType = "application/json";
            using (var stream = request.GetRequestStream()) // робимо запит з настройками на сервер
            {
                stream.Write(bytes, 0, bytes.Length);
            }
            try
            {
                var response = request.GetResponse(); // отримуємо відповідь
                using (var stream = new StreamReader(response.GetResponseStream()))
                {
                    string data = stream.ReadToEnd(); // тут з сервера приймаємо і переробляємо в фото 
                    var resp = JsonConvert.DeserializeObject<UploadImage>(data);
                    MessageBox.Show(_serverUrl + resp.Image);
                    if (resp != null)
                        _userImage = resp.Image;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ViewMessage(string text, string imageUrl) // напишу тут - коли приходить нове повідомлення список прокручується в низ і користувач бачить повідомлення , це буде використовуватися в циклах і всі будуть бачити це повідомлення 
        {
            var grid = new Grid();
            for (int i = 0; i < 2; i++)
            {
                var colDef = new ColumnDefinition();
                colDef.Width = GridLength.Auto;
                grid.ColumnDefinitions.Add(colDef);
            }
            BitmapImage bmp = new BitmapImage(new Uri($"{_serverUrl}{imageUrl}"));
            var image = new Image();
            image.Source = bmp;
            image.Width = 50;
            image.Height = 50;

            var textBlock = new TextBlock();
            Grid.SetColumn(textBlock, 1);
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            textBlock.Margin = new Thickness(5, 0, 0, 0);
            textBlock.Text = text;
            grid.Children.Add(image);
            grid.Children.Add(textBlock);

            lbInfo.Items.Add(grid);
            lbInfo.Items.MoveCurrentToLast();
            lbInfo.ScrollIntoView(lbInfo.Items.CurrentItem);
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_userImage)) // чи користувач вибрав фото і нікнейм
            {
                MessageBox.Show("Оберіть фото для корристувача");
                return;
            }
            if (string.IsNullOrEmpty(txtUserName.Text))
            {
                MessageBox.Show("Вкажіть назву користувача");
                return;
            }
            try
            {
                IPAddress ip = IPAddress.Parse("26.114.83.34"); //  конектимося до сервера по ip або домену
                int port = 4512;
                _message.UserId = Guid.NewGuid().ToString(); //новий id для нашого користувача - це для того щоб відправити на сервер - він не повторюєтся
                _message.Name = txtUserName.Text; // певні данні
                _message.Photo = _userImage;
                _tcpClient.Connect(ip, port); // Підключаємося до сервера ip + port
                _ns = _tcpClient.GetStream(); // Отримаємо мережу сервера  - Щоб щось йому писати і він Міг нам що писати - наш ip адрес 
                _thread = new Thread(obj => ResponseData((TcpClient)obj)); // запускаємо анонімний потік для того щоб коли люди будть щось писати і ми могли бачити
                _thread.Start(_tcpClient); // старт потоку
                btnSend.IsEnabled = true; // насторойки UI
                btnConnect.IsEnabled = false;
                txtUserName.IsEnabled = false;
                _message.Text = "Приєднався до чату";
                var buffer = _message.Serialize();
                _ns.Write(buffer); // пишемо серверу що Ми приїдналися 

            }
            catch (Exception ex)
            {
                MessageBox.Show("Проблема підключення до серверу " + ex.Message);
            }
        }
        private void ResponseData(TcpClient client)
        {
            NetworkStream ns = client.GetStream(); // отримуємо користува 
            byte[] readBytes = new byte[16054400];
            int byte_count;
            while ((byte_count = ns.Read(readBytes)) > 0) 
            {
                Dispatcher.BeginInvoke(new Action(() => // Щоб ми могли бачитися що надсилає нам сервер , коли інша людина щось написала то ми це побачили 
                {
                    ChatMessage msg = ChatMessage.Desserialize(readBytes);
                    string text = $"{msg.Name} -> {msg.Text}";
                    ViewMessage(text, msg.Photo);

                }));
            }
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            _message.Text = txtText.Text;
            var buffer = _message.Serialize();
            _ns.Write(buffer); // пишемо нашому серверу повідомлення - ну пишемо в чат де є люди

            txtText.Text = "";
        }

    }
}