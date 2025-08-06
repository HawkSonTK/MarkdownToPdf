using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MarkdownToPdf
{
    public partial class MainWindow : Window
    {
        private string? selectedFolderPath;
        private PdfGenerator? pdfGenerator;
        private PreviewWindow? previewWindow;
        private string[] markdownFiles = Array.Empty<string>();

        public MainWindow()
        {
            InitializeComponent();
            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            try
            {
                await WebView.EnsureCoreWebView2Async();
                pdfGenerator = new PdfGenerator(WebView);
                LogMessage("WebView2の初期化が完了しました。");
            }
            catch (Exception ex)
            {
                LogMessage($"WebView2の初期化に失敗しました: {ex.Message}");
            }
        }

        private void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog()
            {
                Description = "Markdownファイルを含むフォルダを選択してください",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                selectedFolderPath = dialog.SelectedPath;
                SelectedFolderTextBox.Text = selectedFolderPath;
                ConvertButton.IsEnabled = true;
                PreviewButton.IsEnabled = true;
                LogMessage($"フォルダが選択されました: {selectedFolderPath}");
                LoadMarkdownFilesList();
            }
        }

        private async void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(selectedFolderPath) || pdfGenerator == null)
            {
                LogMessage("フォルダが選択されていないか、WebView2が初期化されていません。");
                return;
            }

            ConvertButton.IsEnabled = false;
            ProgressBar.Visibility = Visibility.Visible;

            try
            {
                await ConvertMarkdownFilesToPdf();
            }
            catch (Exception ex)
            {
                LogMessage($"変換中にエラーが発生しました: {ex.Message}");
            }
            finally
            {
                ConvertButton.IsEnabled = true;
                ProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        private async Task ConvertMarkdownFilesToPdf()
        {
            if (markdownFiles.Length == 0)
            {
                LoadMarkdownFilesList();
            }

            if (markdownFiles.Length == 0)
            {
                LogMessage("指定されたフォルダにMarkdownファイルが見つかりませんでした。");
                return;
            }

            var searchModeText = IncludeSubfoldersCheckBox.IsChecked == true ? "(サブフォルダ含む)" : "(現在のフォルダのみ)";
            LogMessage($"{markdownFiles.Length}個のMarkdownファイルが見つかりました{searchModeText}。変換を開始します...");

            var successCount = 0;
            var failureCount = 0;

            foreach (var mdFile in markdownFiles)
            {
                try
                {
                    var fileName = Path.GetFileName(mdFile);
                    var relativePath = Path.GetRelativePath(selectedFolderPath!, mdFile);
                    LogMessage($"変換中: {relativePath}");

                    var markdownContent = await File.ReadAllTextAsync(mdFile);
                    
                    if (string.IsNullOrWhiteSpace(markdownContent))
                    {
                        LogMessage($"スキップ (空ファイル): {relativePath}");
                        continue;
                    }

                    var htmlContent = MarkdownConverter.ConvertToHtml(markdownContent);
                    var pdfPath = Path.ChangeExtension(mdFile, ".pdf");

                    var success = await pdfGenerator.GeneratePdfAsync(htmlContent, pdfPath);

                    if (success)
                    {
                        successCount++;
                        LogMessage($"✓ 完了: {relativePath} → {Path.GetFileName(pdfPath)}");
                    }
                    else
                    {
                        failureCount++;
                        LogMessage($"✗ 失敗: {relativePath}");
                    }
                }
                catch (Exception ex)
                {
                    failureCount++;
                    var relativePath = Path.GetRelativePath(selectedFolderPath!, mdFile);
                    LogMessage($"✗ エラー ({relativePath}): {ex.Message}");
                }

                await Task.Delay(100);
            }

            LogMessage($"変換完了: 成功 {successCount}件, 失敗 {failureCount}件");
        }

        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(selectedFolderPath))
            {
                LogMessage("フォルダが選択されていません。");
                return;
            }

            if (markdownFiles.Length == 0)
            {
                LogMessage("指定されたフォルダにMarkdownファイルが見つかりませんでした。");
                return;
            }

            var selectedIndex = MarkdownFilesListBox.SelectedIndex;
            if (selectedIndex < 0 || selectedIndex >= markdownFiles.Length)
            {
                LogMessage("プレビューするファイルを選択してください。");
                return;
            }

            var selectedFilePath = markdownFiles[selectedIndex];
            
            if (previewWindow == null || !previewWindow.IsVisible)
            {
                previewWindow = new PreviewWindow();
                previewWindow.Owner = this;
                if (pdfGenerator != null)
                {
                    previewWindow.SetPdfGenerator(pdfGenerator, LogMessage);
                }
                previewWindow.Show();
            }
            
            previewWindow.SetFileList(markdownFiles, selectedIndex);
            previewWindow.LoadMarkdownFile(selectedFilePath);
            previewWindow.Activate();
            
            LogMessage($"プレビューを開きました: {Path.GetFileName(selectedFilePath)}");
        }

        private void LoadMarkdownFilesList()
        {
            if (string.IsNullOrEmpty(selectedFolderPath))
                return;

            var searchOption = IncludeSubfoldersCheckBox.IsChecked == true 
                ? SearchOption.AllDirectories 
                : SearchOption.TopDirectoryOnly;
                
            markdownFiles = Directory.GetFiles(selectedFolderPath, "*.md", searchOption)
                                    .Where(f => !Path.GetFileName(f).StartsWith("."))
                                    .ToArray();

            MarkdownFilesListBox.Items.Clear();
            
            foreach (var file in markdownFiles)
            {
                var relativePath = Path.GetRelativePath(selectedFolderPath, file);
                MarkdownFilesListBox.Items.Add(relativePath);
            }

            var searchModeText = IncludeSubfoldersCheckBox.IsChecked == true ? "(サブフォルダ含む)" : "(現在のフォルダのみ)";
            LogMessage($"{markdownFiles.Length}個のMarkdownファイルが見つかりました{searchModeText}");
        }

        private void MarkdownFilesListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (MarkdownFilesListBox.SelectedIndex >= 0 && MarkdownFilesListBox.SelectedIndex < markdownFiles.Length)
            {
                var selectedIndex = MarkdownFilesListBox.SelectedIndex;
                var selectedFilePath = markdownFiles[selectedIndex];
                
                if (previewWindow == null || !previewWindow.IsVisible)
                {
                    previewWindow = new PreviewWindow();
                    previewWindow.Owner = this;
                    if (pdfGenerator != null)
                    {
                        previewWindow.SetPdfGenerator(pdfGenerator, LogMessage);
                    }
                    previewWindow.Show();
                }
                
                previewWindow.SetFileList(markdownFiles, selectedIndex);
                previewWindow.LoadMarkdownFile(selectedFilePath);
                previewWindow.Activate();
                
                LogMessage($"プレビューを開きました: {Path.GetFileName(selectedFilePath)}");
            }
        }

        private void LogMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                LogListBox.Items.Insert(0, $"[{timestamp}] {message}");
                
                if (LogListBox.Items.Count > 100)
                {
                    LogListBox.Items.RemoveAt(LogListBox.Items.Count - 1);
                }
            });
        }
    }
}