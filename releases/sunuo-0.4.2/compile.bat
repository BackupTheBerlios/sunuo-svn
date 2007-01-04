@ECHO OFF

set FRAMEWORK=%windir%\Microsoft.NET\Framework\v1.1.4322
REM set FRAMEWORK=%windir%\Microsoft.NET\Framework64\v2.0.50727

mkdir build

cd src
%FRAMEWORK%\csc.exe /nologo /debug:full /out:..\build\SunUO.exe /lib:..\lib /r:log4net.dll /recurse:*.cs

cd ..\util
%FRAMEWORK%\csc.exe /nologo /debug:full /out:..\build\UOGQuery.exe UOGQuery.cs

cd ..

PAUSE
