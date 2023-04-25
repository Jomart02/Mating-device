using Microsoft.VisualBasic;
using System;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
//using System.Text.Unicode;
using System.Collections.Generic;
using System.IO;
using System.Security.Policy;
using System.ComponentModel;

namespace ProtokolLibrary {

    #region Перевод систем счисления 
    public static class Decoding {//статичный класс чтобы не создавать экземпляр этого класса

        /// <summary>
        /// Метод конвертирует(кодирует) переменную типа string в строку двоичного кода 
        /// </summary>
        /// <param name="usr_input">Строка для кодировки(тип string)</param>
        /// <returns>Строку в двоичном виде</returns>
        public static string StringToBinary(string usr_input) {
            string binary_str = "";
            foreach (char ch in usr_input) {
                int ascii_value = (int)ch;//код символа 
                string binary_value = "";
                int remainder = 0;//остаток 
                while (ascii_value > 0) {//Перевод строки в ручную, без использования Convert
                    remainder = ascii_value % 2;
                    ascii_value /= 2;
                    binary_value = remainder + binary_value;
                }
                binary_value = binary_value.PadLeft(8, '0');
                binary_str = binary_str + binary_value;
            }

            return binary_str;
        }


        /// <summary>
        /// Метод конвертирует(кодирует) переменную типа string в строку двоичного кода 
        /// </summary>
        /// <param name="message">Строка для кодировки(тип string)</param>
        /// <returns>Строку в двоичном виде</returns>
       /* public static string StringToBinary(string message) {

            StringBuilder sb = new StringBuilder();

            foreach (char c in message.ToCharArray()) {
                sb.Append(Convert.ToString(c, 2).PadLeft(8, '0'));
            }
            return sb.ToString();
        }*/
        /// <summary>
        /// Метод конвертирует(декодирует) сообщение типа string из двоичного представления в ASCII
        /// </summary>
        /// <param name="binarymessage">Строка для декодировки(тип string)</param>
        /// <returns>Строку в ASCIIе</returns>
        public static string BinaryToString(string binarymessage) {
            binarymessage = binarymessage.Substring(4, 16);
            List<Byte> byteList = new List<Byte>();

            for (int i = 0; i < binarymessage.Length; i += 8) {
                byteList.Add(Convert.ToByte(binarymessage.Substring(i, 8), 2));
            }
            return Encoding.ASCII.GetString(byteList.ToArray());
        }


        public static string BinaryToStringAll(string binarymessage) {

            List<Byte> byteList = new List<Byte>();
            
            try {

                for (int i = 0; i < binarymessage.Length; i += 8) {

                    byteList.Add(Convert.ToByte(binarymessage.Substring(i, 8), 2));
                }

            } catch (Exception ex) { Console.WriteLine(ex.ToString()); }

            return Encoding.ASCII.GetString(byteList.ToArray());
        }


        }
    #endregion

    #region Формирование  информационного, командного, ответного слов

    public class MessageProtokol {
        /*
         * Информационные слова содержат синхронизацию 001
         * Командные слова содержат синхронизацию 000
         */

        private static string SYNX = "";
        /// <summary>
        /// Метод для формирования информационного слова 
        /// </summary>
        /// <param name="Bin_Symvol">Символы сообщения (2 символа по 8 бит ) в двоичной системе для формирования ИС</param>
        /// <returns></returns>
        public static string InformationWord(string Bin_Symvol) {

            SYNX = "000";

            string ParityBit = GetParityBit(Bin_Symvol);
            if (Bin_Symvol.Length == 8) Bin_Symvol += "00000000";//Для формирования полного слова в случае остатка 8 байт добавляется символ пробела

            return $"{SYNX}{Bin_Symvol}{ParityBit}";
        }

        /// <summary>
        /// Метод для формирования командного сдова с заданными параметрами , возвращает string командного слова , синхронизация по умолчанию 001
        /// </summary>
        /// <param name="Addres">Адресс оконечного устройства для отправки </param>
        /// <param name="Sub_Addres">Адресс отправителя КС</param>
        /// <param name="WR"> Бит приема-передачи =>(Если WR = 0, контроллер канала передает данные на оконечное устройство. Если WR = 1, контроллер канала принимает данные от оконечного устройства.)</param>
        /// <param name="N">Число информационных слов в пакете </param>
        /// <returns></returns>
        public static string CommandWord(string Addres, string Sub_Addres, string WR , int N, string SYNX = "001") {
            SYNX = "001";

            //string Quantity_Word = "00000"; //число слов которое будет передано ???

            string binaryNum = Convert.ToString(N, 2);

            while (binaryNum.Length <5) binaryNum = "0" + binaryNum;//Для устранения недостающих знаков при кодировке

            string ParityBit = GetParityBit($"{Addres}{WR}{Sub_Addres}{binaryNum}");

            
            return $"{SYNX}{Addres}{WR}{Sub_Addres}{binaryNum}{ParityBit}";
        }

