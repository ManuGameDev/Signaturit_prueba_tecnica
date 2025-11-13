namespace Domain.Entities.Results
{
    /// <summary>
    /// Resultado de una operación de firmado
    /// </summary>
    public class SigningResult
    {
        public bool IsSuccess { get; private set; }
        public string ErrorMessage { get; private set; }
        public Signature Signature { get; private set; }

        private SigningResult() { }

        public static SigningResult Success(Signature signature) => new()
        {
            IsSuccess = true,
            Signature = signature
        };

        public static SigningResult Failure(string error) => new()
        {
            IsSuccess = false,
            ErrorMessage = error ?? "Unknown error"
        };
    }
}