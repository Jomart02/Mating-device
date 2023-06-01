@echo off
echo Compiler...
"dotnet" build "%cd%\MatingDevice.csproj"

echo Start...
"dotnet" "%cd%\bin\Release\net7.0\MatingDevice.dll"

echo Done.
pause