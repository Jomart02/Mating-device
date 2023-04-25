@echo off
echo Compiler...
"C:\Program Files\dotnet\dotnet.exe" build "C:\2022-2023\EthernetNavigationSystem\Controller\Controller.csproj" 

echo Start...
"C:\Program Files\dotnet\dotnet.exe" "C:\2022-2023\EthernetNavigationSystem\Controller\bin\Debug\net7.0\Controller.dll"

echo Done.
pause