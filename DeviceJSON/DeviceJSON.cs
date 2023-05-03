using System.Net;
using System.Net.Sockets;
using System.Text;

// Клиент 1
namespace Sensor {
    class DeviceJSON {
        static void Main(string[] args) {


            using var UDPClient = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            //Адресс данного устройства - внести в JSON
            var localIP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5021);
            SocketFlags SF = new SocketFlags();

            // начинаем прослушивание входящих сообщений
            UDPClient.Bind(localIP);
            Console.WriteLine("Клиент запущен...");

            // буфер для получаемых данных
            byte[] datares = new byte[1024];

            //Отправляемые данные - должны меняться 
            //string nmeaString = "$GPGLL,4916.45,N,12311.12,W,225444,A,*31";
            //string nmeaString = "$CUSPP,PORT,*31";

            // Преобразуем строку в байты
            //byte[] sendBuffer = File.ReadAllBytes(@"C:\2022-2023\EthernetNavigationSystem\DeviceJSON\Config1.json");
            string JSONASCII = File.ReadAllText(@"C:\2022-2023\EthernetNavigationSystem\DeviceJSON\Config1.json");
            var sendBuffer = Encoding.ASCII.GetBytes(JSONASCII);

            //Адресс оконечного устройства для отправки - добавить в JSON
            EndPoint remotePoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5001);

            while (true) {

                int bytes = UDPClient.SendTo(sendBuffer, remotePoint);

                Console.WriteLine($"Отправлено {bytes} байт file");

                Thread.Sleep(1000);
            }



        }


    }
}