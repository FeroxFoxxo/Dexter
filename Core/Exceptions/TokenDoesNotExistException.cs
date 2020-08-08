using System;

namespace Dexter.Core.Exceptions {
    public class TokenDoesNotExistException : Exception {
        public TokenDoesNotExistException() : base("Token is set to null or is empty! Please provide a token before you launch Dexter.") { }
    }
}
