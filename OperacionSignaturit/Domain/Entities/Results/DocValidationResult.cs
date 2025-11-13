namespace Domain.Entities.Results
{
    /// <summary>
    /// Resultado de una operación de validación
    /// </summary>
    public class DocValidationResult
    {
        public bool IsValid { get; private set; }
        public string ErrorMessage { get; private set; }

        private DocValidationResult() { }

        public static DocValidationResult Success() => new() { IsValid = true };

        public static DocValidationResult Failure(string error) => new()
        {
            IsValid = false,
            ErrorMessage = error ?? "Unknown error"
        };
    }
}