        /// <summary>
        /// Метод для формирования ответного слова
        /// </summary>
        /// <param name="SYNC_C"></param>
        /// <param name="Addres"></param>
        /// <returns></returns>
        //public static string ResponseWord(string SYNC_C, string Addres) {
        public static string ResponseWord(string message , string SYNC_C, string Addres,string WR) {

            
            string A = "0";
            string B = "0";
            string C = "0";
            string X = "0";
            string D = "0";
            string E = "0";
            string F = "0";
            string G = "0";
            string H = "0";

            SYNC_C = "001";
            //string BinMessage = Decoding.StringToBinary(message);//Если идет прием сообщения
            string RW = "";//Не используется в данном случае 
            

            if (WR == "1") ReadMessageProtokol.CheckInformationWord(message, out A, out B, out C, out X, out D, out E, out F, out G, out H);//Если идет отправка сообщения сообщения
            else {
                string IW = Receive.GetMessageClient(message);
                
                ReadMessageProtokol.CheckInformationWord(IW, out A, out B, out C, out X, out D, out E, out F, out G, out H);//Если идет прием сообщения
            }
                //проверка на кс - если все норм то А = 1

            
            string ParityBit = GetParityBit($"{Addres}{A}{B}{C}{X}{X}{X}{D}{E}{F}{G}{H}");
            
            return $"{SYNC_C}{Addres}{A}{B}{C}{X}{X}{X}{D}{E}{F}{G}{H}{ParityBit}";
        }


        /// <summary>
        /// Метот для проверки 
        /// </summary>
        /// <param name="Bin_Symvol">Бит паритета , должен иметь такое значение, чтобы общее количество единиц в слове (за исключением синхросигнала) было нечетным</param>
        /// <returns></returns>
        private static string GetParityBit(string Bin_Symvol) {
            string ParityBit = "";
            int ones = 0;
            foreach (var one in Bin_Symvol)
                if (one == '1') ones++;

            if ((ones % 2) == 0) ParityBit = "1";
            else ParityBit = "0";

            return ParityBit;
        }
    }


    #endregion

    //Параметры передачи КК->ОУ
    #region Отправка сообщений

    public static class SendMessageProtokol {

        //static string resfile = @"C:\2022-2023\Shaep\ConverterNumber\Result.txt";
        /// <summary>
        /// Метод формирует по заданным параметрам пакет с сообщением и отправляет по указанному адресу в указанном режиме
        /// </summary>
        /// <param name="message">Сообщение в кодировке ASCII- nmea</param>
        /// <param name="Addres">Адрес оконечного устройства - 5 бит</param>
        /// <param name="Sub_Addres">Адрес отправляющего устройства - 5 бит</param>
        /// <param name="WR">Бит приема-передачи =>(Если WR = 0, контроллер канала передает данные на оконечное устройство. Если WR = 1, контроллер канала принимает данные от оконечного устройства.)</param>
        /// <param name="N">Число принимаемых информационных слов  - 5 бит</param>
        public static string StartSend(string message, string SYNC, string Addres, string WR, string Sub_Addres, int N) {

            
            string BinMessage = Decoding.StringToBinary(message);
            
            string recive = string.Empty;
            switch (WR) {
                case "0":
                    recive = PackageFormation(BinMessage, message, SYNC, Addres, WR, Sub_Addres, N);

                    break;

                case "1":
                    recive = PackageFormation(BinMessage, message, SYNC, Addres, WR, Sub_Addres, N);
                    break;

                case "2":
                    
                    recive = MessageProtokol.ResponseWord( message ,SYNC, Addres , WR );//нужно реальное сообщение чтобы проверить его кс 
                    break;
            }
            return recive;

        }

