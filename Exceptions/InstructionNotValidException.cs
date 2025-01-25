namespace Chip8.Exceptions
{
    public class InstructionNotValidException : NotImplementedException
    {
        public InstructionNotValidException(){}
        public InstructionNotValidException(string message) : base(message){}
        public InstructionNotValidException(string message, Exception innerException) : base(message, innerException){}
        
    }
}