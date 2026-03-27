# Kill any running instance so the OS releases the file lock
pkill -f lightning_interpreter 2>/dev/null || true

# Delete win-x64 intermediate dirs (memory-mapped apphost.exe lock)
rm -rf lightning_interpreter/bin/Release/net10.0/win-x64
rm -rf lightning_interpreter/obj/Release/net10.0/win-x64

cd lightning_programs
	rm -f  *.ast
	rm -f  *.tokens
	rm -f  *.log
	rm -f  *.chunk
	rm -f  *.out

	cd tests
		rm -f  *.ast
		rm -f  *.tokens
		rm -f  *.log
		rm -f  *.chunk
		rm -f  *.out
	cd ..
cd ..
