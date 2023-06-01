# Сопрягающее устройство морских навигационных данных

Программа для сопряжения утройств и программ , обменивающих между собой навигационные данные 

## Основные файлы
- Папка Controller – в ней все исходные данные контроллера 
- Папка ProtokolLibraly – библиотека формирования информационного слова
- Папка SensorOne – в ней все исходные данные датчика - источника 
- Папка SensorTwo – в ней все исходные данные датчика - приемника

## Запуск программ 
В каждой папке есть bat файл, который компилирует и запускает программу, в нем по желанию можно изменить путь к файлам csproj и dll выбранной программы.
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

Все начальные параметры UDP клиента задаются файле Config.json .
Чтобы поменять порт и айпи контроллера, нужно их задать либо при объявлении объекта, либо в файле Config.json


```JSON 
  {
  "CONTROLLER_IP_ADDRESS": "127.0.0.1", // Айпи контроллера 
  "CONTROLLER_PORT": 5001, //Порт контроллера 
  "IP_ADDRESS_INTERFACE": "127.0.0.2", //Айпи устройства отображения данных
  "INTERFACE_PORT": 5002, // Порт устройства отображения данных

  "IP_PORT_SENSOR": { // Айпи и порт источников
    "GGL": [ "127.0.0.5", 5005 ],
    "GGA": [ "127.0.0.6", 5006 ],
    "RMC": [ "127.0.0.7", 5007 ],
    "VTG": [ "127.0.0.8", 5008 ],
    "ZDA": [ "127.0.0.9", 5009 ]
  },
  "IP_PORT_DEVICE": { //Айпи и порт приемников 
    "DEV1": [ "127.0.0.11", 5011 ],
    "DEV2": [ "127.0.0.12", 5012 ],
    "DEV3": [ "127.0.0.13", 5013 ],
    "DEV4": [ "127.0.0.14", 5014 ],
    "DEV5": [ "127.0.0.15", 5015 ]
  },
  //названия и порты источников 
  "COMPORT_DEVICE_OUTPUT": {
    "COMDEV1": "COM11",
    "COMDEV2": "COM12",
    "COMDEV3": "COM13",
    "COMDEV4": "COM14",
    "COMDEV5": "COM15"
  },
  "COMPORT_DEVICE_INPUT": {
    "COMDEV6": "COM16",
    "COMDEV7": "COM17",
    "COMDEV8": "COM18",
    "COMDEV9": "COM19",
    "COMDEV10": "COM20"
  },
  //Задержки на отправку
  "SLEEP_SEND": 1000,
  "SLEEP_SEND_INTERFACE": 1000,
  "SLEEP_RS": 200,
  "CODE": "JSON"

}
```

- PORT_SENSOR - айпи и порты источников информации 
- PORT_DEVICE - айпи и порты потребителей информации 
- COMPORT_DEVICE - com порты источников и приемников через интерфейс RS

Выбор порта и отправка на все порты ведется автоматически

Разработчик [Интерфейса](https://github.com/pandazz77/MatingDeviceUI) и [иммитаторов](https://github.com/pandazz77/MatingDeviceSubs)
