        /// <summary>
        /// Метод формирует пакет сообщений по заданный параметрам для протокола UDP
        /// </summary>
        /// <param name="Bin_Message">Сообщение в кодировке ASCII</param>
        /// <param name="SYNC">Синхронизация</param>
        /// <param name="Addres">Адрес оконечного устройства - 5 бит</param>
        /// <param name="WR">Бит приема-передачи =>(Если WR = 0, контроллер канала передает данные на оконечное устройство. Если WR = 1, контроллер канала принимает данные от оконечного устройства.)</param>
        /// <param name="Sub_Addres">Адрес отправляющего устройства - 5 бит</param>
        /// <param name="N">Число принимаемых информационных слов  - 5 бит</param>
        /// <returns></returns>
        private static string PackageFormation(string Bin_Message,string message, string SYNC, string Addres, string WR,string Sub_Addres, int N) {

            string two_symvol = "";
            int regstart = 0; // фиксирует отправку кс 
            int i = 0;
            string recive = "";

            while (true) {

                //Определение порядка отправляемых слов 
                if (WR == "0" && regstart == 0) {
                    //recive += MessageProtokol.ResponseWord(SYNC, Addres);
                    
                    recive += MessageProtokol.CommandWord( Addres, Sub_Addres , WR, N , SYNC);
                    regstart++;

                } else if (WR == "1" && regstart == 0) {

                    recive += MessageProtokol.ResponseWord(message ,SYNC, Addres , WR);
                    regstart++;
                    
                }
                if (i <= N) {//Для формирования указанного пакета строк 

                    if (Bin_Message.Length >= 16) {//сообщение может быть 8бит 

                        two_symvol = Bin_Message.Substring(0, 16);
                        Bin_Message = Bin_Message.Remove(0, 16);
                        recive += MessageProtokol.InformationWord(two_symvol); //отправка информационного слова 
                    } else if (Bin_Message.Length == 8) {

                        two_symvol = Bin_Message.Substring(0, 8);
                        Bin_Message = Bin_Message.Remove(0, 8);
                        recive += MessageProtokol.InformationWord(two_symvol);
                    } else break;

                    i++;

                    if (Bin_Message.Length > 0 && i > N) {
                        regstart = 0;
                        i= 0;
                    }


                } else { break; }
            }

            return recive;

        }

    }


    #endregion

    #region Прием сообщений коннектором 

    public static class Receive {
        /// <summary>
        /// Метод для обработки полученного пакета сообщения по сокетам, выделяет нужную информацию и отделяет ИС от ОС, проверяет ОС - только для контроллера 
        /// </summary>
        /// <param name="Message">Полученное сообщение коннектором</param>
        /// <returns></returns>
        public static string GetMessageConnector(string Message, out string ResponseWord, out int flag) {

            string SYNX_C = "001";//Синхронизация для ОС
            string SYNC_D = "000";//Синхронизация для ИС
            flag = 1;//Прием информационных сообщений - 1 (0 - только ОС)

            
            string ReceiveMes = "";
            string NMEAmes = "";

            ResponseWord = Message.Substring(0, 20);

            for (int i = 0; i < Message.Length; i += 20) {


                if (Message != null || Message[i] != null) {

                    if (Message.Substring(i, 3) == SYNX_C) {

                        ResponseWord = Message.Substring(i, 20);
                        //Message.Remove(i, 20);

                    } else if (Message.Substring(i, 3) == SYNC_D) {

                        NMEAmes += Message.Substring(i, 20);
                    }
                } else break;

            }

            //Если нет ИС - только ОС
            if (NMEAmes == "") {
                flag = 0;
                //Console.WriteLine(ReadMessageProtokol.ReadResponseWord(ResponseWord));
                return Message;
            }

            //Console.WriteLine(ReadMessageProtokol.ReadResponseWord(ResponseWord));
            ReceiveMes += ReadMessageProtokol.ReadInformationWord(NMEAmes);
            
            return ReceiveMes;
        }


        /// <summary>
        /// Метод для обработки полученного пакета сообщения по сокетам, выделяет нужную информацию и отделяет ИС от КС, проверяет КС - только для контроллера 
        /// </summary>
        /// <param name="Message">Полученное информационное сообщение коннектором</param>
        /// <returns></returns>
        public static string GetMessageClient(string Message) {

            string SYNX_C = "001";//Синхронизация для КС
            string SYNC_D = "000";//Синхронизация для ИС

            string CommandWord = "";
            string ReceiveMes = "";
            string NMEAmes = "";


            for (int i = 0; i < Message.Length; i += 20) {


                if (Message != null || Message[i] != null) {

                    if (Message.Substring(i, 3) == SYNX_C) {

                        CommandWord += Message.Substring(i, 20);
                        //Message.Remove(i, 20);

                    } else if (Message.Substring(i, 3) == SYNC_D) {

                        NMEAmes += Message.Substring(i, 20);
                    }
                } else break;
            }



            ReceiveMes += ReadMessageProtokol.ReadInformationWord(NMEAmes);
            

            return ReceiveMes;
        }
    }

