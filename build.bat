if exist win_builds rmdir win_builds /Q/S

dotnet publish --nologo -c Release -p:UseAppHost=false lightning_interpreter\lightning_interpreter.csproj

if not exist .\win_builds mkdir .\win_builds >nul
xcopy lightning_interpreter\bin\Release\net10.0\publish\* .\win_builds\ /E/H >nul

if not exist .\win_builds\refs mkdir .\win_builds\refs >nul
xcopy lightning\bin\Release\net10.0\refs\* .\win_builds\refs\ /E/H >nul

@REM Drop a .bat launcher so the interpreter can be called without 'dotnet'
echo @dotnet "%%~dp0lightning_interpreter.dll" %%* > .\win_builds\lightning_interpreter.bat

@REM Works but compiles old code
@REM dotnet build -c Release -o win_builds lightning
@REM dotnet publish -r win-x64 -c Release -o win_builds  lightning_interpreter