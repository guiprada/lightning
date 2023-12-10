rm -rf linux_builds/*

dotnet build --nologo --self-contained true -r linux-x64 -c Debug lightning_interpreter/lightning_interpreter.csproj

mkdir -p linux_builds
mv lightning_interpreter/bin/Debug/net8.0/linux-x64/* linux_builds/

# works but compiles old version
# dotnet publish --nologo -r linux-x64 -c Release -o linux_builds lightning_interpreter/lightning_interpreter.csproj