using System;
using System.Collections.Generic;
using System.Text;

#if DOUBLE
    using Number = System.Double;
#else
using Number = System.Single;
#endif

namespace lightning
{
   public enum TokenType {
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
        PLUS,
        PLUS_EQUAL,
        SEMICOLON,
        COLON,
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
        PFOR,
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

        public int Line { get; private set; }
        public Token(TokenType p_type, int p_Line)
        {
            //Console.WriteLine(p_type.ToString() + " " + p_Line);
            Type = p_type;            
            Line = p_Line;
        }

        public override string ToString()
        {
            return Type.ToString() + " line: " + Line.ToString();
        }
    }

    public class TokenString: Token
    {
        public string value;
        public TokenString(TokenType p_Type, int p_Line, string p_value): base(p_Type, p_Line)
        {
            value = p_value;
        }
        public override string ToString()
        {
            return Type.ToString() + " Value: " + value + " line: " + Line.ToString();
        }
    }

    public class TokenNumber: Token
    {
        public Number value;
        public TokenNumber(TokenType p_Type, int p_Line, Number p_value): base(p_Type, p_Line)
        {
            value = p_value;
        }

        public override string ToString()
        {
            return Type.ToString() + " Value: " + value.ToString() + " line: " + Line.ToString();
        }
    }
}
