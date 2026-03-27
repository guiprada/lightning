if exist win_builds rmdir win_builds /Q/S

@REM Release file locks before touching the win-x64 dirs
taskkill /F /IM lightning_interpreter.exe >nul 2>&1
dotnet build-server shutdown >nul 2>&1
if exist lightning_interpreter\bin\Release\net10.0\win-x64 rmdir /Q/S lightning_interpreter\bin\Release\net10.0\win-x64
if exist lightning_interpreter\obj\Release\net10.0\win-x64 rmdir /Q/S lightning_interpreter\obj\Release\net10.0\win-x64

dotnet publish --nologo --self-contained true -r win-x64 -c Release /p:LinkDuringPublish=true lightning_interpreter\lightning_interpreter.csproj

if not exist .\win_builds mkdir .\win_builds >nul
xcopy lightning_interpreter\bin\Release\net10.0\win-x64\publish\* .\win_builds\ /E/H >nul

if not exist .\win_builds\refs mkdir .\win_builds\refs >nul
xcopy lightning\bin\Release\net10.0\refs\* .\win_builds\refs\ /E/H >nul

echo @"%%~dp0lightning_interpreter.exe" %%* > .\win_builds\lightning_interpreter.bat
