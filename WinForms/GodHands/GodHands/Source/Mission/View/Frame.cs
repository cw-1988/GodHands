using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GodHands {
    public partial class Frame : Form {
        private OpenFileDialog ofd = new OpenFileDialog();
        private SaveFileDialog sfd = new SaveFileDialog();
        private bool triedAutoOpen = false;
        private bool restoredWindowPlacement = false;
        //private Subscriber_PropertyGrid sub_property = null;

        public Frame() {
            InitializeComponent();
            RestoreWindowPlacement();
            //Icon = View.IconFromFile("/img/menu/tools-disk-16.png");
            Icon = View.IconFromFile("/img/icon.ico");
            menu_open.Image = View.ImageFromFile("/img/menu/file-open-16.png");
            menu_save.Image = View.ImageFromFile("/img/menu/file-save-16.png");
            menu_close.Image = View.ImageFromFile("/img/menu/file-close-16.png");
            menu_exit.Image = View.ImageFromFile("/img/menu/file-exit-16.png");
            menu_undo.Image = View.ImageFromFile("/img/menu/edit-undo-16.png");
            menu_redo.Image = View.ImageFromFile("/img/menu/edit-redo-16.png");
            menu_disktool.Image = View.ImageFromFile("/img/menu/tools-disk-16.png");
            menu_database.Image = View.ImageFromFile("/img/menu/tools-database-16.png");
            menu_monitor.Image = View.ImageFromFile("/img/menu/tools-monitor-16.png");
            menu_logtool.Image = View.ImageFromFile("/img/menu/tools-logfile-16.png");
            menu_configtool.Image = View.ImageFromFile("/img/menu/tools-options-16.png");
            menu_customtool.Image = View.ImageFromFile("/img/menu/tools-custom-16.png");

            tool_open.Image = View.ImageFromFile("/img/menu/file-open-16.png");
            tool_save.Image = View.ImageFromFile("/img/menu/file-save-16.png");
            tool_close.Image = View.ImageFromFile("/img/menu/file-close-16.png");
            tool_undo.Image = View.ImageFromFile("/img/menu/edit-undo-16.png");
            tool_redo.Image = View.ImageFromFile("/img/menu/edit-redo-16.png");
            tool_disktool.Image = View.ImageFromFile("/img/menu/tools-disk-16.png");
            tool_database.Image = View.ImageFromFile("/img/menu/tools-database-16.png");
            tool_monitor.Image = View.ImageFromFile("/img/menu/tools-monitor-16.png");
            tool_logtool.Image = View.ImageFromFile("/img/menu/tools-logfile-16.png");
            tool_configtool.Image = View.ImageFromFile("/img/menu/tools-options-16.png");
            tool_customtool.Image = View.ImageFromFile("/img/menu/tools-custom-16.png");

            //sub_property = new Subscriber_PropertyGrid(propertyGrid1);
            Logger.AddStatusBar(statusbar);
            Logger.AddProgressBar(progressbar);
            statusbar.Image = View.ImageFromFile("/img/status/status-info.png");
            Shown += new EventHandler(OnShown);
        }

        private void OnClosing(object sender, FormClosingEventArgs e) {
            SaveWindowPlacement();
            Logger.RemoveStatusBar(statusbar);
            Logger.RemoveProgressBar(progressbar);
        }

        private void OnMenu_FileOpen(object sender, EventArgs e) {
            ofd.Title = "Open CD Image";
            ofd.Filter = "CD Images|*.bin;*.img;*.iso|All Files|*.*";
            if (ofd.ShowDialog() == DialogResult.OK) {
                OpenDiskImage(ofd.FileName);
            }
        }

        private void OnMenu_FileSaveAs(object sender, EventArgs e) {
            sfd.Title = "Save CD Image";
            sfd.Filter = "CD Images|*.bin;*.img;*.iso|All Files|*.*";
            if (sfd.ShowDialog() == DialogResult.OK) {
                //Iso9660.Open(ofd.FileName);
            }
        }

        private void OnMenu_FileClose(object sender, EventArgs e) {
            Iso9660.Close();
            zndeditor.CloseDisk();
        }

        private void OnMenu_FileExit(object sender, EventArgs e) {
            Close();
        }

        private void OnMenu_EditUndo(object sender, EventArgs e) {
            UndoRedo.Undo();
        }

        private void OnMenu_EditRedo(object sender, EventArgs e) {
            UndoRedo.Redo();
        }

        private void OnMenu_ToolsDiskTool(object sender, EventArgs e) {
            if (View.disktool == null) {
                View.disktool = new DiskTool();
                View.disktool.Show();
            } else {
                View.disktool.BringToFront();
            }
        }

        private void OnMenu_ToolsDatabase(object sender, EventArgs e) {
            if (View.databasetool == null) {
                View.databasetool = new DatabaseTool();
                View.databasetool.Show();
            } else {
                View.databasetool.BringToFront();
            }
        }

        private void OnMenu_ToolsMonitor(object sender, EventArgs e) {
            if (View.monitortool == null) {
                View.monitortool = new MonitorTool();
                View.monitortool.Show();
            } else {
                View.monitortool.BringToFront();
            }
        }

        private void OnMenu_ToolsLogFile(object sender, EventArgs e) {
            if (View.logtool == null) {
                View.logtool = new LogTool();
                View.logtool.Show();
            } else {
                View.logtool.BringToFront();
            }
        }

        private void OnMenu_ToolsConfig(object sender, EventArgs e) {
            if (View.configtool == null) {
                View.configtool = new ConfigTool();
                View.configtool.Show();
            } else {
                View.configtool.BringToFront();
            }
        }

        private void OnMenu_ToolsCustom(object sender, EventArgs e) {
            OpenFileDialog fd = new OpenFileDialog();
            fd.Title = "Open Custom Tool";
            fd.Filter = "C# Files|*.cs|All Files|*.*";
            if (fd.ShowDialog() == DialogResult.OK) {
                Form form = View.CompileForm(fd.FileName);
                form.Icon = View.IconFromFile("/img/menu/tools-custom-16.png");
                if (form != null) {
                    form.Show();
                }
            }
        }

        private void OnShown(object sender, EventArgs e) {
            if (triedAutoOpen) {
                return;
            }
            triedAutoOpen = true;

            string path = Config.LastOpenedImagePath;
            if (string.IsNullOrWhiteSpace(path)) {
                return;
            }

            if (!File.Exists(path)) {
                Logger.Warn("Auto-open skipped because the configured image path was not found: " + path);
                return;
            }

            OpenDiskImage(path);
        }

        private bool OpenDiskImage(string path) {
            if (string.IsNullOrWhiteSpace(path)) {
                return false;
            }

            if (Iso9660.Open(path)) {
                Config.LastOpenedImagePath = Path.GetFullPath(path);
                //sub_property.Notify(Iso9660.pvd);
                //sub_property.Notify(Iso9660.root);
                zndeditor.OpenDisk();
                return true;
            }
            return false;
        }

        private void RestoreWindowPlacement() {
            if (restoredWindowPlacement) {
                return;
            }
            restoredWindowPlacement = true;

            int? x = Config.WindowX;
            int? y = Config.WindowY;
            int? width = Config.WindowWidth;
            int? height = Config.WindowHeight;
            if (!x.HasValue || !y.HasValue || !width.HasValue || !height.HasValue) {
                return;
            }

            if (width.Value < 200 || height.Value < 200) {
                return;
            }

            Rectangle bounds = new Rectangle(x.Value, y.Value, width.Value, height.Value);
            if (!IsVisibleOnAnyScreen(bounds)) {
                bounds = EnsureVisibleBounds(bounds);
            }

            StartPosition = FormStartPosition.Manual;
            DesktopBounds = bounds;

            FormWindowState? state = Config.WindowState;
            if (state.HasValue && state.Value == FormWindowState.Maximized) {
                WindowState = FormWindowState.Maximized;
            }
        }

        private void SaveWindowPlacement() {
            Rectangle bounds = WindowState == FormWindowState.Normal ? DesktopBounds : RestoreBounds;
            if (bounds.Width > 0 && bounds.Height > 0) {
                Config.WindowX = bounds.X;
                Config.WindowY = bounds.Y;
                Config.WindowWidth = bounds.Width;
                Config.WindowHeight = bounds.Height;
            }

            FormWindowState state = WindowState;
            if (state == FormWindowState.Minimized) {
                state = FormWindowState.Normal;
            }
            Config.WindowState = state;
        }

        private static bool IsVisibleOnAnyScreen(Rectangle bounds) {
            foreach (Screen screen in Screen.AllScreens) {
                if (screen.WorkingArea.IntersectsWith(bounds)) {
                    return true;
                }
            }
            return false;
        }

        private static Rectangle EnsureVisibleBounds(Rectangle bounds) {
            Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
            int width = Math.Min(bounds.Width, workingArea.Width);
            int height = Math.Min(bounds.Height, workingArea.Height);
            int x = Math.Max(workingArea.Left, Math.Min(bounds.X, workingArea.Right - width));
            int y = Math.Max(workingArea.Top, Math.Min(bounds.Y, workingArea.Bottom - height));
            return new Rectangle(x, y, width, height);
        }
    }
}
