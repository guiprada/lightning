if (Test-Path test_many.log)
{
	Remove-Item test_many.log
}
foreach($i in 1 .. 500){.\run_test.bat | Out-File -Append -FilePath test_many.log}