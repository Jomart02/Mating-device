using System.Net.Sockets;
using System.Net;
using System.Text;





namespace Sensor {
    class SensorOne {
        static void Main(string[] args) {

            

            using var UDPClient = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            //Адресс данного устройства - внести в JSON
            var localIP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5002);
            SocketFlags SF = new SocketFlags();

            // начинаем прослушивание входящих сообщений
            UDPClient.Bind(localIP);
            Console.WriteLine("Клиент запущен...");

            // буфер для получаемых данных
            byte[] datares = new byte[2048];

            //Адресс оконечного устройства для отправки - добавить в JSON
            EndPoint remotePoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5001);

            

            while (true) {
               
                //Получение данных через сокет 
                UDPClient.ReceiveFrom(datares, ref remotePoint);

                string NMEAmes = Encoding.ASCII.GetString(datares);
                Console.WriteLine(NMEAmes);


            }
        }
    }
}