if exist win_builds rmdir win_builds /Q/S

@REM Kill any running instance so Windows releases the file lock before publish
taskkill /F /IM lightning_interpreter.exe >nul 2>&1

@REM The dotnet background build server (MSBuild / VBCSCompiler) keeps the
@REM previous apphost.exe memory-mapped between builds, causing CreateAppHost
@REM to fail with IOException.  Shut it down first, then clean intermediates.
dotnet build-server shutdown >nul 2>&1
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