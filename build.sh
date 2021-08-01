rm -rf linux_builds/*
# dotnet build -c Release -o linux_builds/ lightning
dotnet publish --nologo --self-contained true -r linux-x64 -c Release /p:LinkDuringPublish=true lightning_interpreter/lightning_interpreter.csproj

mkdir -p linux_builds
mv lightning_interpreter/bin/netcoreapp5.0/* linux_builds/
