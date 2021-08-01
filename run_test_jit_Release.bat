call clean.bat >nul
cd lightning_programs
..\lightning_interpreter\bin\Release\netcoreapp5.0\lightning_interpreter.exe .\tests\test.ltn
cd ..