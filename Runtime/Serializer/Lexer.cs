using System.Collections.Generic;
using System.IO;

/// <summary>
/// The Lexer stores a list of tokens. Often known as the Tokenizer.
/// </summary>
/// <remarks>
/// The Lexer creates tokens by adding characters together to form strings. It will add the string/char to the token list once a new delimiter has been detected.
/// </remarks>

namespace Visave {
    public static class Lexer
    {
        #region Members
        public class Token {
            public Type m_type { get; }
            public string m_value { get; }
            public Token(Type type, string value) { m_type = type; m_value = value; }
            public enum Type {
                // Delimiters
                DELIMITER,

                // Keywords
                OBJECT, END_OBJECT, COMPONENT, PROPERTY, GROUP, END,

                // Values
                VALUE,
            };
        };
        private static List<Token> sm_tokens = new List<Token>();
        private static readonly char[] DELIMITERS = { '.', '"', '>' };
        private static int sm_tokenIndex = 0;
        #endregion

        // ========================================================================================================================= //

        #region Getters & Setters

        // TEMP FUNC \/
        public static List<Token> Tokens() { return sm_tokens; }
        // TEMP FUNC ^

        public static Token Advance() { return sm_tokens[sm_tokenIndex++]; }
        public static Token Peak() { return sm_tokens[sm_tokenIndex]; }
        public static Token PeakNext() { return sm_tokens[sm_tokenIndex + 1]; }
        public static bool PeakType(Lexer.Token.Type type)
        {
            if (sm_tokenIndex + 1 >= sm_tokens.Count) { return false; }
            if (sm_tokens[sm_tokenIndex].m_type == type) { return true; }
            return false;
        }
        public static bool IsEnd() { return sm_tokenIndex + 1 >= sm_tokens.Count || sm_tokens[sm_tokenIndex].m_type == Token.Type.END_OBJECT; }

        private static bool IsDelimiter(char c) { return c == DELIMITERS[0]  || c == DELIMITERS[1] || c == DELIMITERS[2]; }
        private static void ResetTokens() { sm_tokens.Clear(); sm_tokenIndex = 0; }
        private static Token.Type FindType(string typeName)
        {
            if (typeName == Token.Type.OBJECT.ToString()) { return Token.Type.OBJECT; }
            if (typeName == Token.Type.GROUP.ToString()) { return Token.Type.GROUP; }
            if (typeName == Token.Type.COMPONENT.ToString()) { return Token.Type.COMPONENT; }
            if (typeName == Token.Type.END.ToString()) { return Token.Type.END; }
            if (typeName == Token.Type.END_OBJECT.ToString()) { return Token.Type.END_OBJECT; }
            return Token.Type.PROPERTY;
        }
        #endregion

        // ========================================================================================================================= //

        #region Tokenize Methods - Create a token list from the input passed in
        /* 'TokenizeFile' is the entry point for tokenizing a file - From here, the Lexer loops through the data and creates the token list. */
        public static void TokenizeFile(FileInfo file)
        {
            using StreamReader reader = new StreamReader(file.FullName);
            // Reset properties for reading a new file
            ResetTokens();

            // Read each line in file
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                if (string.IsNullOrEmpty(line)) continue;
                
                TokenizeLine(line);
            }
        }

        /* 'TokenizeLine' is where a single line is tokenized. Each line is passed in from 'TokenizeFile'. */
        private static void TokenizeLine(string line)
        {
            // Read the line
            string currentToken = string.Empty;

            // Find each
            bool readingVar = false;
            foreach (char c in line)
            {
                if (char.IsWhiteSpace(c) && !readingVar) { continue; }

                if (IsDelimiter(c))
                {
                    // Check delimiter type
                    if (c == DELIMITERS[0] && !readingVar)         // .
                    {
                        // Clear current token to read new keyword
                        currentToken = string.Empty;
                        //sm_tokens.Add(new Token(Token.Type.DELIMITER, c.ToString()));
                        continue;
                    }
                    else if (c == DELIMITERS[1])    // "
                    {
                        // Add current token if readingVar is true (Meaning it has read the data)
                        if (readingVar) {
                            sm_tokens.Add(new Token(Token.Type.VALUE, currentToken));
                            currentToken= string.Empty;
                        }
                        readingVar = !readingVar;
                        continue;
                    } else if (c == DELIMITERS[2] && !readingVar)  // >
                    {
                        // Add previous token (From . delimiter)
                        sm_tokens.Add(new Token(FindType(currentToken), currentToken));
                        currentToken = string.Empty;
                        continue;
                    }
                }

                // Add to current token
                currentToken += c;
            }
            // Add final token from line
            if (currentToken.Length > 0) { sm_tokens.Add(new Token(FindType(currentToken), currentToken)); }
        }
        #endregion
    }
}