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
