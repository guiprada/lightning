using System;

namespace lightningExceptions
{
	class Exceptions
	{
		// Compiler
		public static Exception compiler_error = new Exception("Compilation Error!");

		// VM
		public static Exception non_value = new Exception("Trying to assign a non-value!");
		public static Exception created_void = new Exception("Void ResultUnit created!");
	}
}