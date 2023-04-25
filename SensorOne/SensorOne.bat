@echo off
echo Compiler...
"C:\Program Files\dotnet\dotnet.exe" build "C:\2022-2023\EthernetNavigationSystem\SensorOne\SensorOne.csproj" 

echo Start...
"C:\Program Files\dotnet\dotnet.exe" "C:\2022-2023\EthernetNavigationSystem\SensorOne\bin\Debug\net7.0\SensorOne.dll"

echo Done.
pause