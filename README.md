# Сопрягающее устройство морских навигационных данных

Программа для сопряжения утройств и программ , обменивающих между собой навигационные данные 

## Основные файлы
- Папка Controller – в ней все исходные данные контроллера 
- Папка ProtokolLibraly – библиотека формирования информационного слова
- Папка SensorOne – в ней все исходные данные датчика - источника 
- Папка SensorTwo – в ней все исходные данные датчика - приемника

## Запуск программ 
В каждой папке есть bat файл, который компилирует и запускает программу, в нем ао желанию можно изменить путь к файлам csproj и dll выбранной программы.
Запуск произойдет автоматически из любой директории.
```
@echo off
echo Compiler...
"dotnet" build "%cd%\Controller.csproj"

echo Start...
"dotnet" "%cd%\bin\Debug\net7.0\Controller.dll"

echo Done.
pause
```

Для работы программы должны быть установлены [.NET 7.0 и .NET Framework 4.7](https://dotnet.microsoft.com/en-us/download/visual-studio-sdks)


## Программа Controller

Все начальные параметры UDP клиента задаются в полях класса Controller.
Чтобы поменять порт и айпи контроллера, нужно их задать при объявлении 


```C#
  //Порт контроллера
  private const int localPort = 5001;

  //Обьявление клиента 
  private static Socket UDP_CONTROLLER = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
  private static SocketFlags SF = new SocketFlags();
  private static IPEndPoint LOCAL_IP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), localPort);
```
Для изменения айпи и порта устроства отображения данных - менять при объявлении данное поле 
```C#
  //Объявление клиента - интерфейса 
  IPEndPoint END_POINT_INTERFACE = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5006);
```

- PORT_SENSOR - порты источников информации 
- PORT_DEVICE - порты потребителей информации 
- COMPORT_DEVICE - com порты источников через интерфейс RS

Выбор порта и отправка на все порты ведется автоматически

















