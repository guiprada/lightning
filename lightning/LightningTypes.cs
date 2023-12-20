#if DOUBLE
	global using Float = System.Double;
	global using Integer = System.Int64;
	global using Operand = System.UInt16;
#else
    global using Float = System.Single;
    global using Integer = System.Int32;
    global using Operand = System.UInt16;
#endif