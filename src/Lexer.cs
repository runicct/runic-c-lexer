/*
 * MIT License
 * 
 * Copyright (c) 2025 Runic Compiler Toolkit Contributors
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

namespace Runic.C
{
    public abstract class Lexer : ITokenStream
    {
        int _lineIndex = 1;
        public int LineIndex { get { return _lineIndex; } }
        int _chrIndex = 1;
        public int ChrIndex { get { return _chrIndex; } }

        System.IO.StreamReader _reader;
#if NET6_0_OR_GREATER
        protected abstract Token CreateToken(int startLine, int startColumn, int endLine, int endColumn, string? value);
#else
        protected abstract Token CreateToken(int startLine, int startColumn, int endLine, int endColumn, string value);
#endif
        public Lexer(System.IO.StreamReader reader)
        {
            _reader = reader;
        }
        char ReadChar()
        {
            int chr = _reader.Read();
            if (chr == -1) return '\0';
            if (chr == '\n') { _lineIndex++; _chrIndex = 1; }
            else if (chr == '\r') { _chrIndex = 1; }
            else { _chrIndex++; }
            return (char)chr;
        }
        char PeekChar()
        {
            int chr = _reader.Peek();
            if (chr == -1) return '\0';
            return (char)chr;
        }

        /// <summary>
        /// Skip all the whitespaces and return the first character
        /// that is not a whitespace
        /// </summary>
        char SkipWhiteSpaces(out int line, out int column)
        {
            line = _lineIndex;
            column = _chrIndex;
            char chr = ReadChar();
            while (chr != '\0' && chr != '\n' && char.IsWhiteSpace(chr))
            {
                line = _lineIndex;
                column = _chrIndex;
                chr = ReadChar();
            }
            return chr;
        }
        /// <summary>
        /// Assume the first ' has been processed and read a literal char like
        /// 'A'
        /// </summary>
        /// <returns></returns>
        string ReadLiteralChar(bool wide)
        {
            string token = wide ? "L'" : "'";
            while (true)
            {
                char chr = ReadChar();
                switch (chr)
                {
                    case '\0': return token; // That will be an error in the parser
                    case '\'': return token + "'";
                    case '\n': case '\r': return token; // That will be an error in the parser
                    case '\\':
                        char nextChr = ReadChar();
                        switch (nextChr)
                        {
                            case '\0': return token;
                            case '\n': case '\r': return token; // That will be an error in the parser
                        }
                        token += "\\" + nextChr.ToString();
                        break;
                    default:
                        token += chr.ToString();
                        break;
                }
            }
            return token;
        }

        string ReadLiteralString(bool wide)
        {
            string token = wide ? "L\"" : "\"";
            while (true)
            {
                char chr = ReadChar();
                switch (chr)
                {
                    case '\0': return token; // That will be an error in the parser
                    case '\"': return token + "\"";
                    case '\n': case '\r': return token; // That will be an error in the parser
                    case '\\':
                        char nextChr = ReadChar();
                        switch (nextChr)
                        {
                            case '\0': return token;
                            case '\n': case '\r': return token; // That will be an error in the parser
                        }
                        token += "\\" + nextChr.ToString();
                        break;
                    default:
                        token += chr.ToString();
                        break;
                }
            }
            return token;
        }
        string ReadToken(char FirstChr)
        {
            bool numerical = false;
            switch (FirstChr)
            {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    numerical = true;
                    break;
            }
            string Token = FirstChr.ToString();

            while (true)
            {
                char chr = PeekChar();
                switch (chr)
                {
                    case ';':
                    case ',':
                    case '(':
                    case ')':
                    case '[':
                    case ']':
                    case '{':
                    case '}':
                    case '?':
                    case '=':
                    case '+':
                    case '-':
                    case '*':
                    case '/':
                    case '&':
                    case '|':
                    case '%':
                    case '^':
                    case '~':
                    case '!':
                    case '>':
                    case '<':
                    case '#':
                    case ':':
                    case '\'':
                    case '\"':
                    case '\0':
                        return Token;
                    case '.':
                        if (!numerical) return Token;
                        break;
                    default:
                        if (char.IsWhiteSpace(chr)) { return Token; }
                        break;
                }
                Token += ReadChar();
            }
            return Token;
        }
        string ReadSingleLineComment()
        {
            string comment = "//";
            char chr = ReadChar();
            while (true)
            {
                switch (chr)
                {
                    case '\r':
                        switch (PeekChar())
                        {
                            case '\n': return comment;
                        }
                        break;
                    case '\n':
                    case '\0':
                        return comment;
                }
                comment += chr;
                chr = ReadChar();
            }
        }
        string ReadMultiLineComment()
        {
            string comment = "/*";
            char chr = ReadChar();
            while (true)
            {
                switch (chr)
                {
                    case '\0':
                        return comment;
                    case '*':
                        chr = PeekChar();
                        switch (chr)
                        {
                            case '\0': return comment + "*";
                            case '/': ReadChar(); return comment + "*/";
                        }
                        break;
                }
                comment += chr;
                chr = ReadChar();
            }
        }
