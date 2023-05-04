using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ProtokolLibrary;
using System.IO.Ports;
using Newtonsoft.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Newtonsoft.Json.Linq;
using System.Text.Json.Nodes;

namespace NavigationSystem {
    internal class Controller {
        //Стандартные параметры клиента контролерра 
        //Локальная сеть ИУС
        public string CODE = "JSON";
        //Локальная сеть ИУС на которой контроллер
        public string CONTROLLER_IP_ADDRESS { get; set; }
        //Порт контроллера
        public int CONTROLLER_PORT { get; set; }
        // 
        public string IP_ADDRESS_INTERFACE { get; set; }
        //Порт интерфейса
        public int INTERFACE_PORT { get; set; }

        public string IP_ADDRESS_SENSOR  { get; set; }
        public Dictionary<string, object[]> IP_PORT_SENSOR = new Dictionary<string, object[]>();

        public string IP_ADDRESS_DEVICE { get; set; }
        public Dictionary<string, object[]> IP_PORT_DEVICE = new Dictionary<string, object[]>();

        public Dictionary<string, string> COMPORT_DEVICE_OUTPUT = new Dictionary<string, string>();
        public Dictionary<string, string> COMPORT_DEVICE_INPUT = new Dictionary<string, string>();

        public int SLEEP_SEND  { get; set; }
        [JsonProperty("SLEEP_SEND_INTERFACE")]
        public int SLEEP_SEND_INTERFACE { get; set; }
        public int SLEEP_RS { get; set; }

        
        [JsonIgnore]
        private static string PROTOCOL_MESSAGE = "$MDRND,000000.00,000000,0000.0000,N,00000.0000,E,000.0,N,00.0,K,000.0*73"; //-стандартное сообщение 
        [JsonIgnore]
        private static string LAST_ETHERNET_MESSAGE = "====";
        [JsonIgnore]
        private static string LAST_ETHERNET_POINT = "====";
        [JsonIgnore]
        private static string LAST_RS_MESSAGE = "====";
        [JsonIgnore]
        private static ProtokolMessage MESSAGE = new ProtokolMessage();
        [JsonIgnore]
        private static Controller CONTROLLER = new Controller();
        [JsonIgnore]
        private IPEndPoint END_POINT_INTERFACE;
        [JsonIgnore]
        private IPEndPoint END_POINT_CONTROLLER;

        #region Модуль отправки сообщений по Ethernet 
        /// <summary>
        /// Метод для отправки информационного и последнего поступившего сообщения на определенный порт 
        /// </summary>
        /// <param name="port">Порт UDP оконечного устройства</param>
        /// <returns></returns>
        internal async Task SendToReceiversAsync(int port , string IP_ADDRESS , Socket UDP_CONTROLLER) {

            // Создаем endPoint по информации об удаленном хосте девайсов
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(IP_ADDRESS), port);
            try {

                byte[] ByteProtocolMessage = Encoding.ASCII.GetBytes(PROTOCOL_MESSAGE);
                byte[] ByteEthernetMessage = Encoding.ASCII.GetBytes(LAST_ETHERNET_MESSAGE);
                byte[] ByteRSMessage = Encoding.ASCII.GetBytes(LAST_RS_MESSAGE);

                //Отправка на нужные источники и устройства 
                int bytes = await UDP_CONTROLLER.SendToAsync(ByteProtocolMessage, endPoint);
                await UDP_CONTROLLER.SendToAsync(ByteEthernetMessage, endPoint);
                await UDP_CONTROLLER.SendToAsync(ByteRSMessage, endPoint);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Отправлено: " + PROTOCOL_MESSAGE + "   " + bytes);
                
            } catch (ArgumentOutOfRangeException ex) {
                Console.WriteLine("Возникло исключение: " + ex.ToString() + "\n  " + ex.Message);
            } catch (Exception ex) {
                Console.WriteLine("Возникло исключение: " + ex.ToString() + "\n  " + ex.Message);
            }
        }

