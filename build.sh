rm -rf linux_builds/*
# dotnet build -c Release -o linux_builds/ lightning
dotnet publish --nologo -r linux-x64 -c Release lightning_interpreter/lightning_interpreter.csproj

mkdir -p linux_builds
mv lightning_interpreter/bin/Release/netcoreapp5.0/linux-x64/* linux_builds/
