using System.Net;
using System.Net.Sockets;
using System.Text;
using ProtokolLibrary;
using System.Diagnostics;
using System.IO.Ports;

namespace NavigationSystem {

    class Controller {

        //Стандартные параметры клиента контролерра 

        //Инфрмация об удаленных портах 
        private static IPAddress REMOTE_IP_ADDRESS = IPAddress.Parse("127.0.0.1");
        
        //Порт контроллера
        private const int localPort = 5001;

        //Обьявление клиента 
        private static Socket UDP_CONTROLLER = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        //private static SocketFlags SF = new SocketFlags();
        private static IPEndPoint LOCAL_IP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), localPort);

        private static string PROTOCOL_MESSAGE = "$MDRND,000000.00,000000,0000.0000,N,00000.0000,E,000.0,N,00.0,K,000.0*73"; //-стандартное сообщение 
        private static string LAST_ETHERNET_MESSAGE = "====";
        private static string LAST_RS_MESSAGE = "====";
        
        private static ProtokolMessage MESSAGE = new ProtokolMessage();
		
		//Объявление клиента - интерфейса 
		IPEndPoint END_POINT_INTERFACE = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5006);

        private static Dictionary<string, int> PORT_SENSOR = new Dictionary<string, int>() {
           
            { "GGL" , 5005 },
            { "GGA" , 5006 },
            { "RMC" , 5007 },
            { "VTG" , 5008 },
            { "ZDA" , 5009 }

        };

        private static Dictionary<string, int> PORT_DEVICE = new Dictionary<string, int>() {

            { "DEV1" , 5011 },
			{ "DEV2" , 5012 },
            { "DEV3" , 5013 },
            { "DEV4" , 5014 },
            { "DEV5" , 5015 }
        };

        private static Dictionary<string, string> COMPORT_DEVICE_OUTPUT = new Dictionary<string, string>() {
            { "COMDEV1" , "COM11" },
            { "COMDEV2" , "COM12" },
            { "COMDEV3" , "COM13" },
            { "COMDEV4" , "COM14" },
            { "COMDEV5" , "COM15" }
        };

        private static Dictionary<string, string> COMPORT_DEVICE_INPUT = new Dictionary<string, string>() {
            { "COMDEV6" ,   "COM16" },
            { "COMDEV7" ,   "COM17" },
            { "COMDEV8" ,   "COM18" },
            { "COMDEV9" ,   "COM19" },
            { "COMDEV10" ,  "COM20" }
        };

        static int SLEEP_SEND = 100;
        static int SLEEP_SEND_INTERFACE = 100;
        static int SLEEP_RS = 100;


        static async Task Main(string[] args) {

            try {

                //Process.Start(@"C:\2022-2023\EthernetNavigationSystem\SensorOne\SensorOne.bat");
                //Process.Start(@"C:/2022-2023/EthernetNavigationSystem/SensorTwo/bin/Debug/net7.0/SensorTwo.exe");

                Controller DiplomDevice = new Controller();

                UDP_CONTROLLER.Bind(LOCAL_IP);

                //Поток для прослушивания Com портов
                Thread COMReceive = new Thread(DiplomDevice.ReceiverRS);
                COMReceive.Start();
                //Поток для отправки сообщений Com портам
                Thread COMSend = new Thread(DiplomDevice.SendToRS);
                COMSend.Start();

                //Асинхронный поток для прослушивания Ethernet 
                Task.Run(() => DiplomDevice.ReceiverEthernetAsync());
                //Асинхронный поток для отправки сообщений Ethernet  на интерфейс
                Task.Run(() => DiplomDevice.SendToInterfaceAsync());



                while (true) {

                    foreach (var port in PORT_DEVICE.Values){
                         //REMOTE_PORT = port;
                         await DiplomDevice.SendToReceiversAsync(port);
                        
                    }

                    foreach (var port in PORT_SENSOR.Values) {
                        //REMOTE_PORT = port;
                        await DiplomDevice.SendToReceiversAsync(port);
                    }

                   /* foreach (var comport in COMPORT_DEVICE.Values) {
                        DiplomDevice.ReceiverRSAsync(comport);

                    }*/
					
                    Thread.Sleep(SLEEP_SEND);
                }

            } catch(Exception e) { }
        }

        #region Модуль отправки сообщений по Ethernet 
        protected async Task SendToReceiversAsync(int port) {

            // Создаем endPoint по информации об удаленном хосте девайсов
            IPEndPoint endPoint = new IPEndPoint(REMOTE_IP_ADDRESS, port);

            //Широковещательная рассылка 
            /* UDP_CONTROLLER.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, 27000);*/
            
            try {

                byte[] ByteProtocolMessage = Encoding.ASCII.GetBytes(PROTOCOL_MESSAGE);
                byte[] ByteEthernetMessage = Encoding.ASCII.GetBytes(LAST_ETHERNET_MESSAGE);
                byte[] ByteRSMessage = Encoding.ASCII.GetBytes(LAST_RS_MESSAGE);

                //Отправка на нужные источники и устройства 
                await UDP_CONTROLLER.SendToAsync(ByteProtocolMessage, endPoint);
                await UDP_CONTROLLER.SendToAsync(ByteEthernetMessage, endPoint);
                await UDP_CONTROLLER.SendToAsync(ByteRSMessage, endPoint);

                Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine("Отправлено: " + PROTOCOL_MESSAGE );
               // Thread.Sleep(SLEEP_SEND);

            } catch (ArgumentOutOfRangeException ex) {
                Console.WriteLine("Возникло исключение: " + ex.ToString() + "\n  " + ex.Message);
            } catch (Exception ex) {
                Console.WriteLine("Возникло исключение: " + ex.ToString() + "\n  " + ex.Message);
            }
        }
		
		
		protected async Task SendToInterfaceAsync() {

            //Широковещательная рассылка 
            /* UDP_CONTROLLER.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, 27000);*/
            while (true) {
                try {

                    byte[] ByteProtocolMessage = Encoding.ASCII.GetBytes(PROTOCOL_MESSAGE);
                    byte[] ByteEthernetMessage = Encoding.ASCII.GetBytes(LAST_ETHERNET_MESSAGE + " |Time send " + DateTime.Now);
                    byte[] ByteRSMessage = Encoding.ASCII.GetBytes(LAST_RS_MESSAGE + " |Time send " + DateTime.Now);

                    //отправка на интерфейс постоянно 
                    int bytes = await UDP_CONTROLLER.SendToAsync(ByteProtocolMessage, END_POINT_INTERFACE);
                    int bytes1 = await UDP_CONTROLLER.SendToAsync(ByteEthernetMessage, END_POINT_INTERFACE);
                    int bytes2 = await UDP_CONTROLLER.SendToAsync(ByteRSMessage, END_POINT_INTERFACE);


                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine("Отправлено на интерфейс: " + PROTOCOL_MESSAGE + " " + bytes + "  |||  " + LAST_ETHERNET_MESSAGE + " " + DateTime.Now + " " + bytes1);
                    Thread.Sleep(SLEEP_SEND_INTERFACE);

                } catch (ArgumentOutOfRangeException ex) {
                    Console.WriteLine("Возникло исключение: " + ex.ToString() + "\n  " + ex.Message);
                } catch (Exception ex) {
                    Console.WriteLine("Возникло исключение: " + ex.ToString() + "\n  " + ex.Message);
                }
            }
        }
        #endregion

        #region Модуль отправки и приема сообщений через COM порт

        protected void ReceiverRS() {

            while (true) {

                //Счетчик по каждому COM - порту
                foreach (var port in COMPORT_DEVICE_OUTPUT.Values) {

                    try {
                        //Проверка работоспособности выбранного порта
                        var isValid = SerialPort.GetPortNames().Any(x => string.Compare(x, port, true) == 0);
                        if (!isValid)
                            throw new System.IO.IOException(string.Format("{0} port was not found", port));//Информация - что порт закрыт
                        else {
                            SerialPort comport = new SerialPort(port);
                            comport.Open();
                            
                            // Читаем данные из открытого порта 
                            string data1 = comport.ReadLine();
                            Console.WriteLine(data1);
                            //Формируем информационное сообщение и сохраняем пришедшие данные 
                            PROTOCOL_MESSAGE = MESSAGE.GetMessage(data1);
                            LAST_RS_MESSAGE = data1;
                            //закрываем порт для работы без ошибок 
                            comport.Close();
                        }

                    } catch (Exception ex) { Console.WriteLine(ex.Message); }

                }
                //Задержка на чтение и отправку 
                Thread.Sleep(SLEEP_RS);
            }
        }

        protected void SendToRS() {

            while (true) {

                //Счетчик по каждому COM - порту
                foreach (var port in COMPORT_DEVICE_INPUT.Values) {

                    try {


                        //Проверка работоспособности выбранного порта
                        var isValid = SerialPort.GetPortNames().Any(x => string.Compare(x, port, true) == 0);
                        if (!isValid)
                            throw new System.IO.IOException(string.Format("{0} port was not found", port));//Информация - что порт закрыт
                        else {
                            SerialPort comport = new SerialPort(port);
                            comport.Open();

                            //передаем данные оконечномоу устройству 
                            comport.WriteLine(PROTOCOL_MESSAGE);
                            comport.WriteLine(LAST_ETHERNET_MESSAGE);
                            comport.WriteLine(LAST_RS_MESSAGE);

                            //закрываем порт для работы без ошибок 
                            comport.Close();
                        }

                    } catch (Exception ex) { Console.WriteLine(ex.Message); }

                }
                //Задержка на чтение и отправку 
                Thread.Sleep(SLEEP_RS);
            }
        }

        #endregion

        #region Модуль приема сообщений по Ethernet 

        protected async Task ReceiverEthernetAsync() {


            byte[] ReceiveBytes = new byte[512];
            SocketFlags SF = new SocketFlags();
            //Получаем пришедшие IP с прослушивания 
            EndPoint RemoteIpEndPoint = (EndPoint)LOCAL_IP;


            Console.WriteLine("\n-----------Получение сообщений-----------");
            while (true) {
                try {

                    var result = await UDP_CONTROLLER.ReceiveFromAsync(ReceiveBytes, SocketFlags.None , RemoteIpEndPoint);
                    var Message = Encoding.ASCII.GetString(ReceiveBytes, 0, result.ReceivedBytes);


                    if (Message.Contains("CUSPP")) {
                        CheckCommand(Message);
                        continue;
                    }
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Принято:  " + Message);

                    LAST_ETHERNET_MESSAGE = Message;
                    PROTOCOL_MESSAGE = MESSAGE.GetMessage(Message);

                } catch (Exception ex) { }

            }
        }

        #endregion

        #region Модуль проверки и настройки контроллера 

        protected void CheckCommand(string CommandMessage) {


            string data = FormattingDate(CommandMessage);//Получение только данных из сообщения

            List<string> data_mas = new List<string>(data.Split(','));//Получаю массив данных 

            for(int i = 0;i<data_mas.Count;i++) {
                
                switch (data_mas[i]) {
                    case "FREQ":
                        SLEEP_SEND = Convert.ToInt32( data_mas[i + 1] );
                    break;

                }
            }
        }


        protected string FormattingDate(string NMEAmes) {


            int startsum = (NMEAmes.IndexOf('$') + 7);
            int length = (NMEAmes.Length - 10);

            NMEAmes = NMEAmes.Substring(startsum, length);

            return NMEAmes;
        }

        #endregion

    }

}

