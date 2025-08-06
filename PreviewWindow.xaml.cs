using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace MarkdownToPdf
{
    public partial class PreviewWindow : Window
    {
        private string? currentFilePath;
        private string[] fileList = Array.Empty<string>();
        private int currentIndex = -1;
        private PdfGenerator? pdfGenerator;
        private Action<string>? logAction;

        public PreviewWindow()
        {
            InitializeComponent();
            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            try
            {
                await PreviewWebView.EnsureCoreWebView2Async();
                
                // WebView2の設定を調整
                PreviewWebView.CoreWebView2.Settings.IsWebMessageEnabled = true;
                PreviewWebView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;
                PreviewWebView.CoreWebView2.Settings.IsScriptEnabled = true;
                PreviewWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"WebView2の初期化に失敗しました: {ex.Message}", "エラー", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void SetFileList(string[] files, int selectedIndex)
        {
            fileList = files;
            currentIndex = selectedIndex;
            UpdateNavigationButtons();
        }

        public void SetPdfGenerator(PdfGenerator generator, Action<string> logAction)
        {
            pdfGenerator = generator;
            this.logAction = logAction;
            ConvertToPdfButton.IsEnabled = true;
        }

        public async void LoadMarkdownFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show("ファイルが見つかりません。", "エラー", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            currentFilePath = filePath;
            FileNameTextBlock.Text = Path.GetFileName(filePath);

            try
            {
                await PreviewWebView.EnsureCoreWebView2Async();

                var markdownContent = await File.ReadAllTextAsync(filePath);
                
                if (string.IsNullOrWhiteSpace(markdownContent))
                {
                    PreviewWebView.NavigateToString("<html><body><h3>空のファイルです</h3></body></html>");
                    return;
                }

                var htmlContent = MarkdownConverter.ConvertToHtml(markdownContent);
                
                // PDF生成と同じようにMermaidのパス処理を行う
                var appDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                var mermaidPath = Path.Combine(appDirectory!, "Assets", "mermaid.min.js");
                htmlContent = htmlContent.Replace("file:///Assets/mermaid.min.js", $"file:///{mermaidPath.Replace('\\', '/')}");
                
                // 一時HTMLファイルを作成してNavigateで読み込む（PDF生成と同じ方法）
                var tempHtmlPath = Path.GetTempFileName() + ".html";
                await File.WriteAllTextAsync(tempHtmlPath, htmlContent);
                PreviewWebView.CoreWebView2.Navigate($"file:///{tempHtmlPath.Replace('\\', '/')}");
                
                // MermaidレンダリングまでPDF生成と同じように待機
                await WaitForMermaidRendering();
                
                // 一時ファイルを削除
                try
                {
                    File.Delete(tempHtmlPath);
                }
                catch
                {
                    // エラーは無視
                }
            }
            catch (Exception ex)
            {
                var errorHtml = $"<html><body><h3>エラー</h3><p>{ex.Message}</p></body></html>";
                if (PreviewWebView.CoreWebView2 != null)
                {
                    PreviewWebView.NavigateToString(errorHtml);
                }
                else
                {
                    MessageBox.Show($"WebView2エラー: {ex.Message}", "エラー", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UpdateNavigationButtons()
        {
            PrevButton.IsEnabled = currentIndex > 0;
            NextButton.IsEnabled = currentIndex >= 0 && currentIndex < fileList.Length - 1;
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(currentFilePath))
            {
                LoadMarkdownFile(currentFilePath);
            }
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentIndex > 0)
            {
                currentIndex--;
                LoadMarkdownFile(fileList[currentIndex]);
                UpdateNavigationButtons();
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentIndex < fileList.Length - 1)
            {
                currentIndex++;
                LoadMarkdownFile(fileList[currentIndex]);
                UpdateNavigationButtons();
            }
        }

        private async void ConvertToPdfButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentFilePath) || pdfGenerator == null)
            {
                MessageBox.Show("変換するファイルがないか、PDF生成機能が利用できません。", "エラー", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ConvertToPdfButton.IsEnabled = false;
            
            try
            {
                var fileName = Path.GetFileName(currentFilePath);
                logAction?.Invoke($"PDF変換中: {fileName}");

                var markdownContent = await File.ReadAllTextAsync(currentFilePath);
                
                if (string.IsNullOrWhiteSpace(markdownContent))
                {
                    logAction?.Invoke($"スキップ (空ファイル): {fileName}");
                    return;
                }

                var htmlContent = MarkdownConverter.ConvertToHtml(markdownContent);
                var pdfPath = Path.ChangeExtension(currentFilePath, ".pdf");

                var success = await pdfGenerator.GeneratePdfAsync(htmlContent, pdfPath);

                if (success)
                {
                    logAction?.Invoke($"✓ PDF変換完了: {fileName} → {Path.GetFileName(pdfPath)}");
                    MessageBox.Show($"PDF変換が完了しました。\n{pdfPath}", "変換完了", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    logAction?.Invoke($"✗ PDF変換失敗: {fileName}");
                    MessageBox.Show("PDF変換に失敗しました。", "エラー", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                var fileName = Path.GetFileName(currentFilePath);
                logAction?.Invoke($"✗ エラー ({fileName}): {ex.Message}");
                MessageBox.Show($"変換中にエラーが発生しました: {ex.Message}", "エラー", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ConvertToPdfButton.IsEnabled = true;
            }
        }

        private async Task WaitForMermaidRendering()
        {
            try
            {
                // Mermaidの初期化を待つ
                await Task.Delay(3000);
                
                var checkScript = @"
                    (function() {
                        try {
                            // mermaidが定義されているかチェック
                            if (typeof mermaid === 'undefined') {
                                return 'mermaid_not_loaded';
                            }
                            
                            // mermaidブロックを探す
                            var mermaidElements = document.querySelectorAll('code.language-mermaid');
                            console.log('Found mermaid elements:', mermaidElements.length);
                            
                            if (mermaidElements.length === 0) {
                                return 'no_mermaid_blocks';
                            }
                            
                            // 各mermaidブロックをdivに変換してレンダリング
                            var allRendered = true;
                            for (var i = 0; i < mermaidElements.length; i++) {
                                var element = mermaidElements[i];
                                var parent = element.parentElement;
                                
                                if (parent && parent.tagName === 'PRE') {
                                    // まだレンダリングされていない場合
                                    if (!parent.nextSibling || !parent.nextSibling.classList || !parent.nextSibling.classList.contains('mermaid-rendered')) {
                                        var mermaidDiv = document.createElement('div');
                                        mermaidDiv.className = 'mermaid mermaid-rendered';
                                        mermaidDiv.textContent = element.textContent;
                                        parent.parentNode.insertBefore(mermaidDiv, parent.nextSibling);
                                        parent.style.display = 'none';
                                        allRendered = false;
                                    }
                                }
                            }
                            
                            if (!allRendered) {
                                // mermaidを再実行
                                mermaid.run();
                                return 'rendering';
                            }
                            
                            // SVGが生成されているかチェック
                            var renderedElements = document.querySelectorAll('.mermaid-rendered');
                            for (var j = 0; j < renderedElements.length; j++) {
                                if (!renderedElements[j].innerHTML.includes('<svg')) {
                                    return 'svg_not_ready';
                                }
                            }
                            
                            return 'ready';
                        } catch (e) {
                            console.error('Mermaid check error:', e);
                            return 'error: ' + e.message;
                        }
                    })();
                ";

                var maxAttempts = 20;
                var attempt = 0;
                
                while (attempt < maxAttempts)
                {
                    var result = await PreviewWebView.CoreWebView2.ExecuteScriptAsync(checkScript);
                    result = result.Trim('"'); // JSONの引用符を除去
                    
                    if (result == "ready")
                    {
                        break;
                    }
                    else if (result == "no_mermaid_blocks")
                    {
                        // Mermaidブロックがない場合は即座に完了
                        break;
                    }
                    
                    await Task.Delay(500);
                    attempt++;
                }
                
                // 最終的な待機
                await Task.Delay(1000);
            }
            catch
            {
                // エラーの場合は長めに待機
                await Task.Delay(5000);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}