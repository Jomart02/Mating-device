using System.Net;
using System.Net.Sockets;
using System.Text;
using ProtokolLibrary;
using System.Diagnostics;
using System.IO.Ports;
//using static System.Runtime.InteropServices.JavaScript.JSType;


namespace NavigationSystem {

    class Controller {

        //Стандартные параметры клиента контролерра 

        //Инфрмация об удаленных портах 
        private static IPAddress REMOTE_IP_ADDRESS = IPAddress.Parse("127.0.0.1");
        private static int REMOTE_PORT;

        //Порт контроллера
        private const int localPort = 5001;

        //Обьявление клиента 
        private static Socket UDP_CONTROLLER = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private static SocketFlags SF = new SocketFlags();
        private static IPEndPoint LOCAL_IP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), localPort);

        private static string PROTOCOL_MESSAGE = "$MDRND,000000.00,000000,0000.0000,N,00000.0000,E,000.0,N,00.0,K,000.0*73"; //-стандартное сообщение 
        private static string LAST_NMEA_MESSAGE = "====";
        private static string LAST_NMEA_DATA = "";
        private static ProtokolMessage MESSAGE = new ProtokolMessage();
		
		//Объявление клиента - интерфейса 
		IPEndPoint END_POINT_INTERFACE = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5006);
        private Dictionary<string, int> PORT_SENSOR = new Dictionary<string, int>() {
           
            { "GGL" , 5005 },
            { "GGA" , 5006 },
            { "RMC" , 5007 },
            { "VTG" , 5008 },
            { "ZDA" , 5009 }

        };

        private Dictionary<string, int> PORT_DEVICE = new Dictionary<string, int>() {
            { "DEV1" , 5011 },
			{ "DEV2" , 5006 },
        };

        private Dictionary<string, string> COMPORT_DEVICE = new Dictionary<string, string>() {
            { "DEV1" , "COM11" },
            { "DEV2" , "COM12" },
        };

        static async Task Main(string[] args) {

            try {

                //Process.Start(@"C:\2022-2023\EthernetNavigationSystem\SensorOne\SensorOne.bat");
                //Process.Start(@"C:/2022-2023/EthernetNavigationSystem/SensorTwo/bin/Debug/net7.0/SensorTwo.exe");

                Controller DiplomDevice = new Controller();

                UDP_CONTROLLER.Bind(LOCAL_IP);

              

                //Поток для прослушивания Com портов
                /*Thread COMReceive = new Thread(DiplomDevice.ReceiverRS);
                COMReceive.Start();
*/
                //Асинхронный поток для прослушивания Ethernet 
                Task.Run(() => DiplomDevice.ReceiverEthernetAsync());
                //Асинхронный поток для отправки сообщений Ethernet 
                Task.Run(() => DiplomDevice.SendToInterfaceAsync());



                while (true) {

                    foreach (var port in DiplomDevice.PORT_DEVICE.Values){
                         //REMOTE_PORT = port;
                         DiplomDevice.SendToReceiversAsync(port);
                    }


                    foreach (var comport in DiplomDevice.COMPORT_DEVICE.Values) {
                        DiplomDevice.ReceiverRSAsync(comport);
                    }
					
                    //Thread.Sleep(10);
                }

            } catch(Exception e) { }


        }

        protected async Task SendToReceiversAsync(int port) {

            // Создаем endPoint по информации об удаленном хосте девайсов
            IPEndPoint endPoint = new IPEndPoint(REMOTE_IP_ADDRESS, port);

            //Широковещательная рассылка 
            /* UDP_CONTROLLER.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, 27000);*/
            
            try {

                byte[] ByteProtocolMessage = Encoding.ASCII.GetBytes(PROTOCOL_MESSAGE);
        	
				//Отправка на нужные источники и устройства 
                await UDP_CONTROLLER.SendToAsync(ByteProtocolMessage, endPoint);
				
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine("Отправлено: " + PROTOCOL_MESSAGE);				

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
                    byte[] ByteNMEAMessage = Encoding.ASCII.GetBytes(LAST_NMEA_MESSAGE + " " + DateTime.Now);

                    //отправка на интерфейс постоянно 
                    int bytes = await UDP_CONTROLLER.SendToAsync(ByteProtocolMessage, END_POINT_INTERFACE);
                    int bytes1 = await UDP_CONTROLLER.SendToAsync(ByteNMEAMessage, END_POINT_INTERFACE);

                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine("Отправлено на интерфейс: " + PROTOCOL_MESSAGE + " " + bytes + "  |||  " + LAST_NMEA_MESSAGE + " " + DateTime.Now + " " + bytes1);
                    Task.Delay(100);


                } catch (ArgumentOutOfRangeException ex) {
                    Console.WriteLine("Возникло исключение: " + ex.ToString() + "\n  " + ex.Message);
                } catch (Exception ex) {
                    Console.WriteLine("Возникло исключение: " + ex.ToString() + "\n  " + ex.Message);
                }
            }
        }

        protected void Receiver() {


            byte[] ReceiveBytes = new byte[512];

            //Получаем пришедшие IP с прослушивания 
            EndPoint RemoteIpEndPoint = (EndPoint)LOCAL_IP;
            

            Console.WriteLine("\n-----------Получение сообщений-----------");
            while (true) {
                try {

                    int size = UDP_CONTROLLER.ReceiveFrom(ReceiveBytes, ref RemoteIpEndPoint);
                    //Выделение памяти для принятого сообщения
                    byte[] DataBytes = new byte[size];
                    for (int i = 0; i < size; i++) DataBytes[i] = ReceiveBytes[i]; 
                    
                    string Message = Encoding.ASCII.GetString(DataBytes);
					
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("Принято: " + Message);
				
                    LAST_NMEA_MESSAGE= Message;
                    PROTOCOL_MESSAGE = MESSAGE.GetMessage(Message);
	
                } catch (Exception ex) {}

            }
        }

        protected async void ReceiverRSAsync(string port) {

           
                try {

                    var isValid = SerialPort.GetPortNames().Any(x => string.Compare(x, port, true) == 0);
                    if (!isValid)
                        throw new System.IO.IOException(string.Format("{0} port was not found", port));
                    else {
                        SerialPort comport = new SerialPort(port);
                        comport.Open();
                        await Task.Delay(10);
                        // Читаем данные из каждого порта
                        string data1 = comport.ReadLine();
                        Console.WriteLine(data1);
                        PROTOCOL_MESSAGE = MESSAGE.GetMessage(data1);
                        LAST_NMEA_MESSAGE = data1;
                        comport.Close();
                    }

                } catch (Exception ex) { Console.WriteLine(ex.Message); }
            
        }

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

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Принято: ================================================================================================================================================================ " + Message);

                    LAST_NMEA_MESSAGE = Message;
                    PROTOCOL_MESSAGE = MESSAGE.GetMessage(Message);

                } catch (Exception ex) { }

            }
        }

    }

}

