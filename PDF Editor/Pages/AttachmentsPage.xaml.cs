// Pages/AttachmentsPage.xaml.cs
#nullable enable
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using PDF_Editor.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
// iText 9.2.0
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Filespec;
using System.Drawing.Imaging;
using Microsoft.Web.WebView2.Core;
using System.Threading.Tasks;

namespace PDF_Editor.Pages
{
    public sealed class AttachmentItem
    {
        public string Key { get; set; } = "";
        public string Name { get; set; } = "";
        public long SizeBytes { get; set; }
        public string SizeDisplay =>
            SizeBytes < 0 ? "" :
            (SizeBytes >= 1_048_576 ? $"{SizeBytes / 1_048_576.0:0.##} MB"
            : SizeBytes >= 1024 ? $"{SizeBytes / 1024.0:0.#} KB"
            : $"{SizeBytes} B");
    }

    public partial class AttachmentsPage : Page, INotifyPropertyChanged
    {
        private readonly IPdfSession _pdf;
        public ObservableCollection<AttachmentItem> Attachments { get; } = new();

        private string? _fileName;
        public string? FileName { get => _fileName; private set { _fileName = value; OnPropertyChanged(nameof(FileName)); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public AttachmentsPage() : this(App.AppHost.Services.GetRequiredService<IPdfSession>()) { }
        public AttachmentsPage(IPdfSession pdf)
        {
            _pdf = pdf;
            InitializeComponent();

            // Bind: show filename from session + the attachment list
            DataContext = this;
            FileName = _pdf.FileName;
            LoadAttachments();


            // 1) When the page is loaded, init WebView2 then show preview
            this.Loaded += async (_, __) =>
            {
                await EnsurePreviewReady();
                await LoadFirstPagePreviewAsync(); // fire once the control is ready
            };

            // 2) Refresh filename, attachments, and preview when the session file changes
            _pdf.PropertyChanged += async (_, e) =>
            {
                if (e.PropertyName == nameof(_pdf.FilePath) || e.PropertyName == nameof(_pdf.FileName))
                {
                    await Dispatcher.InvokeAsync(async () =>
                    {
                        FileName = _pdf.FileName;
                        LoadAttachments();
                        await EnsurePreviewReady();
                        await LoadFirstPagePreviewAsync();
                    });
                }
            };
        }

        private void SelectPdf_Click(object sender, RoutedEventArgs e) => SelectPdf();
        private void AddAttachment_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select a file to attach",
                Filter = "All Files|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    AddAttachment(_pdf.FilePath!, dlg.FileName); // call your method
                    LoadAttachments(); // refresh the list
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adding attachment: {ex.Message}",
                        "Attachments", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteAttachment_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as FrameworkElement)?.DataContext as AttachmentItem;
            if (item == null) return;

            if (string.IsNullOrEmpty(_pdf.FilePath) || !File.Exists(_pdf.FilePath))
            {
                MessageBox.Show("No PDF is loaded.", "Attachments",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Delete attachment \"{item.Name}\"?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                DeleteAttachment(_pdf.FilePath!, item.Key);
                LoadAttachments(); // refresh
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete attachment:\n{ex.Message}",
                    "Attachments", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void SelectPdf()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                Title = "Select a PDF file"
            };
            if (dlg.ShowDialog() == true)
            {
                _pdf.Set(dlg.FileName);
                // FileName + list will refresh via PropertyChanged handler
            }
        }

        private void LoadAttachments()
        {

            Attachments.Clear();
            var src = _pdf.FilePath;
            if (string.IsNullOrEmpty(src) || !File.Exists(src)) return;

            try
            {
                using var pdfDoc = new iText.Kernel.Pdf.PdfDocument(new PdfReader(src));
                var nameTree = pdfDoc.GetCatalog().GetNameTree(PdfName.EmbeddedFiles);
                if (nameTree == null) return;

                // nameTree.GetNames() returns a Dictionary<string, PdfObject>
                foreach (var kv in nameTree.GetNames())
                {
                    var keyName = kv.Key;                 // the logical name/key
                    var obj = kv.Value;                   // should be a filespec dictionary
                    var fsDict = obj as PdfDictionary;
                    if (fsDict == null) continue;

                    // name to show
                    var fNameObj = fsDict.GetAsString(PdfName.F) ?? fsDict.GetAsString(PdfName.UF);
                    string displayName = fNameObj?.ToUnicodeString() ?? keyName.ToUnicodeString();

                    // size (if present)
                    long size = -1;
                    var efDict = fsDict.GetAsDictionary(PdfName.EF);
                    if (efDict != null)
                    {
                        var stream = efDict.GetAsStream(PdfName.F) ?? efDict.GetAsStream(PdfName.UF);
                        if (stream != null)
                        {
                            var bytes = stream.GetBytes();
                            size = bytes != null ? bytes.LongLength : -1;
                        }
                    }

                    Attachments.Add(new AttachmentItem
                    {
                        Key = keyName.ToUnicodeString(),
                        Name = System.IO.Path.GetFileName(displayName),
                        SizeBytes = size
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading attachments: {ex.Message}", "Attachments",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private void AddAttachment(string pdfPath, string fileToAttach)
        {
            var tmp = Path.ChangeExtension(Path.GetTempFileName(), ".pdf");

            using var reader = new PdfReader(pdfPath);
            using var writer = new PdfWriter(tmp);
            using var pdfDoc = new iText.Kernel.Pdf.PdfDocument(reader, writer);

            var fileName = Path.GetFileName(fileToAttach);
            var bytes = File.ReadAllBytes(fileToAttach);

            // Create the embedded file spec and attach to the catalog
            var spec = PdfFileSpec.CreateEmbeddedFileSpec(
                pdfDoc,
                bytes,
                fileName,   // description
                fileName,   // file name
                null,       // mimeType (null = guess/omit)
                null        // fileParameter (null = defaults)
            );

            pdfDoc.AddFileAttachment(fileName, spec);

            // Close before replace
            pdfDoc.Close();

            var bak = pdfPath + ".bak";
            if (File.Exists(bak)) File.Delete(bak);
            File.Replace(tmp, pdfPath, bak);
        }



        private void DeleteAttachment(string pdfPath, string keyToRemove)
        {
            var tmp = Path.ChangeExtension(Path.GetTempFileName(), ".pdf");

            using (var reader = new PdfReader(pdfPath))
            using (var writer = new PdfWriter(tmp, new WriterProperties()))
            using (var pdfDoc = new iText.Kernel.Pdf.PdfDocument(reader, writer))
            {
                // Catalog -> /Names -> /EmbeddedFiles -> /Names [ key, filespec, key, filespec, ... ]
                var catalogDict = pdfDoc.GetCatalog().GetPdfObject();                 // PdfDictionary
                var namesDict = catalogDict.GetAsDictionary(PdfName.Names);
                if (namesDict == null) throw new InvalidOperationException("No /Names dictionary.");

                var embDict = namesDict.GetAsDictionary(PdfName.EmbeddedFiles);
                if (embDict == null) throw new InvalidOperationException("No /EmbeddedFiles dictionary.");

                var namesArray = embDict.GetAsArray(PdfName.Names);
                if (namesArray == null) throw new InvalidOperationException("No /Names array.");

                // Find the pair [key, filespec] whose key matches keyToRemove
                int removeAt = -1;
                for (int i = 0; i + 1 < namesArray.Size(); i += 2)
                {
                    var key = namesArray.GetAsString(i);
                    var keyText = key?.ToUnicodeString();
                    if (string.Equals(keyText, keyToRemove, StringComparison.Ordinal))
                    {
                        removeAt = i;
                        break;
                    }
                }
                if (removeAt < 0) throw new FileNotFoundException($"Attachment key not found: {keyToRemove}");

                // Remove value first, then key
                namesArray.Remove(removeAt + 1);
                namesArray.Remove(removeAt);

                // Optional cleanup if list becomes empty
                if (namesArray.Size() == 0)
                {
                    embDict.Remove(PdfName.Names);
                    namesDict.Remove(PdfName.EmbeddedFiles);
                    if (namesDict.Size() == 0)
                        catalogDict.Remove(PdfName.Names);
                }
            }

            var bak = pdfPath + ".bak";
            if (File.Exists(bak)) File.Delete(bak);
            File.Replace(tmp, pdfPath, bak);
        }

        private bool _navHandlerAttached;

        private async Task EnsurePreviewReady()
        {
            if (PdfPreview.CoreWebView2 != null) return;

            // Ensure it's created on the UI thread after the control is loaded
            var env = await CoreWebView2Environment.CreateAsync();
            await PdfPreview.EnsureCoreWebView2Async(env);

            var s = PdfPreview.CoreWebView2.Settings;
            s.AreDefaultContextMenusEnabled = false;
            s.AreDevToolsEnabled = false;
            s.IsStatusBarEnabled = false;
            s.IsZoomControlEnabled = false;

            PdfPreview.IsHitTestVisible = false;
            PdfPreview.Focusable = false;
            PdfPreview.AllowExternalDrop = false;

            // Attach once
            if (!_navHandlerAttached)
            {
                _navHandlerAttached = true;
                PdfPreview.CoreWebView2.NavigationCompleted += (s2, e) =>
                {
                    if (!e.IsSuccess)
                    {
                        // Surface the reason so you know why it failed
                        MessageBox.Show($"PDF preview navigation failed: {e.WebErrorStatus}",
                            "PDF Preview", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    // Hide outer scrollbars
                    PdfPreview.CoreWebView2.ExecuteScriptAsync(
                        "document.documentElement.style.overflow='hidden';document.body.style.overflow='hidden';");
                };
            }
        }

        private async Task LoadFirstPagePreviewAsync()
        {
            await EnsurePreviewReady();

            var path = _pdf.FilePath;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                PdfPreview.CoreWebView2?.NavigateToString("");
                return;
            }

            // Use AbsoluteUri to get file:///C:/... form
            var uri = new Uri(path).AbsoluteUri + "#page=1&zoom=page-fit";

            // Either way works; Source is simpler in WPF:
            PdfPreview.Source = new Uri(uri);
            // Or: PdfPreview.CoreWebView2?.Navigate(uri);
        }
    }
}


