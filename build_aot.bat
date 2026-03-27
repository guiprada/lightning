if exist win_builds rmdir win_builds /Q/S

dotnet publish --nologo -r win-x64 -c Release -p:PublishAot=true lightning_interpreter\lightning_interpreter.csproj

if not exist .\win_builds mkdir .\win_builds >nul
xcopy lightning_interpreter\bin\Release\net10.0\win-x64\publish\* .\win_builds\ /E/H >nul

if not exist .\win_builds\refs mkdir .\win_builds\refs >nul
xcopy lightning\bin\Release\net10.0\refs\* .\win_builds\refs\ /E/H >nul

echo @"%%~dp0lightning_interpreter.exe" %%* > .\win_builds\lightning_interpreter.bat