#if NET6_0_OR_GREATER
        string? ReadIncludeWithChevron()
#else
        string ReadIncludeWithChevron()
#endif
        {
            string token = "<";
            while (true)
            {
                char chr = ReadChar();
                switch (chr)
                {
                    case '\0': return token; // That will be an error in the parser
                    case '>': return token + ">";
                    case '\n': case '\r': return token; // That will be an error in the parser
                    default:
                        token += chr.ToString();
                        break;
                }
            }
            return token;
        }
#if NET6_0_OR_GREATER
        string? ReadNextTokenInternal(out int line, out int column)
#else
        string ReadNextTokenInternal(out int line, out int column)
#endif
        {
            line = _lineIndex;
            column = _chrIndex;
            char chr = ReadChar();
            if (chr == '\0') { return null; }
            switch (chr)
            {
                case '\r':
                    switch (PeekChar())
                    {
                        case '\n': ReadChar(); return "\n";
                        case '\0': return " ";
                        default:
                            if (!char.IsWhiteSpace(chr)) { return " "; }
                            break;
                    }
                    ReadChar();
                    goto case ' ';
                case ' ':
                case '\t':
                    {
                        while (true)
                        {
                            chr = PeekChar();
                            switch (chr)
                            {
                                case '\0':
                                case '\n': return " ";
                                case '\r':
                                    ReadChar();
                                    break;
                                default:
                                    if (!char.IsWhiteSpace(chr)) { return " "; }
                                    ReadChar();
                                    break;
                            }
                        }
                    }
                case '\n':
                    switch (PeekChar())
                    {
                        case '\r': ReadChar(); return "\n";
                        default: return "\n";
                    }
                case ';': return ";";
                case ',': return ",";
                case '(': return "(";
                case ')': return ")";
                case '[': return "[";
                case ']': return "]";
                case '{': return "{";
                case '}': return "}";
                case '?': return "?";
                case '.':
                    switch (PeekChar())
                    {
                        case '.':
                            ReadChar();
                            switch (PeekChar())
                            {
                                case '.': ReadChar(); return "...";
                                default: return "..";
                            }
                        default: return ".";
                    }
                case '=':
                    switch (PeekChar())
                    {
                        case '=': ReadChar(); return "==";
                        default: return "=";
                    }
                case '+':
                    switch (PeekChar())
                    {
                        case '+': ReadChar(); return "++";
                        case '=': ReadChar(); return "+=";
                        default: return "+";
                    }
                case '-':
                    switch (PeekChar())
                    {
                        case '-': ReadChar(); return "--";
                        case '=': ReadChar(); return "-=";
                        case '>': ReadChar(); return "->";
                        default: return "-";
                    }
                case '*':
                    switch (PeekChar())
                    {
                        case '=': ReadChar(); return "*=";
                        default: return "*";
                    }
                case '/':
                    switch (PeekChar())
                    {
                        case '/': ReadChar(); return ReadSingleLineComment();
                        case '*': ReadChar(); return ReadMultiLineComment();
                        case '=': ReadChar(); return "/=";
                        default: return "/";
                    }
                case '&':
                    switch (PeekChar())
                    {
                        case '=': ReadChar(); return "&=";
                        case '&': ReadChar(); return "&&";
                        default: return "&";
                    }
                case '|':
                    switch (PeekChar())
                    {
                        case '=': ReadChar(); return "|=";
                        case '|': ReadChar(); return "||";
                        default: return "|";
                    }
                case '~':
                    switch (PeekChar())
                    {
                        case '=': ReadChar(); return "~=";
                        default: return "~";
                    }
                case '!':
                    switch (PeekChar())
                    {
                        case '=': ReadChar(); return "!=";
                        default: return "!";
                    }
                case '%':
                    switch (PeekChar())
                    {
                        case '=': ReadChar(); return "%=";
                        case '>': ReadChar(); return "%>";
                        case ':':
                            ReadChar();
                            switch (PeekChar())
                            {
                                case '%':
                                    ReadChar();
                                    switch (PeekChar())
                                    {
                                        case ':': ReadChar(); return "%:%:";
                                        default: return "%:%";
                                    }
                                default: return "%:";
                            }
                        default: return "%";
                    }
                case '^':
                    switch (PeekChar())
                    {
                        case '=': ReadChar(); return "^=";
                        default: return "^";
                    }
                case '>':
                    switch (PeekChar())
                    {
                        case '=': ReadChar(); return ">=";
                        case '>':
                            ReadChar();
                            switch (PeekChar())
                            {
                                case '=': ReadChar(); return ">>=";
                                default: return ">>";
                            }
                        default: return ">";
                    }
                case '<':
                    switch (PeekChar())
                    {
                        case '=': ReadChar(); return "<=";
                        case '%': ReadChar(); return "<%";
                        case ':': ReadChar(); return "<:";
                        case '<':
                            ReadChar();
                            switch (PeekChar())
                            {
                                case '=': ReadChar(); return "<<=";
                                default: return "<<";
                            }
                        default: return "<";
                    }
                case '#':
                    switch (PeekChar())
                    {
                        case '#': ReadChar(); return "##";
                        default: return "#";
                    }
                case '\\':
                    switch (PeekChar())
                    {
                        case '\n': ReadChar(); return "\\\n";
                        case '\r':
                            ReadChar();
                            switch (PeekChar())
                            {
                                case '\n': ReadChar(); return "\\\n";
                                default: return "\\r";
                            }
                        default: return "\\";
                    }
                case ':':
                    switch (PeekChar())
                    {
                        case '>': ReadChar(); return ":>";
                        default: return ":";
                    }
                case 'L':
                    switch (PeekChar())
                    {
                        case '\'': ReadChar(); return ReadLiteralChar(true);
                        case '\"': ReadChar(); return ReadLiteralString(true);
                        default: return ReadToken(chr);
                    }
                case '\'': return ReadLiteralChar(false);
                case '\"': return ReadLiteralString(false);
                default: return ReadToken(chr);
            }
        }

        // We need a mini-state machine for processing #include <something.h>
        // as it needs to be processed like a string but without the proper
        // delimiter

        bool _preprocessor = false;
        bool _newLine = true;
        bool _include = false;
