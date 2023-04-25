# Сопрягающее устройство морских навигационных данных

Программа для сопряжения утройств и программ , обменивающих между собой навигационные данные 

## Основные файлы
- Папка Controller – в ней все исходные данные контроллера 
- Папка ProtokolLibraly – библиотека формирования информационного слова
- Папка SensorOne – в ней все исходные данные датчика - источника 
- Папка SensorTwo – в ней все исходные данные датчика - приемника

## Запуск программ 
В каждой папке есть bat файл, который компилирует и запускает программу, в нем нужно изменить путь к файлам csproj и dll выбранной программы.
```
@echo off
echo Compiler...
"C:\Program Files\dotnet\dotnet.exe" build "Менять\Controller.csproj" 

echo Start...
"C:\Program Files\dotnet\dotnet.exe" "менять\Controller\bin\Debug\net7.0\Controller.dll"

echo Done.
pause
```

Для работы программы должны быть установлены [.NET 7.0 и .NET Framework 4.7](https://dotnet.microsoft.com/en-us/download/visual-studio-sdks)


## Программа Controller

Все начальные параметры UDP клиента задаются в полях класса Controller.
Чтобы поменять порт и айпи контроллера, нужно их задать при объявлении 

