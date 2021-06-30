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
    public class Scanner
    {
        private static Dictionary<String, TokenType> keywords = new Dictionary<string, TokenType>()
        {
            {"else", TokenType.ELSE},
            {"false", TokenType.FALSE},
            {"for", TokenType.FOR},
            {"function", TokenType.FUN},
            {"if", TokenType.IF},
            {"null", TokenType.NIL},
            {"return", TokenType.RETURN},
            {"true", TokenType.TRUE},
            {"var", TokenType.VAR},
            {"while", TokenType.WHILE},
            {"and", TokenType.AND},
            {"or", TokenType.OR},
            {"xor", TokenType.XOR},
            {"nand", TokenType.NAND},
            {"nor", TokenType.NOR},
            {"xnor", TokenType.XNOR},
        };

        char[] source;
        int line;
        int start;
        int current;

        public List<string> Errors { get; private set;}

        List<Token> tokens;
        bool hasScanned;
        public List<Token> Tokens{
            get
            {
                if (hasScanned == false)
                {
                    ScanTokens();
                    hasScanned = true;
                }
                return tokens;
            }}

        public Scanner(string input)
        {
            source = input.ToCharArray();
            hasScanned = false;
            Errors = new List<string>();
            line = 1;
            start = 0;
            current = 0;
        }

        private List<Token> ScanTokens()
        {
            tokens = new List<Token>();

            while (!IsAtEnd())
            {
                // We are at the beginning of the next lexeme.
                start = current;
                ScanToken();
            }

            tokens.Add(new Token(TokenType.EOF, line));
            return tokens;

        }

        private void ScanToken()
        {
            char c = Advance();
            switch (c)
            {
                case ' ':
                case '\r':
                case '\t':
                    // Ignore whitespace.
                    break;
                case '\n':
                    line++;
                    break;
                case '(': tokens.Add(new Token(TokenType.LEFT_PAREN, line)); break;
                case ')': tokens.Add(new Token(TokenType.RIGHT_PAREN, line)); break;
                case '{': tokens.Add(new Token(TokenType.LEFT_BRACE, line)); break;
                case '}': tokens.Add(new Token(TokenType.RIGHT_BRACE, line)); break;
                case '[': tokens.Add(new Token(TokenType.LEFT_BRACKET, line)); break;
                case ']': tokens.Add(new Token(TokenType.RIGHT_BRACKET, line)); break;
                case ',': tokens.Add(new Token(TokenType.COMMA, line)); break;
                case '-':
                    if (Match('='))
                    {
                        tokens.Add(new Token(TokenType.MINUS_EQUAL, line));
                    }
                    else if (Match('-'))
                    {
                        tokens.Add(new Token(TokenType.MINUS_MINUS, line));
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.MINUS, line));
                    }
                    break;
                case '+':
                    if (Match('='))
                    {
                        tokens.Add(new Token(TokenType.PLUS_EQUAL, line));
                    }
                    else if (Match('+'))
                    {
                        tokens.Add(new Token(TokenType.PLUS_PLUS, line));
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.PLUS, line));
                    }
                    break;
                case ';': tokens.Add(new Token(TokenType.SEMICOLON, line)); break;
                case ':': tokens.Add(new Token(TokenType.COLON, line)); break;
                case '*':
                    tokens.Add(Match('=') ? new Token(TokenType.STAR_EQUAL, line) : new Token(TokenType.STAR, line));
                    break;
                case '.':
                    tokens.Add(Match('.') ? new Token(TokenType.APPEND, line) : new Token(TokenType.DOT, line));
                    break;
                case '!':
                    tokens.Add(Match('=') ? new Token(TokenType.BANG_EQUAL, line) : new Token(TokenType.BANG, line));
                    break;
                case '=':
                    tokens.Add(Match('=') ? new Token(TokenType.EQUAL_EQUAL, line) : new Token(TokenType.EQUAL, line));
                    break;
                case '<':
                    tokens.Add(Match('=') ? new Token(TokenType.LESS_EQUAL, line) : new Token(TokenType.LESS, line));
                    break;
                case '>':
                    tokens.Add(Match('=') ? new Token(TokenType.GREATER_EQUAL, line) : new Token(TokenType.GREATER, line));
                    break;
                case '/':
                    if (Match('/'))
                    {
                        // A comment goes until the end of the line.
                        while (Peek() != '\n' && !IsAtEnd()) Advance();
                    }
                    else if (Match('='))
                    {
                        tokens.Add(new Token(TokenType.SLASH_EQUAL, line));
                    }
                    else
                    {
                        tokens.Add( new Token(TokenType.SLASH, line));
                    }
                    break;
                case '"': tokens.Add(new TokenString(TokenType.STRING, line, ReadString('"'))); break;
                case '\'': tokens.Add(new TokenString(TokenType.STRING, line, ReadString('\''))); break;
                default:
                    if (IsDigit(c))
                    {
                        tokens.Add(new TokenNumber(TokenType.NUMBER, line, ReadNumber()));
                    }
                    else if (IsAlpha(c))
                    {
                        string maybe_identifier = ReadIdentifier();
                        if (keywords.ContainsKey(maybe_identifier))// it is a reserved word
                        {
                            tokens.Add(new Token(keywords[maybe_identifier], line));
                        }
                        else
                        {
                            tokens.Add(new TokenString(TokenType.IDENTIFIER, line, maybe_identifier));
                        }
                    }
                    else
                    {
                        Error("Unexpected character.");
                    }
                    break;
            }
        }

        private char Advance()
        {
            current++;
            return source[current - 1];
        }

        private bool Match(char expected)
        {
            if (IsAtEnd()) return false;
            if (source[current] != expected) return false;

            current++;
            return true;
        }

        private char Peek()
        {
            if (IsAtEnd()) return '\0';
            return source[current];
        }

        private char PeekNext()
        {
            if (current + 1 >= source.Length) return '\0';
            return source[current+1];
        }

        private bool IsAtEnd()
        {
            bool is_end = current >= source.Length;
            return is_end;
        }

        private bool IsDigit(char c)
        {
            return (c >= '0' && c <= '9');
        }

        private bool IsAlpha(char c)
        {
            return (c >= 'a' && c <= 'z') ||
                    (c >= 'A' && c <= 'Z') ||
                    c == '_';
        }

        private bool IsAlphaNumeric(char c)
        {
            return IsAlpha(c) || IsDigit(c);
        }

        private Number ReadNumber()
        {
            while (IsDigit(Peek())) Advance();

            // Look for a fractional part.
            if ( (Peek() == '.') && (IsDigit(PeekNext())))
            {
                // Consume the "."
                Advance();

                while (IsDigit(Peek())) Advance();
            }
            return Number.Parse(new string(source, start, current - start));
        }

        private string ReadIdentifier()
        {
            while (IsAlphaNumeric(Peek())) Advance();

            return new string(source, start, current - start);
        }

        private string ReadString(char terminator)
        {
            while (Peek() != terminator && !IsAtEnd())
            {
                if (Peek() == '\n') line++;
                else if (Peek() == '\\' && PeekNext() == terminator) // scaping terminator
                {
                    Advance();
                }
                Advance();
            }

            if (IsAtEnd())
            {
                Error("Unterminated string.");
                return null;
            }

            // The closing ".
            Advance();

            // Trim the surrounding quotes.
            //int this_lenght = (current - 1) - (start + 1);
            String new_string = new string(source, start + 1, current - start - 2);

            return new_string;
        }


        private void Error(string msg)
        {

            Errors.Add(msg + " on line: " + line);
        }

    }
}
