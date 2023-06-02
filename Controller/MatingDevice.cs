using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ProtocolLibrary;
using System.IO.Ports;
using Newtonsoft.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Newtonsoft.Json.Linq;
using System.Text.Json.Nodes;
using System.Diagnostics;

namespace NavigationSystem {
    public class MatingDevice {
        //Стандартные параметры клиента контролерра 
        //Локальная сеть ИУС
        public string CODE = "JSON";
        //Локальная сеть ИУС на которой контроллер
        public string CONTROLLER_IP_ADDRESS { get; set; }
        //Порт контроллера
        public int CONTROLLER_PORT { get; set; }
        public string IP_ADDRESS_INTERFACE { get; set; }
        //Порт интерфейса
        public int INTERFACE_PORT { get; set; }

        public Dictionary<string, object[]> IP_PORT_SENSOR = new Dictionary<string, object[]>();
        public Dictionary<string, object[]> IP_PORT_DEVICE = new Dictionary<string, object[]>();

        public Dictionary<string, string> COMPORT_DEVICE_OUTPUT = new Dictionary<string, string>();
        public Dictionary<string, string> COMPORT_DEVICE_INPUT = new Dictionary<string, string>();

        public int SLEEP_SEND  { get; set; }
        public int SLEEP_SEND_INTERFACE { get; set; }
        public int SLEEP_RS { get; set; }

        [JsonIgnore]
        public static string PROTOCOL_MESSAGE = "$MDRND,000000.00,000000,0000.0000,N,00000.0000,E,000.0,N,00.0,K,000.0*73"; //-стандартное сообщение 
        [JsonIgnore]
        public static string LAST_ETHERNET_MESSAGE = "";
        [JsonIgnore]
        public static string LAST_ETHERNET_POINT = "";
        [JsonIgnore]
        public static string LAST_RS_MESSAGE = "";
        [JsonIgnore]
        public static ProtocolMessage MESSAGE = new ProtocolMessage();
        [JsonIgnore]
        public static MatingDevice CONTROLLER = new MatingDevice();
        [JsonIgnore]
        public IPEndPoint END_POINT_INTERFACE;
        [JsonIgnore]
        public IPEndPoint END_POINT_CONTROLLER;

