rm -rf linux_builds/*

dotnet publish --nologo --self-contained true -r linux-x64 -c Release lightning_interpreter/lightning_interpreter.csproj

mkdir -p linux_builds
mv lightning_interpreter/bin/Release/net8.0/linux-x64/* linux_builds/

# Copy .NET reference assemblies needed by Roslyn's CSharpCompilation.
# These must live in linux_builds/refs/ (Defaults.Config.AssembliesPath).
# We use the SDK reference packs (not runtime DLLs) so Roslyn has full metadata.
REF_DIR=$(find /usr/lib/dotnet/packs/Microsoft.NETCore.App.Ref -name "System.Runtime.dll" -path "*/net8.0/*" 2>/dev/null | head -1 | xargs dirname)
if [ -n "$REF_DIR" ]; then
    mkdir -p linux_builds/refs
    cp "$REF_DIR/System.Runtime.dll" linux_builds/refs/
    cp "$REF_DIR/System.Console.dll" linux_builds/refs/
    cp "$REF_DIR/System.Core.dll"    linux_builds/refs/
    echo "Roslyn refs copied from $REF_DIR"
else
    echo "WARNING: .NET reference packs not found; Roslyn compile() will fail."
fi

# works but compiles old version
# dotnet publish --nologo -r linux-x64 -c Release -o linux_builds lightning_interpreter/lightning_interpreter.csproj