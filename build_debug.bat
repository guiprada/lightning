if exist win_builds\* del /q win_builds\*

dotnet build --nologo --self-contained true -r win-x64 -c Debug /p:LinkDuringPublish=true lightning_interpreter\lightning_interpreter.csproj

if not exist .\win_builds mkdir .\win_builds
move lightning_interpreter\bin\Debug\net8.0\win-x64\* .\win_builds\ >nul


@REM Works but compiles old code
@REM dotnet build -c Release -o win_builds lightning
@REM dotnet publish -r win-x64 -c Release -o win_builds  lightning_interpreter