        #region Модуль отправки сообщений по Ethernet 
        /// <summary>
        /// Метод для отправки информационного и последнего поступившего сообщения на определенный порт 
        /// </summary>
        /// <param name="port">Порт UDP оконечного устройства</param>
        /// <returns></returns>
        public async Task SendToReceiversAsync(int port , string IP_ADDRESS , Socket UDP_CONTROLLER) {

            // Создаем endPoint по информации об удаленном хосте девайсов
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(IP_ADDRESS), port);
            try {
                int bytes = 0;

                //Отправка на нужные источники и устройства 
                byte[] ByteProtocolMessage = Encoding.ASCII.GetBytes(PROTOCOL_MESSAGE);
                bytes += await Task.Run(() => UDP_CONTROLLER.SendToAsync(ByteProtocolMessage, endPoint));
                Array.Clear(ByteProtocolMessage);
                //Console.WriteLine($"Отправлено устройству на адрес {IP_ADDRESS}:{port}");
                byte[] ByteEthernetMessage = Encoding.ASCII.GetBytes(LAST_ETHERNET_MESSAGE);
                bytes +=  await Task.Run(() => UDP_CONTROLLER.SendToAsync(ByteEthernetMessage, endPoint));
                //Console.WriteLine($"Отправлено на адресс устройства {IP_ADDRESS}:{port}: {LAST_ETHERNET_MESSAGE} ");
                Array.Clear(ByteEthernetMessage);
                
                if (LAST_RS_MESSAGE.Length > 2) {
                    byte[] ByteRSMessage = Encoding.ASCII.GetBytes(LAST_RS_MESSAGE);
                    bytes +=  await Task.Run(() => UDP_CONTROLLER.SendToAsync(ByteRSMessage, endPoint));
                //Console.WriteLine($"Отправлено на адресс устройства {IP_ADDRESS}:{port}: {LAST_RS_MESSAGE} ");
                    Array.Clear(ByteRSMessage);
                }
                //Console.WriteLine($"Отправлено на адресс устройства {IP_ADDRESS}:{port}: {PROTOCOL_MESSAGE} . Всего переданно байт {bytes} ");

            } catch (ArgumentOutOfRangeException ex) {
                Console.WriteLine("Возникло исключение: " + ex.ToString() + "\n  " + ex.Message);
            } catch (Exception ex) {
                Console.WriteLine("Возникло исключение: " + ex.ToString() + "\n  " + ex.Message);
            }
            
        }
        /// <summary>
        /// Метод для отправки данных устройству отображения данных
        /// </summary>
        /// <param name="POINT_INTERFACE"></param>
        /// <param name="SLEEP"></param>
        /// <param name="UDP_CONTROLLER"></param>
        /// <returns></returns>
        public async Task SendToInterfaceAsync(IPEndPoint POINT_INTERFACE , int SLEEP , Socket UDP_CONTROLLER) {
           
           
            CONTROLLER.SLEEP_SEND_INTERFACE = SLEEP;
            END_POINT_INTERFACE = POINT_INTERFACE;
            while (true) {
                try {
                    
                    string json = "{\"type\":\"protocol_message\"," + "\"client_name\":" + "\"CONTROLLER\","  + "\"message\":" + JsonConvert.SerializeObject(PROTOCOL_MESSAGE) + "," + "\"client_address\":" + JsonConvert.SerializeObject(Convert.ToString(END_POINT_CONTROLLER)) + ",\"date_time\":" + "\"" + DateTime.Now + "\"" + "}";
                    
                    byte[] ByteMessage = Encoding.ASCII.GetBytes(json);
                    var bytes = await Task.Run(() => UDP_CONTROLLER.SendTo(ByteMessage, END_POINT_INTERFACE)) ;


                    
                    /*  Console.ForegroundColor = ConsoleColor.Blue;
                      Console.WriteLine("Отправлено на интерфейс: " + PROTOCOL_MESSAGE + " " + bytes + "  |||  " + LAST_ETHERNET_MESSAGE );*/

                    Thread.Sleep(CONTROLLER.SLEEP_SEND_INTERFACE);
                    Array.Clear(ByteMessage);

                } catch (ArgumentOutOfRangeException ex) {
                    Console.WriteLine("Возникло исключение: " + ex.ToString() + "\n  " + ex.Message);
                } catch (Exception ex) {
                    Console.WriteLine("Возникло исключение: " + ex.ToString() + "\n  " + ex.Message);
                }
            }
        }
        #endregion

