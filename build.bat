if exist win_builds rmdir win_builds /Q/S

@REM Kill any running instance so Windows releases the file lock before publish
taskkill /F /IM lightning_interpreter.exe >nul 2>&1

@REM CreateAppHost writes to obj\ (AppHostIntermediatePath), not bin\.
@REM Windows keeps the previous apphost.exe memory-mapped so we must delete
@REM both intermediate directories to avoid the IOException on next publish.
if exist lightning_interpreter\bin\Release\net8.0\win-x64 rmdir lightning_interpreter\bin\Release\net8.0\win-x64 /Q/S
if exist lightning_interpreter\obj\Release\net8.0\win-x64 rmdir lightning_interpreter\obj\Release\net8.0\win-x64 /Q/S

dotnet publish --nologo --self-contained true -r win-x64 -c Release /p:LinkDuringPublish=true lightning_interpreter\lightning_interpreter.csproj

if not exist .\win_builds mkdir .\win_builds >nul
xcopy lightning_interpreter\bin\Release\net8.0\win-x64\* .\win_builds\ /E/H >nul

if not exist .\win_builds\refs mkdir .\win_builds\refs >nul
xcopy lightning\bin\Release\net8.0\refs\* .\win_builds\refs\ /E/H >nul

@REM Works but compiles old code
@REM dotnet build -c Release -o win_builds lightning
@REM dotnet publish -r win-x64 -c Release -o win_builds  lightning_interpreter