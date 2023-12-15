using System;

#if DOUBLE
using Float = System.Double;
using Integer = System.Int64;
#else
	using Float = System.Single;
	using Integer = System.Int32;
#endif

namespace lightning
{
    public struct PositionData
    {
        int line;
        int column;
        public PositionData(int p_line, int p_column)
        {
            line = p_line;
            column = p_column;
        }
        public override string ToString()
        {
            return "(" + line + "," + column + ")";
        }
    }
    public enum TokenType
    {
        NEW_LINE,
        // one character tokens.
        LEFT_PAREN,
        RIGHT_PAREN,
        LEFT_BRACE,
        RIGHT_BRACE,
        LEFT_BRACKET,
        RIGHT_BRACKET,
        COMMA,
        MINUS,
        MINUS_EQUAL,
        MINUS_MINUS,
        PLUS,
        PLUS_EQUAL,
        PLUS_PLUS,
        SEMICOLON,
        COLON,
        PIPE,
        SLASH,
        SLASH_EQUAL,
        STAR,
        STAR_EQUAL,

        // one or two character tokens.
        DOT,
        APPEND,
        BANG,
        BANG_EQUAL,
        EQUAL,
        EQUAL_EQUAL,
        GREATER,
        GREATER_EQUAL,
        LESS,
        LESS_EQUAL,

        // literals.
        IDENTIFIER,
        STRING,
        CHAR,
        NUMBER,

        // reserved words.
        XNOR,
        NOR,
        NAND,
        XOR,
        AND,
        CLASS,
        ELSE,
        FALSE,
        FUN,
        FOR,
        IF,
        NIL,
        OR,
        RETURN,
        SUPER,
        THIS,
        TRUE,
        VAR,
        WHILE,
        EOF
    }
    public class Token
    {
        public TokenType Type { get; private set; }

        public PositionData PositionData { get; private set; }
        public Token(TokenType p_Type, int p_line, int p_column)
        {
            //Console.WriteLine(p_type.ToString() + " " + p_Line);
            Type = p_Type;
            PositionData = new PositionData(p_line, p_column);
        }

        public override string ToString()
        {
            return Type.ToString() + " position: " + PositionData.ToString();
        }
    }

    public class TokenString : Token
    {
        public string value;
        public TokenString(TokenType p_Type, int p_line, int p_column, string p_value) : base(p_Type, p_line, p_column)
        {
            value = p_value;
        }
        public override string ToString()
        {
            return Type.ToString() + " string: " + value + " position: " + PositionData.ToString();
        }
    }

    public class TokenChar : Token
    {
        public char value;
        public TokenChar(int p_line, int p_column, char p_value) : base(TokenType.CHAR, p_line, p_column)
        {
            value = p_value;
        }

        public override string ToString()
        {
            return Type.ToString() + " char: " + value.ToString() + " position: " + PositionData.ToString();
        }
    }

    public class TokenNumber : Token
    {
        public Float floatValue;
        public Integer integerValue;
        public Type type;
        public TokenNumber(TokenType p_Type, int p_line, int p_column, String p_value) : base(p_Type, p_line, p_column)
        {
            try
            {
                integerValue = Integer.Parse(p_value);
                type = integerValue.GetType();
            }
            catch (FormatException)
            {
                try
                {
                    floatValue = Float.Parse(p_value);
                    type = floatValue.GetType();
                }
                catch (FormatException)
                {
                    Console.WriteLine("{0}: Bad Format", p_value);
                }
                catch (OverflowException)
                {
                    Console.WriteLine("{0}: Overflow", p_value);
                }
            }
            catch (OverflowException)
            {
                Console.WriteLine("{0}: Overflow", p_value);
            }
        }

        public override string ToString()
        {
            if (type == typeof(Float))
                return Type.ToString() + " Float: " + floatValue.ToString() + " position: " + PositionData.ToString();
            else
                return Type.ToString() + " Integer: " + integerValue.ToString() + " position: " + PositionData.ToString();
        }
    }
}