        #region Модуль отправки и приема сообщений через COM порт
        /// <summary>
        /// Метод для приема сообщений с COM-порта
        /// </summary>
        /// <param name="COMPORT"></param>
        /// <param name="COMPORT_IN"></param>
        /// <param name="SLEEP"></param>
        /// <param name="UDP_CONTROLLER"></param>
        public void ReceiverRS( Dictionary<string,string> COMPORT, Dictionary<string, string> COMPORT_IN, int SLEEP, Socket UDP_CONTROLLER) {

            CONTROLLER.SLEEP_RS = SLEEP;

            while (true) {
                try {
                    //Счетчик по каждому COM - порту
                    foreach (var port in COMPORT.Keys) {
                        //Проверка работоспособности выбранного порта
                        var isValid = SerialPort.GetPortNames().Any(x => string.Compare(x, COMPORT[port], true) == 0);
                        if (!isValid) {

                            var json = "{\"type\":\"rs_info\"," + "\"name_device\":" + JsonConvert.SerializeObject(port) + ",\"COM_PORT\":" + JsonConvert.SerializeObject(COMPORT[port]) + ",\"valid\":" + JsonConvert.SerializeObject(0) + ",\"message\":" + JsonConvert.SerializeObject(0) +  "}";
                            var ByteMessage = Encoding.ASCII.GetBytes(json);
                            UDP_CONTROLLER.SendTo(ByteMessage, END_POINT_INTERFACE);

                            //throw new System.IO.IOException(string.Format("{0} port was not found", COMPORT[port]));//Информация - что порт закрыт
                        }else {
                            SerialPort comport = new SerialPort(COMPORT[port]);
                            comport.Open();

                            // Читаем данные из открытого порта 
                            string data1 = comport.ReadLine();
                            //Console.WriteLine(data1);
                            //Формируем информационное сообщение и сохраняем пришедшие данные 
                            PROTOCOL_MESSAGE = MESSAGE.GetMessage(data1);
                            LAST_RS_MESSAGE = data1;
                           
                            Console.WriteLine($"Принято от устройства {port} порта {COMPORT[port]}: {data1}" );

                            var json = "{\"type\":\"rs_info\"," + "\"name_device\":" + JsonConvert.SerializeObject(port) + ",\"COM_PORT\":" + JsonConvert.SerializeObject(COMPORT[port]) + ",\"valid\":" + JsonConvert.SerializeObject(1) + ",\"message\":" + JsonConvert.SerializeObject(LAST_RS_MESSAGE) + "}";
                            var ByteMessage = Encoding.ASCII.GetBytes(json);
                            UDP_CONTROLLER.SendTo(ByteMessage, END_POINT_INTERFACE);
                            Array.Clear(ByteMessage);
                            //закрываем порт для работы без ошибок 
                            
                            comport.Close();
                        }

                    }
                } catch (Exception ex) {
                    Console.WriteLine(ex.ToString());
                }
                //Задержка на чтение и отправку 
                Thread.Sleep(CONTROLLER.SLEEP_RS);
            }
        }
            /// <summary>
            ///  Метод для отправки сообщений COM-порту
            /// </summary>
            /// <param name="COMPORT"></param>
            /// <param name="SLEEP"></param>
            /// <param name="UDP_CONTROLLER"></param>
            public void SendToRS(Dictionary<string,string> COMPORT , int SLEEP, Socket UDP_CONTROLLER) {

            CONTROLLER.SLEEP_RS = SLEEP;

            while (true) {

                //Счетчик по каждому COM - порту
                foreach (var port in COMPORT.Keys) {

                    try {
                        //Проверка работоспособности выбранного порта
                        var isValid = SerialPort.GetPortNames().Any(x => string.Compare(x, COMPORT[port], true) == 0);
                        if (!isValid) {

                             var json = "{\"type\":\"rs_info\"," + "\"name_device\":" + JsonConvert.SerializeObject(port) + ",\"COM_PORT\":" + JsonConvert.SerializeObject(COMPORT[port]) + ",\"valid\":" + JsonConvert.SerializeObject(0) + ",\"message\":" + JsonConvert.SerializeObject(0) + "}";
                            var ByteMessage = Encoding.ASCII.GetBytes(json);
                            UDP_CONTROLLER.SendTo(ByteMessage, END_POINT_INTERFACE);
                            Array.Clear(ByteMessage);
                            //throw new System.IO.IOException(string.Format("{0} port was not found", COMPORT[port]));//Информация - что порт закрыт
                            
                        } else {

                            SerialPort comport = new SerialPort(COMPORT[port]);
                            comport.Open();
                            Console.WriteLine(COMPORT[port] + " Valid");
                            //передаем данные оконечномоу устройству 
                            //comport.WriteLine(PROTOCOL_MESSAGE);
                            if (LAST_ETHERNET_MESSAGE.Length >2 ) comport.WriteLine(LAST_ETHERNET_MESSAGE);
                            //закрываем порт для работы без ошибок 
                            comport.Close();
                        }

                    } catch (Exception ex) {
                        Console.WriteLine(ex.ToString());
                    }

                }
                //Задержка на чтение и отправку 
                Thread.Sleep(CONTROLLER.SLEEP_RS);
            }
        }

        #endregion

        #region Модуль приема сообщений по Ethernet 

