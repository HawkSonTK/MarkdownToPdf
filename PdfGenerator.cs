using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MarkdownToPdf
{
    public class PdfGenerator
    {
        private readonly WebView2 webView;

        public PdfGenerator(WebView2 webView)
        {
            this.webView = webView;
        }

        public async Task<bool> GeneratePdfAsync(string htmlContent, string outputPath)
        {
            try
            {
                await EnsureWebViewInitialized();
                
                // mermaid.min.jsの絶対パスを取得
                var appDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                var mermaidPath = Path.Combine(appDirectory!, "Assets", "mermaid.min.js");
                
                // HTMLでmermaid.jsのパスを絶対パスに置換
                htmlContent = htmlContent.Replace("file:///Assets/mermaid.min.js", $"file:///{mermaidPath.Replace('\\', '/')}");
                
                var tempHtmlPath = Path.GetTempFileName() + ".html";
                await File.WriteAllTextAsync(tempHtmlPath, htmlContent);

                webView.CoreWebView2.Navigate($"file:///{tempHtmlPath.Replace('\\', '/')}");
                
                await WaitForPageLoad();
                
                await WaitForMermaidRendering();

                var printSettings = webView.CoreWebView2.Environment.CreatePrintSettings();
                printSettings.ShouldPrintBackgrounds = true;
                printSettings.ShouldPrintSelectionOnly = false;
                printSettings.Orientation = CoreWebView2PrintOrientation.Portrait;
                printSettings.ScaleFactor = 1.0;
                printSettings.PageWidth = 8.27; // A4 width in inches
                printSettings.PageHeight = 11.69; // A4 height in inches
                printSettings.MarginTop = 0.39; // 10mm in inches
                printSettings.MarginBottom = 0.39;
                printSettings.MarginLeft = 0.39;
                printSettings.MarginRight = 0.39;

                await webView.CoreWebView2.PrintToPdfAsync(outputPath, printSettings);

                try
                {
                    File.Delete(tempHtmlPath);
                }
                catch
                {
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task EnsureWebViewInitialized()
        {
            if (webView.CoreWebView2 == null)
            {
                await webView.EnsureCoreWebView2Async();
            }
        }

        private async Task WaitForPageLoad()
        {
            var tcs = new TaskCompletionSource<bool>();
            
            void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
            {
                webView.CoreWebView2.NavigationCompleted -= OnNavigationCompleted;
                tcs.SetResult(e.IsSuccess);
            }

            webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;

            var timeoutTask = Task.Delay(10000);
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                webView.CoreWebView2.NavigationCompleted -= OnNavigationCompleted;
                throw new TimeoutException("Page load timeout");
            }

            if (!await tcs.Task)
            {
                throw new Exception("Navigation failed");
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
                    var result = await webView.CoreWebView2.ExecuteScriptAsync(checkScript);
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
    }
}