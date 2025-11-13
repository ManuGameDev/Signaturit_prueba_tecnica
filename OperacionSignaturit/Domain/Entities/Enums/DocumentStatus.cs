namespace Domain.Entities.Enums
{
    /// <summary>
    /// Estados posibles de un documento en el sistema
    /// </summary>
    public enum DocumentStatus
    {
        PENDING,    // Recibido, esperando validación
        VALIDATED,  // Validado, esperando firma
        SIGNED,     // Firmado exitosamente
        REJECTED    // Rechazado por alguna anomalía
    }
}
