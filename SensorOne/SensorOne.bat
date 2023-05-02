@echo off
echo Compiler...
"dotnet" build "%cd%\SensorOne.csproj" 

echo Start...
"dotnet" "%cd%\bin\Debug\net7.0\SensorOne.dll"

echo Done.
pause