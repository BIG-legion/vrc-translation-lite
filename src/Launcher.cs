using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace VRCTranslationLite
{
    internal sealed class LauncherSettings
    {
        public string VrctExe { get; set; }
        public string NewApiExe { get; set; }
        public string NewApiBase { get; set; }
        public string BridgeBase { get; set; }
    }

    internal sealed class LauncherForm : Form
    {
        private readonly string appDir;
        private readonly string settingsPath;
        private LauncherSettings settings;
        private Process newApiProcess;
        private Process bridgeProcess;
        private Process vrctProcess;
        private bool busy;
        private bool allowClose;
        private bool statusRefreshRunning;

        private Label newApiStatus;
        private Label bridgeStatus;
        private Label vrctStatus;
        private Label messageLabel;
        private Button startButton;
        private Button configButton;
        private Button newApiButton;
        private Button stopButton;
        private System.Windows.Forms.Timer monitorTimer;

        public LauncherForm()
        {
            appDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            settingsPath = Path.Combine(appDir, "launcher-settings.json");
            settings = LoadSettings();
            BuildUi();
            monitorTimer = new System.Windows.Forms.Timer();
            monitorTimer.Interval = 2000;
            monitorTimer.Tick += MonitorTimerTick;
            monitorTimer.Start();
            Shown += delegate { RefreshStatusesAsync(); };
            FormClosing += LauncherFormClosing;
        }

        private LauncherSettings LoadSettings()
        {
            if (!File.Exists(settingsPath))
            {
                return new LauncherSettings();
            }
            try
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                return serializer.Deserialize<LauncherSettings>(File.ReadAllText(settingsPath, Encoding.UTF8));
            }
            catch (Exception ex)
            {
                MessageBox.Show("无法读取启动器配置：\r\n" + ex.Message, "VRC 翻译启动器", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return new LauncherSettings();
            }
        }

        private void BuildUi()
        {
            Text = "VRC 翻译启动器";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(610, 430);
            MinimumSize = new Size(626, 469);
            Font = new Font("Microsoft YaHei UI", 9F);
            BackColor = Color.FromArgb(246, 248, 252);

            Panel header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = 88;
            header.BackColor = Color.FromArgb(37, 99, 235);
            Controls.Add(header);

            Label title = new Label();
            title.Text = "VRC 翻译启动器";
            title.ForeColor = Color.White;
            title.Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold);
            title.AutoSize = true;
            title.Location = new Point(24, 17);
            header.Controls.Add(title);

            Label subtitle = new Label();
            subtitle.Text = "New API 与兼容中转会隐藏运行，关闭 VRCT 后自动结束。";
            subtitle.ForeColor = Color.FromArgb(219, 234, 254);
            subtitle.AutoSize = true;
            subtitle.Location = new Point(27, 56);
            header.Controls.Add(subtitle);

            GroupBox statusGroup = new GroupBox();
            statusGroup.Text = "运行状态";
            statusGroup.Location = new Point(22, 108);
            statusGroup.Size = new Size(566, 116);
            statusGroup.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(statusGroup);

            AddStatusRow(statusGroup, "New API", 28, out newApiStatus);
            AddStatusRow(statusGroup, "兼容中转", 56, out bridgeStatus);
            AddStatusRow(statusGroup, "VRCT", 84, out vrctStatus);

            startButton = MakeButton("一键启动", 22, 244, 176, true);
            startButton.Click += StartButtonClick;
            Controls.Add(startButton);

            configButton = MakeButton("配置 New API 令牌", 214, 244, 176, false);
            configButton.Click += ConfigButtonClick;
            Controls.Add(configButton);

            newApiButton = MakeButton("打开 New API", 406, 244, 182, false);
            newApiButton.Click += delegate { OpenUrl((settings.NewApiBase ?? "http://127.0.0.1:3000").TrimEnd('/') + "/"); };
            Controls.Add(newApiButton);

            stopButton = MakeButton("停止本次后台服务", 22, 296, 176, false);
            stopButton.Click += delegate { StopOwnedServices(); RefreshStatusesAsync(); };
            Controls.Add(stopButton);

            Button folderButton = MakeButton("打开安装目录", 214, 296, 176, false);
            folderButton.Click += delegate { Process.Start("explorer.exe", "\"" + appDir + "\""); };
            Controls.Add(folderButton);

            Button helpButton = MakeButton("查看使用说明", 406, 296, 182, false);
            helpButton.Click += delegate
            {
                string help = Path.Combine(appDir, "使用说明.txt");
                if (File.Exists(help)) Process.Start(help);
                else MessageBox.Show("未找到使用说明。", "VRC 翻译启动器");
            };
            Controls.Add(helpButton);

            messageLabel = new Label();
            messageLabel.Text = "LM Studio 地址：http://127.0.0.1:1234/v1";
            messageLabel.ForeColor = Color.FromArgb(71, 84, 103);
            messageLabel.AutoSize = false;
            messageLabel.Location = new Point(24, 356);
            messageLabel.Size = new Size(560, 32);
            messageLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Controls.Add(messageLabel);

            Label authorLabel = new Label();
            authorLabel.Text = "作者：little-legion　｜　仅供免费分享，禁止商用";
            authorLabel.ForeColor = Color.FromArgb(100, 116, 139);
            authorLabel.TextAlign = ContentAlignment.MiddleRight;
            authorLabel.Location = new Point(24, 394);
            authorLabel.Size = new Size(560, 22);
            authorLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Controls.Add(authorLabel);
        }

        private static void AddStatusRow(Control parent, string name, int y, out Label value)
        {
            Label label = new Label();
            label.Text = name;
            label.Location = new Point(18, y);
            label.Size = new Size(135, 22);
            parent.Controls.Add(label);

            value = new Label();
            value.Text = "检测中...";
            value.Location = new Point(170, y);
            value.Size = new Size(360, 22);
            value.ForeColor = Color.FromArgb(71, 84, 103);
            parent.Controls.Add(value);
        }

        private Button MakeButton(string text, int x, int y, int width, bool primary)
        {
            Button button = new Button();
            button.Text = text;
            button.Location = new Point(x, y);
            button.Size = new Size(width, 38);
            button.FlatStyle = FlatStyle.Flat;
            button.Cursor = Cursors.Hand;
            if (primary)
            {
                button.BackColor = Color.FromArgb(37, 99, 235);
                button.ForeColor = Color.White;
                button.FlatAppearance.BorderSize = 0;
            }
            else
            {
                button.BackColor = Color.White;
                button.ForeColor = Color.FromArgb(31, 41, 55);
                button.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            }
            return button;
        }

        private async void StartButtonClick(object sender, EventArgs e)
        {
            if (busy) return;
            if (!ValidateSettings()) return;
            SetBusy(true, "正在启动 New API...");
            try
            {
                await Task.Run(delegate { EnsureBackends(); });
                messageLabel.Text = "正在启动 VRCT...";
                await Task.Run(delegate { EnsureVrct(); });
                messageLabel.Text = "启动成功。窗口已最小化；关闭 VRCT 后后台服务会自动结束。";
                RefreshStatusesAsync();
                WindowState = FormWindowState.Minimized;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                messageLabel.Text = "启动失败，请检查路径、New API 初始化状态和端口占用。";
            }
            finally
            {
                SetBusy(false, null);
            }
        }

        private async void ConfigButtonClick(object sender, EventArgs e)
        {
            if (busy) return;
            if (!ValidateSettings()) return;
            SetBusy(true, "正在准备本地配置页...");
            try
            {
                await Task.Run(delegate { EnsureBackends(); });
                OpenUrl((settings.BridgeBase ?? "http://127.0.0.1:1234").TrimEnd('/') + "/configure");
                messageLabel.Text = "已打开令牌配置页。令牌只提交到本机 127.0.0.1。";
                RefreshStatusesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "无法打开配置页", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false, null);
            }
        }

        private bool ValidateSettings()
        {
            if (settings == null || string.IsNullOrWhiteSpace(settings.VrctExe) || !File.Exists(settings.VrctExe))
            {
                MessageBox.Show("VRCT.exe 路径无效。请重新运行安装器修改路径。", "配置错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (string.IsNullOrWhiteSpace(settings.NewApiExe) || !File.Exists(settings.NewApiExe))
            {
                MessageBox.Show("New API.exe 路径无效。请重新运行安装器修改路径。", "配置错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            string python = Path.Combine(appDir, "Compatibility Bridge", "runtime", "python.exe");
            string bridge = Path.Combine(appDir, "Compatibility Bridge", "bridge.py");
            if (!File.Exists(python) || !File.Exists(bridge))
            {
                MessageBox.Show("兼容中转文件不完整，请重新安装。", "配置错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private void EnsureBackends()
        {
            string newApiBase = (settings.NewApiBase ?? "http://127.0.0.1:3000").TrimEnd('/');
            string bridgeBase = (settings.BridgeBase ?? "http://127.0.0.1:1234").TrimEnd('/');
            if (!UrlReady(newApiBase + "/api/status", 1500))
            {
                newApiProcess = StartHidden(settings.NewApiExe, "", Path.GetDirectoryName(settings.NewApiExe));
                if (!WaitUrl(newApiBase + "/api/status", 45))
                    throw new InvalidOperationException("New API 在 45 秒内未就绪。请先单独打开 New API 完成首次初始化。\r\n" + settings.NewApiExe);
            }
            if (!UrlReady(bridgeBase + "/health", 1500))
            {
                string bridgeDir = Path.Combine(appDir, "Compatibility Bridge");
                string python = Path.Combine(bridgeDir, "runtime", "python.exe");
                string bridge = Path.Combine(bridgeDir, "bridge.py");
                bridgeProcess = StartHidden(python, Quote(bridge), bridgeDir);
                if (!WaitUrl(bridgeBase + "/health", 30))
                    throw new InvalidOperationException("兼容中转在 30 秒内未就绪。请检查 1234 端口是否被占用。");
            }
        }

        private void EnsureVrct()
        {
            Process[] existing = Process.GetProcessesByName("VRCT");
            if (existing.Length > 0)
            {
                vrctProcess = existing[0];
                return;
            }
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = settings.VrctExe;
            info.WorkingDirectory = Path.GetDirectoryName(settings.VrctExe);
            info.UseShellExecute = true;
            vrctProcess = Process.Start(info);
            if (vrctProcess == null) throw new InvalidOperationException("Windows 未能启动 VRCT.exe。");
        }

        private static Process StartHidden(string fileName, string arguments, string workingDirectory)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = fileName;
            info.Arguments = arguments;
            info.WorkingDirectory = workingDirectory;
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            info.WindowStyle = ProcessWindowStyle.Hidden;
            Process process = Process.Start(info);
            if (process == null) throw new InvalidOperationException("无法启动：" + fileName);
            return process;
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        private static bool WaitUrl(string url, int seconds)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(seconds);
            while (DateTime.UtcNow < deadline)
            {
                if (UrlReady(url, 1800)) return true;
                Thread.Sleep(500);
            }
            return false;
        }

        private static bool UrlReady(string url, int timeout)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Timeout = timeout;
                request.ReadWriteTimeout = timeout;
                request.Proxy = null;
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    return (int)response.StatusCode < 500;
                }
            }
            catch (WebException ex)
            {
                HttpWebResponse response = ex.Response as HttpWebResponse;
                return response != null && (int)response.StatusCode < 500;
            }
            catch { return false; }
        }

        private void RefreshStatusesAsync()
        {
            if (statusRefreshRunning) return;
            statusRefreshRunning = true;
            Task.Run(delegate
            {
                bool newApi = UrlReady((settings.NewApiBase ?? "http://127.0.0.1:3000").TrimEnd('/') + "/api/status", 800);
                bool bridge = UrlReady((settings.BridgeBase ?? "http://127.0.0.1:1234").TrimEnd('/') + "/health", 800);
                bool vrct = Process.GetProcessesByName("VRCT").Length > 0;
                BeginInvoke((MethodInvoker)delegate
                {
                    SetStatus(newApiStatus, newApi);
                    SetStatus(bridgeStatus, bridge);
                    SetStatus(vrctStatus, vrct);
                    statusRefreshRunning = false;
                });
            });
        }

        private static void SetStatus(Label label, bool running)
        {
            label.Text = running ? "● 运行中" : "○ 未运行";
            label.ForeColor = running ? Color.FromArgb(22, 128, 58) : Color.FromArgb(100, 116, 139);
        }

        private void MonitorTimerTick(object sender, EventArgs e)
        {
            RefreshStatusesAsync();
            if (vrctProcess == null) return;
            bool exited;
            try { exited = vrctProcess.HasExited; }
            catch { exited = true; }
            if (!exited) return;
            monitorTimer.Stop();
            StopOwnedServices();
            allowClose = true;
            Close();
        }

        private void StopOwnedServices()
        {
            StopProcess(ref bridgeProcess);
            StopProcess(ref newApiProcess);
            messageLabel.Text = "本次由启动器启动的后台服务已停止。";
        }

        private static void StopProcess(ref Process process)
        {
            if (process == null) return;
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                    process.WaitForExit(3000);
                }
            }
            catch { }
            finally
            {
                process.Dispose();
                process = null;
            }
        }

        private void SetBusy(bool value, string message)
        {
            busy = value;
            startButton.Enabled = !value;
            configButton.Enabled = !value;
            stopButton.Enabled = !value;
            if (!string.IsNullOrEmpty(message)) messageLabel.Text = message;
        }

        private static void OpenUrl(string url)
        {
            try { Process.Start(url); }
            catch (Exception ex) { MessageBox.Show("无法打开网页：\r\n" + ex.Message, "VRC 翻译启动器"); }
        }

        private void LauncherFormClosing(object sender, FormClosingEventArgs e)
        {
            if (allowClose) return;
            bool vrctRunning = Process.GetProcessesByName("VRCT").Length > 0;
            if (vrctRunning && (newApiProcess != null || bridgeProcess != null))
            {
                DialogResult result = MessageBox.Show(
                    "关闭启动器会停止本次启动的 New API 与兼容中转，VRCT 翻译将中断。\r\n\r\n确定退出吗？",
                    "确认退出", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }
            StopOwnedServices();
        }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LauncherForm());
        }
    }
}

