if exist win_builds\* del /q win_builds\*

@REM dotnet build -c Release -o win_builds lightning
dotnet publish -r win-x64 -c Release -o win_builds  lightning_interpreter


@REM dotnet nuget locals all --clear
@REM dotnet nuget locals global-packages -c

@REM dotnet build --nologo -c Release lightning\lightning.csproj
@REM dotnet build --nologo -c Release lightning_interpreter\lightning_interpreter.csproj

@REM dotnet publish --nologo --self-contained true -r win-x64 -c Release /p:LinkDuringPublish=true lightning_interpreter\lightning_interpreter.csproj

@REM if not exist .\win_builds mkdir .\win_builds
@REM move lightning_interpreter\bin\Release\netcoreapp5.0\win-x64\* .\win_builds\ >nul