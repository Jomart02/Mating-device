@echo off
echo Compiler...
"dotnet" build "%cd%\SensorTwo.csproj" 

echo Start...
"dotnet" "%cd%\bin\Debug\net7.0\SensorTwo.dll"

echo Done.
pause