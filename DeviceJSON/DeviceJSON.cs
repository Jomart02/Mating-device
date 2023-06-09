﻿using System.Net;
using System.Net.Sockets;
using System.Text;

// Клиент 1
namespace Sensor {
    class DeviceJSON {
        static async Task Main(string[] args) {


            using var UDPClient = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            //Адресс данного устройства - внести в JSON
            var localIP = new IPEndPoint(IPAddress.Parse("127.0.0.2"), 5002);
            SocketFlags SF = new SocketFlags();

            // начинаем прослушивание входящих сообщений
            UDPClient.Bind(localIP);
            Console.WriteLine("Клиент запущен...");

            // буфер для получаемых данных
            byte[] datares = new byte[1024];

            //Отправляемые данные - должны меняться 
            //string nmeaString = "$GPGLL,4916.45,N,123117777777777.12,W,225444,A,*31";
            //string nmeaString = "$CUSPP,PORT,*31";

            // Преобразуем строку в байты
            byte[] sendBuffer = File.ReadAllBytes(@"C:\2022-2023\EthernetNavigationSystem\DeviceJSON\Config1.json");
           // string JSONASCII = File.ReadAllText(@"C:\2022-2023\EthernetNavigationSystem\DeviceJSON\Config1.json");
           // var sendBuffer = Encoding.ASCII.GetBytes(nmeaString);

            //Адресс оконечного устройства для отправки - добавить в JSON
            EndPoint remotePoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5001);

            while (true) {
                try {
                    int bytes = await UDPClient.SendToAsync(sendBuffer, remotePoint);

                    
                    var result = await UDPClient.ReceiveFromAsync(datares, SocketFlags.None, remotePoint);
                    var Message = Encoding.ASCII.GetString(datares, 0, result.ReceivedBytes);
                    Console.WriteLine(Message);
                    Console.WriteLine($"Отправлено {bytes} байт file");


                    Thread.Sleep(1000);

                } catch(Exception ex) { }
            }



        }


    }
}