    #endregion


    #region Чтение информационного, командного, ответного слов

    public static class ReadMessageProtokol {

        //private static List<string> IWtoMessage;
        /// <summary>
        /// Метод для расшифровки комендного слова - принимает весь пакет и берет из него первые 20 символов для чтения только КС 
        /// </summary>
        /// <param name="Word"></param>
        /// <param name="N"></param>
        /// <param name="SYNC_C"></param>
        /// <param name="ADDR_RT"></param>
        /// <param name="SUB_ADDRT"></param>
        /// <param name="WR"></param>
        
        public static void ReadCommandWord(string Word, out int N, out string SYNC_C, out string ADDR_RT , out string SUB_ADDRT , out char WR) {
            //string binary = Convert.ToString(value, 2); число в строку ']

            Word = Word.Substring(0,20);
            WR = Word[8];
            string Num = Word.Substring(14, 5);

            N = Convert.ToInt32(Num, 2); //строку в число ==> к-во ИС строк сколько коннектору нужно для приема 
            SYNC_C = Word.Substring(0, 3);
            ADDR_RT = Word.Substring(3, 5);
            SUB_ADDRT = Word.Substring(9, 5);

            /*WR = Word[8];
            string Num = Word.Substring(14, 5);*/
            if (WR == '1') { //если передача от оконечного устройства к коннектору идет запрос от коннектора
                N = Convert.ToInt32(Num, 2); //строку в число ==> к-во ИС строк сколько коннектору нужно для приема 
                SYNC_C = Word.Substring(0, 3);
                ADDR_RT = Word.Substring(3, 5);
                SUB_ADDRT= Word.Substring(9, 5);
            }
        }

        /// <summary>
        /// Метод для перекодировки пакета информационных слов и формулирования сообщения - если пакет один
        /// </summary>
        /// <param name="BinIW">Информационное слово в двоичном формате</param>
        /// <param name="BinIW">Информационное слово в двоичном формате</param>y0
        /// 00
        /// 
        /// <returns></returns>
        public static string ReadInformationWord(string BinIW, out string ResponseWord) {
            
            string inform = "";

            if (BinIW.Length >= 20) {

                ResponseWord = BinIW.Substring(0, 20);

                string check = BinIW.Remove(0, 20);
                
                //int i = 0;
                while (true) {

                    inform += check.Substring(3, 16);
                    check = check.Remove(0, 20);
                    if (check.Length < 20) break;

                }
            } else {

                ResponseWord = null;
                return "Error";
            }
            
            return Decoding.BinaryToStringAll(inform);
        }


        public static string ReadInformationWord(string BinIW) {

            string inform = "";
            string check = BinIW; 
            if (BinIW.Length >= 20) {

                //int i = 0;
                while (true) {

                    inform += check.Substring(3, 16);
                    check = check.Remove(0, 20);
                    if (check.Length < 20) break;

                }
            } else {

                return "Error";
            }


            return Decoding.BinaryToStringAll(inform);
        }


        /// <summary>
        /// Метод для расшифровки ответного слова 
        /// </summary>
        /// <param name="BinIW"></param>
        /// <returns></returns>
        public static string ReadResponseWord(string response) {

            string A = response.Substring(8,1);
            string B = response.Substring(9,1);
            string C = response.Substring(10,1);
            string X = response.Substring(11, 3);
            string D = response.Substring(14, 1);
            string E = response.Substring(15, 1);
            string F = response.Substring(16, 1);
            string G = response.Substring(17, 1);
            string H = response.Substring(18, 1);


            if(A == "1") return "Response word gooooooooooood";
            else return "Response word no goooooooooooooood";
        }

        //Проверка информационного слова его контрольная сумма  
        public static void CheckInformationWord(string message , out string A ,out string B,out string C, out string X,out string D, out string E, out string F, out string G, out string H ){
  
            A = "0";
            B = "0";
            C = "0";
            X = "0";
            D = "0";
            E = "0";
            F = "0";
            G = "0";
            H = "0";

            string checkCS = GetChecksum(message);
            int CS = message.IndexOf('*') + 1 ;
            
            if (message.Substring(CS, 2) == checkCS) A = "1";//true
            else A = "0";

        }

        private static string GetChecksum(string message) {
            //Стартовый символ
           // int checksum = Convert.ToByte(message[message.IndexOf('$') + 1]);
            // Перебор всех символов для получения кс
            int startsum = (message.IndexOf('$') + 1 );
            int lastsum = (message.IndexOf('*') - 1);

            message = message.Substring(startsum, lastsum);
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


    #endregion
}
