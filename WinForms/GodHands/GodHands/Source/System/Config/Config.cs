using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GodHands {
    public static class Config {
        private const string FileName = "GodHands.runtime.config";
        private const string LastOpenedImageKey = "last_opened_image";
        private const string LastOpenedZndKey = "last_opened_znd";
        private const string WindowXKey = "window_x";
        private const string WindowYKey = "window_y";
        private const string WindowWidthKey = "window_width";
        private const string WindowHeightKey = "window_height";
        private const string WindowStateKey = "window_state";

        public static string LastOpenedImagePath {
            get { return GetValue(LastOpenedImageKey); }
            set { SetValue(LastOpenedImageKey, value); }
        }

        public static string LastOpenedZndFileName {
            get { return GetValue(LastOpenedZndKey); }
            set { SetValue(LastOpenedZndKey, value); }
        }

        public static int? WindowX {
            get { return GetIntValue(WindowXKey); }
            set { SetIntValue(WindowXKey, value); }
        }

        public static int? WindowY {
            get { return GetIntValue(WindowYKey); }
            set { SetIntValue(WindowYKey, value); }
        }

        public static int? WindowWidth {
            get { return GetIntValue(WindowWidthKey); }
            set { SetIntValue(WindowWidthKey, value); }
        }

        public static int? WindowHeight {
            get { return GetIntValue(WindowHeightKey); }
            set { SetIntValue(WindowHeightKey, value); }
        }

        public static FormWindowState? WindowState {
            get {
                string value = GetValue(WindowStateKey);
                FormWindowState state;
                if (Enum.TryParse(value, true, out state) &&
                    (state == FormWindowState.Normal || state == FormWindowState.Maximized)) {
                    return state;
                }
                return null;
            }
            set {
                if (!value.HasValue) {
                    SetValue(WindowStateKey, null);
                    return;
                }

                FormWindowState state = value.Value;
                if (state == FormWindowState.Minimized) {
                    state = FormWindowState.Normal;
                }
                SetValue(WindowStateKey, state.ToString());
            }
        }

        private static string ConfigPath {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName); }
        }

        private static Dictionary<string, string> ReadValues() {
            EnsureConfigFileExists();
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string rawLine in File.ReadAllLines(ConfigPath)) {
                string line = rawLine.Trim();
                if ((line.Length == 0) || line.StartsWith("#")) {
                    continue;
                }

                int split = line.IndexOf('=');
                if (split <= 0) {
                    continue;
                }

                string key = line.Substring(0, split).Trim();
                string value = line.Substring(split + 1).Trim();
                if (key.Length > 0) {
                    values[key] = value;
                }
            }

            return values;
        }

        private static string GetValue(string key) {
            Dictionary<string, string> values = ReadValues();
            if (!values.ContainsKey(key)) {
                return "";
            }
            return values[key];
        }

        private static int? GetIntValue(string key) {
            int value;
            if (int.TryParse(GetValue(key), out value)) {
                return value;
            }
            return null;
        }

        private static void SetValue(string key, string value) {
            Dictionary<string, string> values = ReadValues();
            if (string.IsNullOrWhiteSpace(value)) {
                if (values.ContainsKey(key)) {
                    values.Remove(key);
                }
            } else {
                values[key] = value.Trim();
            }

            List<string> lines = new List<string>();
            lines.Add("# Auto-generated GodHands runtime config.");
            lines.Add("# Edit last_opened_image to change the startup auto-open path.");
            lines.Add("# Edit last_opened_znd to auto-select a zone after the image opens.");
            lines.Add("# Window position, size, and startup state are persisted on exit.");
            foreach (KeyValuePair<string, string> pair in values.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)) {
                lines.Add(pair.Key + "=" + pair.Value);
            }
            File.WriteAllLines(ConfigPath, lines.ToArray());
        }

        private static void SetIntValue(string key, int? value) {
            SetValue(key, value.HasValue ? value.Value.ToString() : null);
        }

        private static void EnsureConfigFileExists() {
            if (!File.Exists(ConfigPath)) {
                List<string> lines = new List<string>();
                lines.Add("# Auto-generated GodHands runtime config.");
                lines.Add("# Edit last_opened_image to change the startup auto-open path.");
                lines.Add("# Edit last_opened_znd to auto-select a zone after the image opens.");
                lines.Add("# Window position, size, and startup state are persisted on exit.");
                lines.Add(LastOpenedImageKey + "=");
                lines.Add(LastOpenedZndKey + "=");
                lines.Add(WindowXKey + "=");
                lines.Add(WindowYKey + "=");
                lines.Add(WindowWidthKey + "=");
                lines.Add(WindowHeightKey + "=");
                lines.Add(WindowStateKey + "=");
                File.WriteAllLines(ConfigPath, lines.ToArray());
            }
        }
    }
}
