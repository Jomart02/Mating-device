@echo off
echo Compiler...
"dotnet" build "%cd%\Controller.csproj"

echo Start...
"dotnet" "%cd%\bin\Debug\net7.0\Controller.dll"

echo Done.
pause