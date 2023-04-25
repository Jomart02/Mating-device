@echo off
echo Compiler...
"C:\Program Files\dotnet\dotnet.exe" build "C:\2022-2023\EthernetNavigationSystem\SensorTwo\SensorTwo.csproj" 

echo Start...
"C:\Program Files\dotnet\dotnet.exe" "C:\2022-2023\EthernetNavigationSystem\SensorTwo\bin\Debug\net7.0\SensorTwo.dll"

echo Done.
pause