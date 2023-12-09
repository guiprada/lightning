call clean.bat >nul
cd lightning_programs
..\lightning_interpreter\bin\Release\net8.0\lightning_interpreter.exe .\tests\test.ltn
cd ..