        internal async Task SendToInterfaceAsync(IPEndPoint POINT_INTERFACE , int SLEEP , Socket UDP_CONTROLLER) {

            CONTROLLER.SLEEP_SEND_INTERFACE = SLEEP;
            END_POINT_INTERFACE = POINT_INTERFACE;
            while (true) {
                try {

                    /*byte[] ByteProtocolMessage = Encoding.ASCII.GetBytes(PROTOCOL_MESSAGE);
                    byte[] ByteEthernetMessage = Encoding.ASCII.GetBytes(LAST_ETHERNET_MESSAGE + " |Time send " + DateTime.Now);
                    byte[] ByteRSMessage = Encoding.ASCII.GetBytes(LAST_RS_MESSAGE + " |Time send " + DateTime.Now);*/

                    string json = "{\"type\":\"protokol_message\"," + "\"message\":" + JsonConvert.SerializeObject(PROTOCOL_MESSAGE) +"," + "\"client_address\":" + JsonConvert.SerializeObject( Convert.ToString(END_POINT_CONTROLLER) ) + "}";
                    byte[] ByteMessage = Encoding.ASCII.GetBytes(json);
                    int bytes = await UDP_CONTROLLER.SendToAsync(ByteMessage, END_POINT_INTERFACE);
                    //отправка на интерфейс постоянно 
                    /* int bytes = await UDP_CONTROLLER.SendToAsync(ByteProtocolMessage, END_POINT_INTERFACE);
                     int bytes1 = await UDP_CONTROLLER.SendToAsync(ByteEthernetMessage, END_POINT_INTERFACE);
                     int bytes2 = await UDP_CONTROLLER.SendToAsync(ByteRSMessage, END_POINT_INTERFACE);*/

                    // Console.WriteLine($"{CONTROLLER.SLEEP_SEND_INTERFACE} + {CONTROLLER.INTERFACE_PORT} + {CONTROLLER.IP_ADDRESS_INTERFACE} + {END_POINT_INTERFACE}");

                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine("Отправлено на интерфейс: " + PROTOCOL_MESSAGE + " " + bytes + "  |||  " + LAST_ETHERNET_MESSAGE );
                    Thread.Sleep(CONTROLLER.SLEEP_SEND_INTERFACE);

                } catch (ArgumentOutOfRangeException ex) {
                    Console.WriteLine("Возникло исключение: " + ex.ToString() + "\n  " + ex.Message);
                } catch (Exception ex) {
                    Console.WriteLine("Возникло исключение: " + ex.ToString() + "\n  " + ex.Message);
                }
            }
        }
        #endregion

        #region Модуль отправки и приема сообщений через COM порт

