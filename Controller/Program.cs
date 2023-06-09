﻿using System.Net;
using System.Net.Sockets;
using System.Text;
using ProtocolLibrary;
using System.Diagnostics;
using System.IO.Ports;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Threading;

namespace NavigationSystem {

    class Program {

        static async Task Main(string[] args) {

            try {

                string patch = @"Config.json";

                MatingDevice DiplomDevice = JsonConvert.DeserializeObject<MatingDevice>(File.ReadAllText(patch));

                DiplomDevice.SetConfig(DiplomDevice);

                IPEndPoint CONTROLLER_IP = new IPEndPoint(IPAddress.Parse(DiplomDevice.CONTROLLER_IP_ADDRESS), DiplomDevice.CONTROLLER_PORT);
                IPEndPoint END_POINT_INTERFACE = new IPEndPoint(IPAddress.Parse(DiplomDevice.IP_ADDRESS_INTERFACE), DiplomDevice.INTERFACE_PORT);

                Socket UDP_CONTROLLER = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                UDP_CONTROLLER.Bind(CONTROLLER_IP);

                //Поток для прослушивания Com портов
                Thread COMReceive = new Thread(() => DiplomDevice.ReceiverRS( DiplomDevice.COMPORT_DEVICE_OUTPUT , DiplomDevice.COMPORT_DEVICE_INPUT, DiplomDevice.SLEEP_RS , UDP_CONTROLLER));
                COMReceive.Start();
                //Поток для отправки сообщений Com портам
                Thread COMSend = new Thread(() =>  DiplomDevice.SendToRS(DiplomDevice.COMPORT_DEVICE_INPUT , DiplomDevice.SLEEP_RS, UDP_CONTROLLER) );
                COMSend.Start();
                 
                //Асинхронный поток для прослушивания Ethernet 
                Task.Run(() => DiplomDevice.ReceiverEthernetAsync(CONTROLLER_IP, UDP_CONTROLLER, patch, DiplomDevice));
                //Асинхронный поток для отправки сообщений Ethernet  на интерфейс
                Task.Run(() => DiplomDevice.SendToInterfaceAsync(END_POINT_INTERFACE , DiplomDevice.SLEEP_SEND_INTERFACE, UDP_CONTROLLER));
               
                while (true) {
                   

                    foreach (var device in DiplomDevice.IP_PORT_DEVICE.Values) {
                        
                        await DiplomDevice.SendToReceiversAsync(Convert.ToInt32(device[1]) , (string)device[0], UDP_CONTROLLER);
                    }
                    foreach (var device in DiplomDevice.IP_PORT_SENSOR.Values) {
                        
                        await DiplomDevice.SendToReceiversAsync(Convert.ToInt32(device[1]), (string)device[0], UDP_CONTROLLER);
                    }

                    
                    // DiplomDevice = JsonConvert.DeserializeObject<Controller>(File.ReadAllText(patch));

                    //Thread.Sleep(DiplomDevice.SLEEP_SEND);
                }

            } catch(Exception ex) { Console.WriteLine(ex.Message); }
        }

       
    }

}

