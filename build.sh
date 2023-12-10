rm -rf linux_builds/*

dotnet publish --nologo --self-contained true -r linux-x64 -c Release lightning_interpreter/lightning_interpreter.csproj

mkdir -p linux_builds
mv lightning_interpreter/bin/Release/net8.0/linux-x64/* linux_builds/

# works but compiles old version
# dotnet publish --nologo -r linux-x64 -c Release -o linux_builds lightning_interpreter/lightning_interpreter.csproj