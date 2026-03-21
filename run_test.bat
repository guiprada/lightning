call clean.bat >nul
cd lightning_programs

echo === Pass 1: --compile (fresh bytecode, saves .ltnc for all modules) ===
..\win_builds\lightning_interpreter.exe --compile tests\test.ltn

echo === Pass 2: warm cache (loads .ltnc, tests round-trip) ===
..\win_builds\lightning_interpreter.exe tests\test.ltn

echo === Pass 3: run compiled top-level bytecode directly ===
..\win_builds\lightning_interpreter.exe tests\test.ltnc

cd ..
