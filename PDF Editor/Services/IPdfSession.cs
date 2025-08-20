// IPdfSession.cs
using System;
using System.ComponentModel;
using System.IO;
using System.IO.Packaging;
#nullable enable
namespace PDF_Editor.Services
{
    public interface IPdfSession : INotifyPropertyChanged
    {
        string? FilePath { get; }
        string? FileName { get; }
        bool HasFile { get; }

        void Set(string fullPath);
        void Clear();
    }

    public sealed class PdfSession : IPdfSession
    {
        private string? _filePath;
        public string? FilePath
        {
            get => _filePath;
            private set
            {
                if (_filePath == value) return;
                _filePath = value;
                OnPropertyChanged(nameof(FilePath));
                OnPropertyChanged(nameof(FileName));
                OnPropertyChanged(nameof(HasFile));
            }
        }

        public string? FileName => string.IsNullOrEmpty(_filePath) ? null : Path.GetFileName(_filePath);
        public bool HasFile => !string.IsNullOrEmpty(_filePath);

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void Set(string fullPath) => FilePath = fullPath;
        public void Clear() => FilePath = null;
    }
}
