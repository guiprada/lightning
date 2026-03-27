@REM The dotnet background build server (MSBuild / VBCSCompiler) keeps the
@REM previous apphost.exe memory-mapped between builds, causing CreateAppHost
@REM to fail with IOException.  Shut it down first, then clean intermediates.
dotnet build-server shutdown >nul 2>&1

@REM Kill any running instance so Windows releases the file lock
taskkill /F /IM lightning_interpreter.exe >nul 2>&1

@REM CreateAppHost writes to obj\ (AppHostIntermediatePath), not bin\.
@REM Windows keeps the previous apphost.exe memory-mapped so we must delete
@REM both intermediate directories to avoid the IOException on next publish.
if exist lightning_interpreter\bin\Release\net10.0\win-x64 rmdir lightning_interpreter\bin\Release\net10.0\win-x64 /Q/S
if exist lightning_interpreter\obj\Release\net10.0\win-x64 rmdir lightning_interpreter\obj\Release\net10.0\win-x64 /Q/S

cd lightning_programs
	if exist *.ast del /q *.ast
	if exist *.tokens del /q *.tokens
	if exist *.log del /q *.log
	if exist *.chunk del /q *.chunk
	if exist *.out del /q *.out

	cd tests
		if exist *.ast del /q *.ast
		if exist *.tokens del /q *.tokens
		if exist *.log del /q *.log
		if exist *.chunk del /q *.chunk
		if exist *.out del /q *.out
	cd ..
cd ..
