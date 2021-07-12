dotnet build -c Release -o linux_builds/ lightning
dotnet publish -r linux-x64 -c Release -o linux_builds/ lightning_interpreter