        public async Task ReceiverEthernetAsync(IPEndPoint LOCAL_IP, Socket UDP_CONTROLLER, string patch , MatingDevice Device) {
       

            byte[] ReceiveBytes = new byte[2048];
            SocketFlags SF = new SocketFlags();
            //Получаем пришедшие IP с прослушивания 
            END_POINT_CONTROLLER = LOCAL_IP;
            string code = "";
            Console.WriteLine("\n-----------Получение сообщений-----------");

            while (true) {

                try {

                    var result = await UDP_CONTROLLER.ReceiveFromAsync(ReceiveBytes, SocketFlags.None, END_POINT_CONTROLLER);
                    var Message = Encoding.ASCII.GetString(ReceiveBytes, 0, result.ReceivedBytes);
                    //Message = string.Concat(Message.Where(x => !char.IsWhiteSpace(x)).ToArray() ) ;
                    Message = string.Concat(Message.Where(x => !char.IsWhiteSpace(x)).ToArray());

                    if (result.RemoteEndPoint.ToString() == END_POINT_INTERFACE.ToString()) {

                        await Task.Run(() => SetCommand(Message, Device, patch, UDP_CONTROLLER));
                        continue;
                    }

                    //Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Принято от устройства {result.RemoteEndPoint.ToString()} -> {Message} ");
                    PROTOCOL_MESSAGE = MESSAGE.GetMessage(Message);
                    LAST_ETHERNET_MESSAGE = Message;
                    LAST_ETHERNET_POINT = result.RemoteEndPoint.ToString();
                    code = GetKeyFromValue(LAST_ETHERNET_POINT, Device);

                    string json = "{\"type\":\"last_message\"," + "\"client_name\":" + "\"" + code + "\"," + "\"message\":" + JsonConvert.SerializeObject(Message) + "," + "\"client_address\":" + JsonConvert.SerializeObject(LAST_ETHERNET_POINT) + ",\"date_time\":" + "\"" + DateTime.Now + "\"" + "}";


                    byte[] ByteMessage = Encoding.ASCII.GetBytes(json);
                    string NMEAmes = Encoding.ASCII.GetString(ByteMessage);
                    UDP_CONTROLLER.SendToAsync(ByteMessage, END_POINT_INTERFACE);

                    Array.Clear(ByteMessage);

                } catch (SocketException soc) {

                } catch (ArgumentOutOfRangeException arg ) {
                    Console.WriteLine("Message Error"); 
                } catch (Exception ex) {
                    Console.WriteLine(ex.ToString());
                }

            }
        }
        #endregion

        /// <summary>
        /// Метод для ввода настроек, принятых от устройства отображения данных 
        /// </summary>
        /// <param name="Message"></param>
        /// <param name="device"></param>
        /// <param name="patch"></param>
        /// <param name="UDP_CONTROLLER"></param>
        /// <returns></returns>
        public async Task SetCommand(string Message , MatingDevice device, string patch, Socket UDP_CONTROLLER) {

            try {
                using (StreamWriter fileStream = new StreamWriter("New.json", false)) {
                    fileStream.Write(Message);
                }

                //Console.WriteLine(Message);
                dynamic JsonFile = JsonConvert.DeserializeObject(File.ReadAllText("New.json"));
                string com = Convert.ToString( JsonFile["type"]);
                string json;
                byte[] ByteMessage = new byte[512];

                switch (com) {
                    case "set_frequency":
                        if (Convert.ToString(JsonFile["device"]) == "interface"  && Convert.ToInt32(JsonFile["frequency"]) != device.SLEEP_SEND_INTERFACE && Convert.ToInt32(JsonFile["frequency"]) != null) device.SLEEP_SEND_INTERFACE = Convert.ToInt32(JsonFile["frequency"]);
                        if (Convert.ToString(JsonFile["device"]) == "ethernet_device" && Convert.ToInt32(JsonFile["frequency"]) != device.SLEEP_SEND && Convert.ToInt32(JsonFile["frequency"]) != null) device.SLEEP_SEND = Convert.ToInt32(JsonFile["frequency"]) ;
                        if (Convert.ToString(JsonFile["device"]) == "rs_device" && Convert.ToInt32(JsonFile["frequency"]) != device.SLEEP_RS && Convert.ToInt32(JsonFile["frequency"]) != null) device.SLEEP_RS = Convert.ToInt32(JsonFile["frequency"]);
                    break;

                    case "get_udp_clients":
                        json = "{\"type\":\"clients_udp\"," + "\"clients_sensor\":" + JsonConvert.SerializeObject(device.IP_PORT_SENSOR) + ",\"clients_device\":" + JsonConvert.SerializeObject(device.IP_PORT_DEVICE) + "}" ;
                        ByteMessage = Encoding.ASCII.GetBytes(json);
                        await UDP_CONTROLLER.SendToAsync(ByteMessage, END_POINT_INTERFACE);

                    break;

                    case "get_rs_clients":
                        json = "{\"type\":\"clients_rs\"," + "\"comport_in\":" + JsonConvert.SerializeObject(device.COMPORT_DEVICE_INPUT) + ",\"comport_out\":" +  JsonConvert.SerializeObject(device.COMPORT_DEVICE_OUTPUT) + "}" ;
                        ByteMessage = Encoding.ASCII.GetBytes(json);
                        await UDP_CONTROLLER.SendToAsync(ByteMessage, END_POINT_INTERFACE);
                    break;

                    case "get_frequency":
                        json = "{\"type\":\"clients_frequency\"," + "\"interface\":" +  JsonConvert.SerializeObject(device.SLEEP_SEND_INTERFACE) + ",\"ethernet_device\":" + JsonConvert.SerializeObject(device.SLEEP_SEND) + ",\"rs_device\":" + JsonConvert.SerializeObject(device.SLEEP_RS) + "}";
                        ByteMessage = Encoding.ASCII.GetBytes(json);
                        await UDP_CONTROLLER.SendToAsync(ByteMessage, END_POINT_INTERFACE);
                    break;

                    case "add_client_sensor":
                        if (CheckPort(Convert.ToInt32(JsonFile["client_port"]), device) && !device.IP_PORT_DEVICE.ContainsKey(JsonFile["client_name"]) && !device.IP_PORT_SENSOR.ContainsKey(JsonFile["client_name"])) {
                            device.IP_PORT_SENSOR.Add(Convert.ToString(JsonFile["client_name"]), new object[] { Convert.ToString(JsonFile["client_ip"]) , Convert.ToInt32(JsonFile["client_port"]) }); 
                        } else Console.WriteLine("Error");
                    break;

                    case "remove_client":

                    break;
                }
                Array.Clear(ByteMessage);
                File.WriteAllText(patch, JsonConvert.SerializeObject(device));
                SetConfig(device);

            }catch(Exception ex) { Console.WriteLine(ex.Message); }
        }


