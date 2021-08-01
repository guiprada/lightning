﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

#if DOUBLE
    using Float = System.Double;
    using Integer = System.Int64;
#else
    using Float = System.Single;
    using Integer = System.Int32;
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

        private char[] source;
        private string moduleName;
        private int line;
        private int column;
        private int start;
        private int current;
        private List<Token> tokens;
        private bool hasScanned;
        public bool HasScanned { get{ return hasScanned; } }
        private List<string> errors;
        public List<string> Errors { get{ return errors; } }
        public List<Token> Tokens{
            get
            {
                if (hasScanned == false)
                {
                    try{
                        ScanTokens();
                        PrintErrors();
                        if (errors.Count > 0){
                            return null;
                        }else{
                            hasScanned = true;
                            using (System.IO.StreamWriter file = new System.IO.StreamWriter(Path.ToPath(moduleName) + ".tokens", false)){
                                foreach (Token token in tokens)
                                    file.WriteLine(token.ToString());
                            }
                        }
                    }catch (Exception e){
                        Console.WriteLine("Scanning broke the runtime, check out" + System.IO.Path.DirectorySeparatorChar + Path.ToPath(moduleName) + "_scanner.log!");
                        Logger.LogNew(e.ToString(), Path.ToPath(moduleName) + "_scanner.log");
                        PrintErrors();
                        return null;
                    }
                }
                return tokens;
            }}

        public Scanner(string p_input, string p_moduleName)
        {
            source = p_input.ToCharArray();
            moduleName = p_moduleName;
            hasScanned = false;
            errors = new List<string>();
            line = 1;
            column = 0;
            start = 0;
            current = 0;
        }

        private void PrintErrors(){
            if (errors.Count > 0)
                Console.WriteLine("Scanning had errors on module: " + moduleName);
                    foreach(string error in errors)
                        Console.WriteLine(error);
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

            tokens.Add(new Token(TokenType.EOF, line, column));
            return tokens;

        }

        private void ScanToken()
        {
            char c = Advance();;
            char[] new_line = Environment.NewLine.ToCharArray();
            switch (c)
            {
                case ' ':
                case '\r':
                case '\t':
                    // Ignore whitespace.
                    break;
                case '\n':
                    line++;
                    column = 0;
                    while (IsWhiteSpace(Peek()) )
                        Advance();
                    if(Peek() == '(')
                        Error("Parentheses can not be used in the beggining of a line!");
                    break;
                case '(': tokens.Add(new Token(TokenType.LEFT_PAREN, line, column)); break;
                case ')': tokens.Add(new Token(TokenType.RIGHT_PAREN, line, column)); break;
                case '{': tokens.Add(new Token(TokenType.LEFT_BRACE, line, column)); break;
                case '}': tokens.Add(new Token(TokenType.RIGHT_BRACE, line, column)); break;
                case '[': tokens.Add(new Token(TokenType.LEFT_BRACKET, line, column)); break;
                case ']': tokens.Add(new Token(TokenType.RIGHT_BRACKET, line, column)); break;
                case ',': tokens.Add(new Token(TokenType.COMMA, line, column)); break;
                case '-':
                    if (Match('='))
                    {
                        tokens.Add(new Token(TokenType.MINUS_EQUAL, line, column));
                    }
                    else if (Match('-'))
                    {
                        tokens.Add(new Token(TokenType.MINUS_MINUS, line, column));
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.MINUS, line, column));
                    }
                    break;
                case '+':
                    if (Match('='))
                    {
                        tokens.Add(new Token(TokenType.PLUS_EQUAL, line, column));
                    }
                    else if (Match('+'))
                    {
                        tokens.Add(new Token(TokenType.PLUS_PLUS, line, column));
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.PLUS, line, column));
                    }
                    break;
                case ';': tokens.Add(new Token(TokenType.SEMICOLON, line, column)); break;
                case ':': tokens.Add(new Token(TokenType.COLON, line, column)); break;
                case '|': tokens.Add(new Token(TokenType.PIPE, line, column)); break;
                case '\\': tokens.Add(new Token(TokenType.FUN, line, column)); break;
                case '*':
                    tokens.Add(Match('=') ? new Token(TokenType.STAR_EQUAL, line, column) : new Token(TokenType.STAR, line, column));
                    break;
                case '.':
                    tokens.Add(Match('.') ? new Token(TokenType.APPEND, line, column) : new Token(TokenType.DOT, line, column));
                    break;
                case '!':
                    tokens.Add(Match('=') ? new Token(TokenType.BANG_EQUAL, line, column) : new Token(TokenType.BANG, line, column));
                    break;
                case '=':
                    tokens.Add(Match('=') ? new Token(TokenType.EQUAL_EQUAL, line, column) : new Token(TokenType.EQUAL, line, column));
                    break;
                case '<':
                    tokens.Add(Match('=') ? new Token(TokenType.LESS_EQUAL, line, column) : new Token(TokenType.LESS, line, column));
                    break;
                case '>':
                    tokens.Add(Match('=') ? new Token(TokenType.GREATER_EQUAL, line, column) : new Token(TokenType.GREATER, line, column));
                    break;
                case '/':
                    if (Match('/'))
                    {
                        // A comment goes until the end of the line.
                        while (Peek() != '\n' && !IsAtEnd()) Advance();
                    }
                    else if (Match('='))
                    {
                        tokens.Add(new Token(TokenType.SLASH_EQUAL, line, column));
                    }
                    else
                    {
                        tokens.Add( new Token(TokenType.SLASH, line, column));
                    }
                    break;
                case '"': tokens.Add(new TokenString(TokenType.STRING, line, column, ReadString('"'))); break;
                case '\'':
                    tokens.Add(new TokenChar(line, column, ReadChar()));
                    break;
                case '#':
                    tokens.Add(new TokenString(TokenType.STRING, line, column, ReadString('#'))); break;
                default:
                    if (IsDigit(c))
                    {
                        tokens.Add(new TokenNumber(TokenType.NUMBER, line, column, ReadNumber()));
                    }
                    else if (IsAlpha(c))
                    {
                        string maybe_identifier = ReadIdentifier();
                        if (keywords.ContainsKey(maybe_identifier))// it is a reserved word
                        {
                            tokens.Add(new Token(keywords[maybe_identifier], line, column));
                        }
                        else
                        {
                            tokens.Add(new TokenString(TokenType.IDENTIFIER, line, column, maybe_identifier));
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
            column++;
            return source[current - 1];
        }

        private bool Match(char p_expected)
        {
            if (IsAtEnd()) return false;
            if (source[current] != p_expected) return false;

            current++;
            column++;
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

        private bool IsWhiteSpace(char p_c){
            return (p_c == ' ' || p_c == '\t');
        }

        private bool IsDigit(char p_c)
        {
            return (p_c >= '0' && p_c <= '9');
        }

        private bool IsAlpha(char p_c)
        {
            return (p_c >= 'a' && p_c <= 'z') ||
                    (p_c >= 'A' && p_c <= 'Z') ||
                    p_c == '_';
        }

        private bool IsAlphaNumeric(char p_c)
        {
            return IsAlpha(p_c) || IsDigit(p_c);
        }

        private String ReadNumber()
        {
            while (IsDigit(Peek())) Advance();

            // Look for a fractional part.
            if ( (Peek() == '.') && (IsDigit(PeekNext())))
            {
                // Consume the "."
                Advance();

                while (IsDigit(Peek())) Advance();
            }
            return new string(source, start, current - start);
        }

        private string ReadIdentifier()
        {
            while (IsAlphaNumeric(Peek())) Advance();

            return new string(source, start, current - start);
        }

        private char ReadChar()
        {
            char next_char = Advance();
            string this_string = "";
            while(next_char != '\''){
                if(next_char == '\\' && Peek() == '\''){
                    this_string += next_char;
                    this_string += Advance();
                }else{
                    this_string += next_char;
                }
                next_char = Advance();
            }
            string this_unescaped_string = Regex.Unescape(this_string);
            if(this_unescaped_string.Length > 1){
                Error("Trying to declare a Char literal with more than one char! " + this_unescaped_string);
            }

            return this_unescaped_string.ToCharArray()[0];
        }


        private string ReadString(char p_terminator)
        {
            while (Peek() != p_terminator && !IsAtEnd())
            {
                if (Peek() == '\n'){
                    line++;
                    column = 0;
                }else if (Peek() == '\\' && PeekNext() == p_terminator){// scaping terminator
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


        private void Error(string p_msg)
        {
            errors.Add(p_msg + " on line: " + line);
        }

    }
}
