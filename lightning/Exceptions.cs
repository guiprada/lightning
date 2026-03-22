using System;

namespace lightningExceptions
{
	class Exceptions
	{
		public static Exception empty_stack = new Exception("Stack is empty");
		public static Exception io_error = new Exception("IO Error!");
		public static Exception compiler_error = new Exception("Compilation Error!");
		public static Exception scanner_error = new Exception("Scanning Error!");
		public static Exception parser_error = new Exception("Parsing Error!");
		public static Exception code_execution_error = new Exception("Code execution was not OK!");
		public static Exception wrong_type = new Exception("Wrong type!");
		public static Exception unknown_type = new Exception("Unknown type!");
		public static Exception unknown_operator = new Exception("Unknown operator!");
		public static Exception can_not_convert = new Exception("Could not convert!");
		public static Exception can_not_compare = new Exception("Could not compare!");
		public static Exception can_not_create = new Exception("Could not create!");
		public static Exception not_found = new Exception("Value not found!");
		public static Exception not_supported = new Exception("Operation not supported!");
		public static Exception created_void = new Exception("Void ResultUnit created!");
		public static Exception out_of_bounds = new Exception("Out of bounds!");
		public static Exception non_value_assign = new Exception("Trying to assign a non-value!");
		public static Exception extension_table_has_extension_table = new Exception("Extension Table has an Extention Table!");
		public static Exception can_not_override_extension_table = new Exception("Table already has an Extention Table!");
	}
}