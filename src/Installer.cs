using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace VRCTranslationLiteInstaller
{
    internal sealed class InstallerForm : Form
    {
        private const string VrctDownloadUrl = "https://github.com/misyaguziya/VRCT/releases/latest";
        private const string NewApiDownloadUrl = "https://github.com/QuantumNous/new-api/releases/latest";
        private readonly string packageDir;
        private readonly string payloadDir;

        private TextBox vrctPath;
        private TextBox newApiPath;
        private TextBox installPath;
        private TextBox model1;
        private TextBox model2;
        private TextBox model3;
        private TextBox tokenBox;
        private CheckBox patchVrct;
        private CheckBox desktopShortcut;
        private CheckBox launchAfterInstall;
        private TextBox logBox;
        private ProgressBar progress;
        private Button installButton;
        private Button detectButton;

        public InstallerForm()
        {
            packageDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            payloadDir = Path.Combine(packageDir, "payload");
            BuildUi();
            Shown += delegate { AutoDetect(); };
        }

        private void BuildUi()
        {
            Text = "VRC 翻译轻量安装器";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(820, 710);
            MinimumSize = new Size(836, 749);
            Font = new Font("Microsoft YaHei UI", 9F);
            BackColor = Color.FromArgb(246, 248, 252);

            Panel header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = 92;
            header.BackColor = Color.FromArgb(15, 118, 110);
            Controls.Add(header);

            Label title = new Label();
            title.Text = "VRC 翻译轻量安装器";
            title.ForeColor = Color.White;
            title.Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold);
            title.AutoSize = true;
            title.Location = new Point(25, 17);
            header.Controls.Add(title);

            Label subtitle = new Label();
            subtitle.Text = "不包含 VRCT 与 New API 本体；自动安装兼容中转、配置 VRCT 并创建图形启动器。";
            subtitle.ForeColor = Color.FromArgb(204, 251, 241);
            subtitle.AutoSize = true;
            subtitle.Location = new Point(28, 57);
            header.Controls.Add(subtitle);

            GroupBox paths = new GroupBox();
            paths.Text = "第 1 步：选择已下载的软件";
            paths.Location = new Point(20, 108);
            paths.Size = new Size(780, 158);
            paths.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(paths);

            vrctPath = AddPathRow(paths, "VRCT.exe", 28, BrowseVrct);
            newApiPath = AddPathRow(paths, "New API.exe", 66, BrowseNewApi);
            installPath = AddPathRow(paths, "安装位置", 104, BrowseInstallFolder);
            installPath.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VRCTranslationHelper");

            LinkLabel vrctLink = new LinkLabel();
            vrctLink.Text = "下载";
            vrctLink.AutoSize = true;
            vrctLink.Location = new Point(92, 32);
            vrctLink.LinkClicked += delegate { OpenUrl(VrctDownloadUrl); };
            paths.Controls.Add(vrctLink);

            LinkLabel newApiLink = new LinkLabel();
            newApiLink.Text = "下载";
            newApiLink.AutoSize = true;
            newApiLink.Location = new Point(92, 70);
            newApiLink.LinkClicked += delegate { OpenUrl(NewApiDownloadUrl); };
            paths.Controls.Add(newApiLink);

            GroupBox models = new GroupBox();
            models.Text = "第 2 步：填写 New API 中可用的模型名称（最多 3 个）";
            models.Location = new Point(20, 278);
            models.Size = new Size(780, 116);
            models.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(models);

            model1 = AddModelBox(models, "模型 1", 20, "deepseek-v4-flash-none");
            model2 = AddModelBox(models, "模型 2", 274, "deepseek-v4-flash");
            model3 = AddModelBox(models, "模型 3", 528, "deepseek-v4-pro");

            Label modelHint = new Label();
            modelHint.Text = "必须与 New API 的模型/别名完全一致；只使用一个模型时可将后两项留空。";
            modelHint.ForeColor = Color.FromArgb(71, 84, 103);
            modelHint.AutoSize = true;
            modelHint.Location = new Point(20, 84);
            models.Controls.Add(modelHint);

            GroupBox security = new GroupBox();
            security.Text = "第 3 步：令牌与安装选项";
            security.Location = new Point(20, 406);
            security.Size = new Size(780, 122);
            security.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(security);

            Label tokenLabel = new Label();
            tokenLabel.Text = "New API 令牌（可留空，安装后再配置）";
            tokenLabel.Location = new Point(18, 28);
            tokenLabel.Size = new Size(270, 22);
            security.Controls.Add(tokenLabel);

            tokenBox = new TextBox();
            tokenBox.Location = new Point(292, 25);
            tokenBox.Size = new Size(465, 26);
            tokenBox.UseSystemPasswordChar = true;
            security.Controls.Add(tokenBox);

            patchVrct = new CheckBox();
            patchVrct.Text = "自动备份并配置 VRCT";
            patchVrct.Checked = true;
            patchVrct.AutoSize = true;
            patchVrct.Location = new Point(20, 70);
            security.Controls.Add(patchVrct);

            desktopShortcut = new CheckBox();
            desktopShortcut.Text = "创建桌面快捷方式";
            desktopShortcut.Checked = true;
            desktopShortcut.AutoSize = true;
            desktopShortcut.Location = new Point(228, 70);
            security.Controls.Add(desktopShortcut);

            launchAfterInstall = new CheckBox();
            launchAfterInstall.Text = "安装完成后打开启动器";
            launchAfterInstall.Checked = true;
            launchAfterInstall.AutoSize = true;
            launchAfterInstall.Location = new Point(420, 70);
            security.Controls.Add(launchAfterInstall);

            detectButton = MakeButton("检测环境", 20, 546, 145, false);
            detectButton.Click += delegate { ValidateInputs(true); };
            Controls.Add(detectButton);

            installButton = MakeButton("一键安装", 175, 546, 180, true);
            installButton.Click += InstallButtonClick;
            Controls.Add(installButton);

            Button exitButton = MakeButton("退出", 365, 546, 100, false);
            exitButton.Click += delegate { Close(); };
            Controls.Add(exitButton);

            progress = new ProgressBar();
            progress.Location = new Point(480, 552);
            progress.Size = new Size(320, 23);
            progress.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(progress);

            logBox = new TextBox();
            logBox.Location = new Point(20, 592);
            logBox.Size = new Size(780, 95);
            logBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            logBox.Multiline = true;
            logBox.ReadOnly = true;
            logBox.ScrollBars = ScrollBars.Vertical;
            logBox.BackColor = Color.White;
            logBox.Text = "准备就绪。请先下载并解压 VRCT 与 New API。\r\n";
            Controls.Add(logBox);
        }

        private TextBox AddPathRow(Control parent, string labelText, int y, EventHandler browseHandler)
        {
            Label label = new Label();
            label.Text = labelText;
            label.Location = new Point(18, y + 4);
            label.Size = new Size(72, 22);
            parent.Controls.Add(label);

            TextBox box = new TextBox();
            box.Location = new Point(126, y);
            box.Size = new Size(490, 26);
            box.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            parent.Controls.Add(box);

            Button browse = new Button();
            browse.Text = "浏览...";
            browse.Location = new Point(630, y - 1);
            browse.Size = new Size(125, 29);
            browse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            browse.Click += browseHandler;
            parent.Controls.Add(browse);
            return box;
        }

        private TextBox AddModelBox(Control parent, string labelText, int x, string value)
        {
            Label label = new Label();
            label.Text = labelText;
            label.Location = new Point(x, 28);
            label.Size = new Size(70, 22);
            parent.Controls.Add(label);

            TextBox box = new TextBox();
            box.Text = value;
            box.Location = new Point(x, 52);
            box.Size = new Size(232, 26);
            parent.Controls.Add(box);
            return box;
        }

        private Button MakeButton(string text, int x, int y, int width, bool primary)
        {
            Button button = new Button();
            button.Text = text;
            button.Location = new Point(x, y);
            button.Size = new Size(width, 36);
            button.FlatStyle = FlatStyle.Flat;
            button.Cursor = Cursors.Hand;
            if (primary)
            {
                button.BackColor = Color.FromArgb(15, 118, 110);
                button.ForeColor = Color.White;
                button.FlatAppearance.BorderSize = 0;
            }
            else
            {
                button.BackColor = Color.White;
                button.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            }
            return button;
        }

        private void BrowseVrct(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "选择 VRCT.exe";
            dialog.Filter = "VRCT.exe|VRCT.exe|可执行文件|*.exe";
            if (dialog.ShowDialog(this) == DialogResult.OK) vrctPath.Text = dialog.FileName;
        }

        private void BrowseNewApi(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "选择 New API 可执行文件";
            dialog.Filter = "New API 可执行文件|new-api*.exe|可执行文件|*.exe";
            if (dialog.ShowDialog(this) == DialogResult.OK) newApiPath.Text = dialog.FileName;
        }

        private void BrowseInstallFolder(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "选择 VRC 翻译助手安装位置";
            dialog.SelectedPath = installPath.Text;
            if (dialog.ShowDialog(this) == DialogResult.OK) installPath.Text = dialog.SelectedPath;
        }

        private void AutoDetect()
        {
            string vrct = FindNearbyFile("VRCT.exe");
            string newApi = FindNearbyFile("new-api*.exe");
            if (!string.IsNullOrEmpty(vrct)) vrctPath.Text = vrct;
            if (!string.IsNullOrEmpty(newApi)) newApiPath.Text = newApi;
            if (!string.IsNullOrEmpty(vrct) || !string.IsNullOrEmpty(newApi))
                Log("已自动检测到部分软件路径，请确认后安装。");
        }

        private string FindNearbyFile(string pattern)
        {
            List<string> roots = new List<string>();
            roots.Add(packageDir);
            DirectoryInfo parent = Directory.GetParent(packageDir);
            if (parent != null) roots.Add(parent.FullName);
            foreach (string root in roots.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    string direct = Directory.GetFiles(root, pattern, SearchOption.TopDirectoryOnly).FirstOrDefault();
                    if (direct != null) return direct;
                    foreach (string dir in Directory.GetDirectories(root))
                    {
                        if (string.Equals(dir, payloadDir, StringComparison.OrdinalIgnoreCase)) continue;
                        string nested = Directory.GetFiles(dir, pattern, SearchOption.TopDirectoryOnly).FirstOrDefault();
                        if (nested != null) return nested;
                    }
                }
                catch { }
            }
            return "";
        }

        private bool ValidateInputs(bool showSuccess)
        {
            List<string> errors = new List<string>();
            if (!File.Exists(vrctPath.Text.Trim())) errors.Add("VRCT.exe 路径无效");
            if (!File.Exists(newApiPath.Text.Trim())) errors.Add("New API.exe 路径无效");
            if (string.IsNullOrWhiteSpace(installPath.Text)) errors.Add("安装位置不能为空");
            if (string.IsNullOrWhiteSpace(model1.Text)) errors.Add("模型 1 不能为空");
            if (!Directory.Exists(payloadDir)) errors.Add("安装包 payload 文件夹缺失");
            if (!File.Exists(Path.Combine(payloadDir, "VRC 翻译启动器.exe"))) errors.Add("图形启动器文件缺失");
            if (!File.Exists(Path.Combine(payloadDir, "Compatibility Bridge", "runtime", "python.exe"))) errors.Add("兼容中转运行时缺失");
            if (!File.Exists(Path.Combine(payloadDir, "Compatibility Bridge", "bridge.py"))) errors.Add("兼容中转程序缺失");
            if (errors.Count > 0)
            {
                MessageBox.Show(string.Join("\r\n", errors.ToArray()), "环境检测未通过", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Log("环境检测未通过：" + string.Join("；", errors.ToArray()));
                return false;
            }
            string config = Path.Combine(Path.GetDirectoryName(vrctPath.Text.Trim()), "config.json");
            if (!File.Exists(config))
                Log("提示：VRCT config.json 尚未生成。可先运行并关闭一次 VRCT，再重新安装以自动配置。");
            if (showSuccess)
            {
                MessageBox.Show("路径与安装包文件检测通过。", "环境检测", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Log("环境检测通过。");
            }
            return true;
        }

        private async void InstallButtonClick(object sender, EventArgs e)
        {
            if (!ValidateInputs(false)) return;
            string vrct = Path.GetFullPath(vrctPath.Text.Trim());
            string newApi = Path.GetFullPath(newApiPath.Text.Trim());
            string destination = Path.GetFullPath(installPath.Text.Trim());
            string token = tokenBox.Text.Trim();
            List<string> models = new List<string>();
            AddUniqueModel(models, model1.Text);
            AddUniqueModel(models, model2.Text);
            AddUniqueModel(models, model3.Text);
            bool shouldPatch = patchVrct.Checked;
            bool shouldDesktopShortcut = desktopShortcut.Checked;
            bool shouldLaunch = launchAfterInstall.Checked;

            SetBusy(true);
            Log("开始安装到：" + destination);
            try
            {
                string result = await Task.Run(delegate
                {
                    Directory.CreateDirectory(destination);
                    CopyDirectory(payloadDir, destination);
                    WriteLauncherSettings(destination, vrct, newApi);
                    WriteBridgeSettings(destination, models);
                    if (!string.IsNullOrEmpty(token)) WriteEncryptedToken(destination, token);
                    string patchResult = shouldPatch ? PatchVrctConfig(vrct, models[0]) : "已跳过 VRCT 自动配置";
                    CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "VRC 翻译启动器.lnk"), Path.Combine(destination, "VRC 翻译启动器.exe"), destination);
                    if (shouldDesktopShortcut)
                        CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "VRC 翻译启动器.lnk"), Path.Combine(destination, "VRC 翻译启动器.exe"), destination);
                    return patchResult;
                });
                tokenBox.Clear();
                Log("兼容中转、图形启动器和快捷方式已安装。");
                Log(result);
                Log(string.IsNullOrEmpty(token) ? "令牌未写入，可在启动器中点击“配置 New API 令牌”。" : "令牌已使用 Windows DPAPI 加密保存。");
                MessageBox.Show("安装完成！\r\n\r\n" + result + "\r\nLM Studio 地址：http://127.0.0.1:1234/v1", "VRC 翻译轻量安装器", MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (shouldLaunch) Process.Start(Path.Combine(destination, "VRC 翻译启动器.exe"));
            }
            catch (Exception ex)
            {
                Log("安装失败：" + ex.Message);
                MessageBox.Show("安装失败：\r\n" + ex.Message, "VRC 翻译轻量安装器", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private static void AddUniqueModel(List<string> models, string value)
        {
            string cleaned = (value ?? "").Trim();
            if (cleaned.Length > 0 && !models.Contains(cleaned)) models.Add(cleaned);
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (string file in Directory.GetFiles(source))
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
            foreach (string directory in Directory.GetDirectories(source))
                CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
        }

        private static void WriteLauncherSettings(string destination, string vrct, string newApi)
        {
            Dictionary<string, object> settings = new Dictionary<string, object>();
            settings["VrctExe"] = vrct;
            settings["NewApiExe"] = newApi;
            settings["NewApiBase"] = "http://127.0.0.1:3000";
            settings["BridgeBase"] = "http://127.0.0.1:1234";
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            WriteUtf8(Path.Combine(destination, "launcher-settings.json"), serializer.Serialize(settings));
        }

        private static void WriteBridgeSettings(string destination, List<string> models)
        {
            string configDir = Path.Combine(destination, "Compatibility Bridge", "config");
            Directory.CreateDirectory(configDir);
            string settingsPath = Path.Combine(configDir, "settings.json");
            Dictionary<string, object> settings = new Dictionary<string, object>();
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            if (File.Exists(settingsPath))
            {
                try { settings = serializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(settingsPath, Encoding.UTF8)); }
                catch { settings = new Dictionary<string, object>(); }
            }
            settings["models"] = models.ToArray();
            settings["upstream_base"] = "http://127.0.0.1:3000";
            settings["installed_at"] = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            WriteUtf8(settingsPath, serializer.Serialize(settings));
        }

        private static void WriteEncryptedToken(string destination, string token)
        {
            string configDir = Path.Combine(destination, "Compatibility Bridge", "config");
            Directory.CreateDirectory(configDir);
            string tokenPath = Path.Combine(configDir, "token.dat");
            byte[] entropy = Encoding.ASCII.GetBytes("VRCTNewAPIBridge/v1");
            byte[] encrypted = ProtectedData.Protect(Encoding.UTF8.GetBytes(token), entropy, DataProtectionScope.LocalMachine);
            File.WriteAllBytes(tokenPath, encrypted);
            RestrictTokenAcl(tokenPath);
        }

        private static void RestrictTokenAcl(string tokenPath)
        {
            FileSecurity security = new FileSecurity();
            security.SetAccessRuleProtection(true, false);
            SecurityIdentifier currentUser = WindowsIdentity.GetCurrent().User;
            security.AddAccessRule(new FileSystemAccessRule(currentUser, FileSystemRights.FullControl, AccessControlType.Allow));
            security.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), FileSystemRights.FullControl, AccessControlType.Allow));
            security.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null), FileSystemRights.FullControl, AccessControlType.Allow));
            File.SetAccessControl(tokenPath, security);
        }

        private static string PatchVrctConfig(string vrctExe, string model)
        {
            string configPath = Path.Combine(Path.GetDirectoryName(vrctExe), "config.json");
            if (!File.Exists(configPath))
                return "VRCT config.json 尚未生成；请先运行并关闭一次 VRCT，然后重新运行安装器。";
            string backupPath = configPath + ".before-vrc-translation-lite-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".bak";
            File.Copy(configPath, backupPath, false);
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            serializer.MaxJsonLength = 4 * 1024 * 1024;
            Dictionary<string, object> root = serializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(configPath, Encoding.UTF8));
            root["LMSTUDIO_URL"] = "http://127.0.0.1:1234/v1";
            root["SELECTED_LMSTUDIO_MODEL"] = model;
            object enginesObject;
            Dictionary<string, object> engines = null;
            if (root.TryGetValue("SELECTED_TRANSLATION_ENGINES", out enginesObject))
                engines = enginesObject as Dictionary<string, object>;
            if (engines == null)
            {
                engines = new Dictionary<string, object>();
                root["SELECTED_TRANSLATION_ENGINES"] = engines;
            }
            engines["1"] = "LMStudio";
            WriteUtf8(configPath, serializer.Serialize(root));
            return "VRCT 已自动配置；原配置备份为：" + Path.GetFileName(backupPath);
        }

        private static void WriteUtf8(string path, string content)
        {
            File.WriteAllText(path, content, new UTF8Encoding(false));
        }

        private static void CreateShortcut(string shortcutPath, string targetPath, string workingDirectory)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(shortcutPath));
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            object shell = Activator.CreateInstance(shellType);
            object shortcut = shellType.InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { shortcutPath });
            Type shortcutType = shortcut.GetType();
            shortcutType.InvokeMember("TargetPath", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { targetPath });
            shortcutType.InvokeMember("WorkingDirectory", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { workingDirectory });
            shortcutType.InvokeMember("Description", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { "启动 VRCT、New API 与本地兼容中转" });
            shortcutType.InvokeMember("IconLocation", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { targetPath + ",0" });
            shortcutType.InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, shortcut, null);
        }

        private void SetBusy(bool value)
        {
            installButton.Enabled = !value;
            detectButton.Enabled = !value;
            progress.Style = value ? ProgressBarStyle.Marquee : ProgressBarStyle.Blocks;
            if (!value) progress.Value = 0;
        }

        private void Log(string message)
        {
            logBox.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message + "\r\n");
        }

        private static void OpenUrl(string url)
        {
            try { Process.Start(url); }
            catch (Exception ex) { MessageBox.Show("无法打开网页：\r\n" + ex.Message); }
        }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new InstallerForm());
        }
    }
}

