./clean.sh
cd lightning_programs

echo "=== Pass 1: --compile (fresh bytecode, saves .ltnc for all modules) ==="
../linux_builds/lightning_interpreter --compile tests/test.ltn

echo "=== Pass 2: warm cache (loads .ltnc, tests round-trip) ==="
../linux_builds/lightning_interpreter tests/test.ltn

echo "=== Pass 3: run compiled top-level bytecode directly ==="
../linux_builds/lightning_interpreter tests/test.ltnc

cd ..