        /// <summary>
        /// Метод для проверки наличия вводимого ip в словаре
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="device"></param>
        /// <returns true = "если можно добавить устройство"></returns>
        public static bool CheckPort(int port, MatingDevice device) {
            
            foreach (var keyVar in device.IP_PORT_SENSOR.Values) {

                if (Convert.ToInt32(keyVar[1]) == port) {
                    return false;
                }
            }

            foreach (var keyVar in device.IP_PORT_DEVICE.Values) {
                if (Convert.ToInt32(keyVar[1]) == port) {
                    return false;
                }
            }
            return true;
        }


        
        public static string GetKeyFromValue(string valueVar , MatingDevice device ) {

            int index = valueVar.IndexOf(':');
            string ip = valueVar.Substring(0, index);
            int port = Convert.ToInt32( valueVar.Substring(index+1, 4) );

            object[] dev = { ip, port };


            foreach (string keyVar in device.IP_PORT_SENSOR.Keys) {
                
                if (Convert.ToInt32(device.IP_PORT_SENSOR[keyVar][1]) == port) {
                    return keyVar;
                }
            }

            foreach (string keyVar in device.IP_PORT_DEVICE.Keys) {
                if (device.IP_PORT_DEVICE[keyVar] == dev) {
                    return keyVar;
                }
            }

            return "no_data";
        }


        /// <summary>
        /// Метод выставляет конфигурационные настройки для работы
        /// </summary>
        /// <param name="NewContr">Созданный объект класса Controller</param>
        public void SetConfig(MatingDevice  NewContr) {
            
            CONTROLLER = NewContr;
            END_POINT_INTERFACE = new IPEndPoint(IPAddress.Parse(CONTROLLER.IP_ADDRESS_INTERFACE), CONTROLLER.INTERFACE_PORT);
            END_POINT_CONTROLLER = new IPEndPoint(IPAddress.Parse(CONTROLLER.CONTROLLER_IP_ADDRESS), CONTROLLER.CONTROLLER_PORT);
            
        }

    }
}
