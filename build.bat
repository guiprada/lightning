if exist win_builds rmdir win_builds /Q/S

dotnet publish --nologo --self-contained true -r win-x64 -c Release /p:LinkDuringPublish=true lightning_interpreter\lightning_interpreter.csproj

if not exist .\win_builds mkdir .\win_builds >nul
xcopy lightning_interpreter\bin\Release\net8.0\win-x64\* .\win_builds\ /E/H >nul

if not exist .\win_builds\refs mkdir .\win_builds\refs >nul
xcopy lightning\bin\Release\net8.0\refs\* .\win_builds\refs\ /E/H >nul

@REM Works but compiles old code
@REM dotnet build -c Release -o win_builds lightning
@REM dotnet publish -r win-x64 -c Release -o win_builds  lightning_interpreter