        internal void ReceiverRS( Dictionary<string,string> COMPORT, int SLEEP) {

            CONTROLLER.SLEEP_RS = SLEEP;

            while (true) {

                //Счетчик по каждому COM - порту
                foreach (var port in COMPORT.Values ) {

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
                Thread.Sleep(CONTROLLER.SLEEP_RS);
            }
        }

        internal void SendToRS(Dictionary<string,string> COMPORT , int SLEEP) {

            CONTROLLER.SLEEP_RS = SLEEP;

            while (true) {

                //Счетчик по каждому COM - порту
                foreach (var port in COMPORT.Values) {

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
                Thread.Sleep(CONTROLLER.SLEEP_RS);
            }
        }

        #endregion

        #region Модуль приема сообщений по Ethernet 

        internal async Task ReceiverEthernetAsync(IPEndPoint LOCAL_IP, Socket UDP_CONTROLLER ) {


            byte[] ReceiveBytes = new byte[2048];
            SocketFlags SF = new SocketFlags();
            //Получаем пришедшие IP с прослушивания 
            EndPoint RemoteIpEndPoint = (EndPoint)LOCAL_IP;


            Console.WriteLine("\n-----------Получение сообщений-----------");
            while (true) {
                try {

                    var result = await UDP_CONTROLLER.ReceiveFromAsync(ReceiveBytes, SocketFlags.None, RemoteIpEndPoint);
                    var Message = Encoding.ASCII.GetString(ReceiveBytes, 0, result.ReceivedBytes);

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Принято:  " + Message);

                    LAST_ETHERNET_MESSAGE = Message;
                    PROTOCOL_MESSAGE = MESSAGE.GetMessage(Message);

                } catch (Exception ex) { }
            }
        }

        internal async Task ReceiverEthernetAsync(IPEndPoint LOCAL_IP, Socket UDP_CONTROLLER, string patch , Controller Device) {


            byte[] ReceiveBytes = new byte[2048];
            SocketFlags SF = new SocketFlags();
            //Получаем пришедшие IP с прослушивания 
            END_POINT_CONTROLLER = LOCAL_IP;
            string text = "";
            Console.WriteLine("\n-----------Получение сообщений-----------");
            while (true) {
                try {

                    var result = await UDP_CONTROLLER.ReceiveFromAsync(ReceiveBytes, SocketFlags.None, END_POINT_CONTROLLER);
                    var Message = Encoding.ASCII.GetString(ReceiveBytes, 0, result.ReceivedBytes);
                   
                    if (result.RemoteEndPoint.ToString() == END_POINT_INTERFACE.ToString()) {

                        await Task.Run(() => CommandRelease(Message,Device,patch , UDP_CONTROLLER));
                        continue;
                    }

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Принято:  " + Message);

                    LAST_ETHERNET_MESSAGE = Message;
                    LAST_ETHERNET_POINT = result.RemoteEndPoint.ToString();

                    string json = "{\"type\":\"last_message\"," + "\"message\":" + JsonConvert.SerializeObject(LAST_ETHERNET_MESSAGE) + "," + "\"client_address\":" + JsonConvert.SerializeObject(LAST_ETHERNET_POINT) + "\"date_time\":" + DateTime.Now +  "}";
                    byte[] ByteMessage = Encoding.ASCII.GetBytes(json);
                    int bytes = await UDP_CONTROLLER.SendToAsync(ByteMessage, END_POINT_INTERFACE);

                    PROTOCOL_MESSAGE = MESSAGE.GetMessage(Message);

                } catch (Exception ex) {  }

            }
        }
        #endregion


        internal async Task CommandRelease(string Message , Controller device, string patch, Socket UDP_CONTROLLER) {

            try {

                using (StreamWriter fileStream = new StreamWriter("New.json", false)) {
                    fileStream.Write(Message);
                }
                Console.WriteLine(Message);
                dynamic JsonFile = JsonConvert.DeserializeObject(File.ReadAllText("New.json"));
                string com = Convert.ToString( JsonFile["type"]);
                string json;
                byte[] ByteMessage = new byte[512];

                switch (com) {
                    case "setf":
                        Console.WriteLine(JsonFile["type"] + " " + JsonFile["SLEEP_SEND_INTERFACE"] + " " + device.SLEEP_SEND_INTERFACE);
                        device.SLEEP_SEND_INTERFACE = Convert.ToInt32(JsonFile["SLEEP_SEND_INTERFACE"]);
                    break;

                    case "get_udp_clients":
                        json = JsonConvert.SerializeObject(device.IP_PORT_SENSOR) + JsonConvert.SerializeObject(device.IP_PORT_DEVICE);
                        ByteMessage = Encoding.ASCII.GetBytes(json);
                        await UDP_CONTROLLER.SendToAsync(ByteMessage, END_POINT_INTERFACE);
                    break;

                    case "get_rs_clients":
                        json = JsonConvert.SerializeObject(device.COMPORT_DEVICE_INPUT) + JsonConvert.SerializeObject(device.COMPORT_DEVICE_OUTPUT);
                        ByteMessage = Encoding.ASCII.GetBytes(json);
                        await UDP_CONTROLLER.SendToAsync(ByteMessage, END_POINT_INTERFACE);
                    break;

                    case "get_frequency":

                    break;
                }
                //Console.WriteLine(JsonFile["type"] + " " + JsonFile["SLEEP_SEND_INTERFACE"] + " " + device.SLEEP_SEND_INTERFACE);
                File.WriteAllText(patch, JsonConvert.SerializeObject(device));
                SetConfig(device);

            }catch(Exception ex) { Console.WriteLine(ex.Message); }
        }

        /// <summary>
        /// Метод выставляет конфигурационные настройки для работы
        /// </summary>
        /// <param name="NewContr">Созданный объект класса Controller</param>
        internal void SetConfig( Controller NewContr) {
            
            CONTROLLER = NewContr;
            END_POINT_INTERFACE = new IPEndPoint(IPAddress.Parse(CONTROLLER.IP_ADDRESS_INTERFACE), CONTROLLER.INTERFACE_PORT);
            END_POINT_CONTROLLER = new IPEndPoint(IPAddress.Parse(CONTROLLER.CONTROLLER_IP_ADDRESS), CONTROLLER.CONTROLLER_PORT);
            
        }

    }
}
