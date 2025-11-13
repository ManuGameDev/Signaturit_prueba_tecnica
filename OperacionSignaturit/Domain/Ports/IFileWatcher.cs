namespace Domain.Ports
{
    /// <summary>
    /// Puerto para vigilancia de sistema de archivos
    /// </summary>
    public interface IFileWatcher
    {
        event EventHandler<FileDetectedEventArgs> FileDetected;
        void StartWatching(string folderPath);
        void StopWatching();
    }

    public class FileDetectedEventArgs : EventArgs
    {
        public string FilePath { get; set; }
        public DateTime DetectedAt { get; set; }
    }
}
