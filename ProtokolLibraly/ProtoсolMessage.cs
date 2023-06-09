﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ProtocolLibrary {


    public class ProtocolMessage {

        Dictionary<string, string> PROTOCOL_MESSAGE = new Dictionary<string, string> {

                {   "TIME",             "000000.00"     },
                {   "DATE",             "000000"        },
                {   "LATITUDE",         "0000.0000"     },  //Широта 
                {   "NS_INDICATOR",     "N"             },  //Север/Юг (N/S).
                {   "LONGITUDE",        "00000.0000"    },  //Долгота
                {   "EW_INDICATOR",     "E"             },  //Запад/Восток (E/W).
                {   "VELOCITY_KNOTS",   "000.0"         },  //Скорость  - в узлах 
                {   "UNITS_KNOTS",      "N"             },  //Единицы измерения - узлы
                {   "VELOCITY_KMPH",    "00.0"          },  //Скорость км/ч  - KMPH - kilometer per hour. 
                {   "UNITS_KMPH",       "K"             },  //Единицы измерения - км/ч
                {   "TRUE_COURSE",      "000.0"         }   //Истинный курс

        };

        /// <summary>
        /// Метод возвращает сообщение по протоколу контроллера 
        /// </summary>
        /// <param name="NMEA_MES"></param>
        /// <returns></returns>
        public string  GetMessage(string NMEA_MES) {

           string CODE = "MDRND"; //Mating Device Required Navigation Data - Сопрягающее Устройство Необходимые Навигационные Данные 
           string CONTROL_SUM = "";

           NMEAReader NmeaMes = new NMEAReader();//создаем объект класса для обработки полученного сообщения

           NmeaMes.GetData(NMEA_MES, PROTOCOL_MESSAGE);//Получить данные из принятых информационных слов и обновить наше сообщение 

           string SEND_PM = $"{CODE},{PROTOCOL_MESSAGE["TIME"]},{PROTOCOL_MESSAGE["DATE"]}," +
                    $"{PROTOCOL_MESSAGE["LATITUDE"]},{PROTOCOL_MESSAGE["NS_INDICATOR"]}," +
                    $"{PROTOCOL_MESSAGE["LONGITUDE"]},{PROTOCOL_MESSAGE["EW_INDICATOR"]}," +
                    $"{PROTOCOL_MESSAGE["VELOCITY_KNOTS"]},{PROTOCOL_MESSAGE["UNITS_KNOTS"]}," +
                    $"{PROTOCOL_MESSAGE["VELOCITY_KMPH"]},{PROTOCOL_MESSAGE["UNITS_KMPH"]}," +
                    $"{PROTOCOL_MESSAGE["TRUE_COURSE"]}";

            CONTROL_SUM = NmeaMes.GetChecksum(SEND_PM);//Контрольная сумма сообщения

            return  $"${SEND_PM}*{CONTROL_SUM}";
        }
    }
    public class NMEAReader {

        private const string СheckZDA = "ZDA";
        private const string CheckGLL = "GLL";
        private const string CheckRMC = "RMC";
        private const string CheckGGA = "GGA";
        private const string CheckVTG = "VTG";

        /// <summary>
        /// Возвращает данные для сообщения по протоколу из полученного сообщения
        /// </summary>
        /// <param name="NMEA_MES"></param>
        /// <returns></returns>
        internal Dictionary<string,string> GetData(string NMEA_MES ,Dictionary<string , string > PROTOCOL_MESSAGE) {
            
            //Все сообщения NMEA из информационного слова 
            List<string> ListMessage = new List<string>();
            ListMessage = SearchMessage(NMEA_MES);
            int count = ListMessage.Count;
            int i = 0;
           
            //Индефикатор
            string CheckIndef; 

            while (i<count) {


                CheckIndef = GetCode(ListMessage[i]);//Получение кода 
                
                string DATA = FormattingDate(ListMessage[i]);//Получение только данных из сообщения
                
                List<string> DATA_MAS = new List<string>(DATA.Split(','));//Получаю массив данных 
                
                switch (CheckIndef) {

                    case СheckZDA: {
                            
                            PROTOCOL_MESSAGE["TIME"] = DATA_MAS[0];
                            PROTOCOL_MESSAGE["DATE"] = $"{DATA_MAS[1]}{DATA_MAS[2]}{DATA_MAS[3].Substring(DATA_MAS[3].Length-2)}";
                            i++;
                    } break;
                    case CheckGLL: {
                            PROTOCOL_MESSAGE["LATITUDE"] = DATA_MAS[0];
                            PROTOCOL_MESSAGE["NS_INDICATOR"] = DATA_MAS[1];
                            PROTOCOL_MESSAGE["LONGITUDE"] = DATA_MAS[2];
                            PROTOCOL_MESSAGE["EW_INDICATOR"] = DATA_MAS[3];
                            PROTOCOL_MESSAGE["TIME"] = DATA_MAS[4];
                            i++; 
                    } break;
                    case CheckRMC: {
                            PROTOCOL_MESSAGE["TIME"] = DATA_MAS[0];
                            PROTOCOL_MESSAGE["LATITUDE"] = DATA_MAS[2];
                            PROTOCOL_MESSAGE["NS_INDICATOR"] = DATA_MAS[3];
                            PROTOCOL_MESSAGE["LONGITUDE"] = DATA_MAS[4];
                            PROTOCOL_MESSAGE["EW_INDICATOR"] = DATA_MAS[5];
                            PROTOCOL_MESSAGE["VELOCITY_KNOTS"] = DATA_MAS[6];
                            PROTOCOL_MESSAGE["TRUE_COURSE"] = DATA_MAS[7];
                            PROTOCOL_MESSAGE["DATE"] = DATA_MAS[8];
                            i++; 
                    } break;
                    case CheckGGA: {
                            PROTOCOL_MESSAGE["TIME"] = DATA_MAS[0];
                            PROTOCOL_MESSAGE["LATITUDE"] = DATA_MAS[1];
                            PROTOCOL_MESSAGE["NS_INDICATOR"] = DATA_MAS[2];
                            PROTOCOL_MESSAGE["LONGITUDE"] = DATA_MAS[3];
                            PROTOCOL_MESSAGE["EW_INDICATOR"] = DATA_MAS[4];
                            i++;
                    } break;
                    case CheckVTG: {
                            PROTOCOL_MESSAGE["TRUE_COURSE"] = DATA_MAS[0];
                            PROTOCOL_MESSAGE["VELOCITY_KNOTS"] = DATA_MAS[4];
                            PROTOCOL_MESSAGE["UNITS_KNOTS"] = DATA_MAS[5];
                            PROTOCOL_MESSAGE["VELOCITY_KMPH"] = DATA_MAS[6];
                            PROTOCOL_MESSAGE["UNITS_KMPH"] = DATA_MAS[7];
                            i++;
                     } break;

                    default: { i++; } break;
                }
                
                i++;
            }
            
            return PROTOCOL_MESSAGE;
        }

        /// <summary>
        /// Возвращает индефикатор сообщения NMEA для его чтения
        /// </summary>
        /// <param name="NMEA_MES"></param>
        /// <returns></returns>
        public static string GetCode(string NMEA_MES) {

            string code = "";
            code = NMEA_MES.Substring(3,3);

            return code;
        }

        /// <summary>
        /// Ищет все NMEA сообщения в принятом сообщении и разбивает их на элементы массива 
        /// </summary>
        /// <param name="NMEA_MES"></param>
        /// <returns></returns>
        private List<string> SearchMessage(string NMEA_MES) {

            //Если пришло пустое сообщение 
            if (NMEA_MES == null) {
                return new List<string>() { "ERROR" };
            }

            List<string> MasMessage = new List<string>();
            string NMEAMessage = "";
            int indexStart;
            int indexEnd;

            while (true) {

                //когда окончилось сообщение 
                if (NMEA_MES == null) {
                    break;
                }

                indexStart = NMEA_MES.IndexOf('$');
                indexEnd =(2+ NMEA_MES.IndexOf('*'));

                //Сообщение не полное и его невозможно полностью прочитать 
                if(indexStart == -1 || indexEnd == -1) {
                    break;
                }

                NMEAMessage = NMEA_MES.Substring ( indexStart , indexEnd + 1 );
                MasMessage.Add(NMEAMessage);
                NMEAMessage = "";
                NMEA_MES = NMEA_MES.Remove(indexStart, indexEnd + 1);

            }

            //Возвращает массив сообщений 
            return MasMessage;
        }
        /// <summary>
        /// Получение данных из сообщения NMEA
        /// </summary>
        /// <param name="NMEAmes"> Сообщение по протоколу NMEA</param>
        /// <returns></returns>
        internal string FormattingDate(string NMEAmes) {

           
            int startsum = (NMEAmes.IndexOf('$') + 7);
            int length = (NMEAmes.Length - 10);
            
            NMEAmes = NMEAmes.Substring(startsum, length);
            
            return NMEAmes;
        }

        internal string GetChecksum(string message) {
            //Стартовый символ
            int checksum = Convert.ToByte(message[0]);
            
            // Перебор всех символов для получения кс
            for (int i = 1; i < message.Length; i++) {
                // XOR кс по каждому символу
                checksum ^= Convert.ToByte(message[i]);
            }
            // Возвращает контрольную сумму, отформатированную в виде двухсимвольного шестнадцатеричного числа
            return checksum.ToString("X2");
        }


    }

}