#if NET6_0_OR_GREATER
        public string? ReadNextToken(out int line, out int column)
#else
        public string ReadNextToken(out int line, out int column)
#endif
        {
#if NET6_0_OR_GREATER
            string? token = ReadNextTokenInternal(out line, out column);
#else
            string token = ReadNextTokenInternal(out line, out column);
#endif
            if (token == null) { return token; }
            switch (token)
            {
                case " ":
                    return " ";
                case "#":
                    if (_newLine) { _preprocessor = true; }
                    return "#";
                case "\n":
                    _newLine = true;
                    _preprocessor = false;
                    _include = false;
                    return "\n";
                case "include":
                    if (_preprocessor) { _include = true; }
                    return "include";
                case "<":
                    if (_include)
                    {
                        return ReadIncludeWithChevron();
                    }
                    return "<";
                default:
                    _newLine = false;
                    _include = false;
                    return token;
            }
        }

#if NET6_0_OR_GREATER
        public Token? ReadNextToken()
#else
        public Token ReadNextToken()
#endif
        {
            int startLine = 0;
            int startColumn = 0;
#if NET6_0_OR_GREATER
            string? value = ReadNextToken(out startLine, out startColumn);
#else
            string value = ReadNextToken(out startLine, out startColumn);
#endif
            if (value == null) { return null; }
            int endLine = _lineIndex;
            int endColumn = _chrIndex - 1;

            // This could only happen with a new line
            if (endColumn == 0)
            {
                endLine = endLine - 1;
                endColumn = startColumn;
            }
            return CreateToken(startLine, startColumn, endLine, endColumn, value);
        }
    }
}
