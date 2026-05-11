using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace GodHands {
    public partial class ZndEditor : UserControl {
        private Subscriber_PropertyGrid sub_property = null;
        private Subscriber_TreeView sub_treeview = null;
        private OpenFileDialog ofd = new OpenFileDialog();
        private SaveFileDialog sfd = new SaveFileDialog();
        private Dictionary<string,string> zones = new Dictionary<string,string>();
        private Zone zone = null;

        private ContextMenuStrip menu = null;
        private ToolStripMenuItem menu_insert = null;
        private ToolStripMenuItem menu_remove = null;
        private ToolStripMenuItem menu_import = null;
        private ToolStripMenuItem menu_export = null;

        private TreeNode node = null;
        private Texture texture = null;
        private Image texture2d = null;
        private WebView2 embeddedVstools = null;
        private bool embeddedVstoolsDisabled = false;
        private int autoOpenRequestId = 0;
        private Panel scriptEditorPanel = null;
        private DataGridView scriptEditorGrid = null;
        private Panel scriptEditorEmptyPanel = null;
        private Button scriptEditorAddButton = null;
        private RoomScriptSection activeScriptSection = null;
        private bool scriptEditorRefreshing = false;
        private const string ScriptOpsColumnName = "Ops";
        private const string ScriptOpcodeColumnName = "Opcode";
        private const string ScriptValuesColumnName = "Values";

        public ZndEditor() {
            InitializeComponent();
            treeview.ImageList = View.ImageListFromDir("/img/zone");
            treeview.ShowNodeToolTips = true;
            sub_property = new Subscriber_PropertyGrid(property);
            sub_treeview = new Subscriber_TreeView(treeview);

            menu = new ContextMenuStrip();
            menu_insert = new ToolStripMenuItem("Insert", View.ImageFromFile("/img/zone/insert.png"));
            menu_remove = new ToolStripMenuItem("Remove", View.ImageFromFile("/img/zone/remove.png"));
            menu_import = new ToolStripMenuItem("Import", View.ImageFromFile("/img/zone/import.png"));
            menu_export = new ToolStripMenuItem("Export", View.ImageFromFile("/img/zone/export.png"));
            menu_insert.Click += new System.EventHandler(OnMenuInsert);
            menu_remove.Click += new System.EventHandler(OnMenuRemove);
            menu_import.Click += new System.EventHandler(OnMenuImport);
            menu_export.Click += new System.EventHandler(OnMenuExport);
            menu.Items.Add(menu_insert);
            menu.Items.Add(menu_remove);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(menu_import);
            menu.Items.Add(menu_export);

            InitializeScriptEditor();
        }

        public void OpenDisk() {
            treeview.Nodes.Clear();
            combobox.Items.Clear();
            combobox.Text = "";
            zones.Clear();
            zone = null;
            foreach (Zone zone in Model.zones.Values) {
                string key = zone.GetUrl();
                DirRec rec = zone.GetRec();
                string txt = rec.GetFileName();
                zones.Add(txt, key);
                combobox.Items.Add(txt);
            }
            sub_property.Notify(null);
            textbox.Text = "";
            activeScriptSection = null;
            ShowDefaultDetails();
            TryRestoreLastOpenedZone();
        }

        public void CloseDisk() {
            combobox.Text = "";
            treeview.Nodes.Clear();
            combobox.Items.Clear();
            property.SelectedObject = null;
            zones.Clear();
            node = null;
            texture = null;
            texture2d = null;
            textbox.Text = "";
            activeScriptSection = null;
            ShowDefaultDetails();
            picturebox.Invalidate();
            sub_property.Notify(null);
        }

        private async void OnTreeSelect(object sender, TreeViewEventArgs e) {
            int requestId = ++autoOpenRequestId;
            node = e.Node;
            texture = null;
            texture2d = null;
            textbox.Text = "";
            object obj = null;
            if (node != null) {
                string url = node.Name;
                obj = Model.Get(url);
                RoomScriptSection scriptSection = obj as RoomScriptSection;
                sub_property.Notify((scriptSection != null) ? null : obj);

                ActorSeqSection seq = obj as ActorSeqSection;
                if (seq != null) {
                    textbox.Text = seq.GetSummaryText();
                }

                ActorSeqAnimation animation = obj as ActorSeqAnimation;
                if (animation != null) {
                    textbox.Text = animation.GetSummaryText();
                }

                if (scriptSection != null) {
                    ShowScriptEditor(scriptSection);
                } else {
                    HideScriptEditor();
                }

                ActorModelSection section = obj as ActorModelSection;
                if ((section != null) && (textbox.Text.Length == 0)) {
                    textbox.Text = BuildSectionSummary(section);
                }

                if (zone != null) {
                    foreach (Texture image in zone.images) {
                        if (image.GetUrl() == url) {
                            texture = image;
                            texture2d = image.ToImage(true);
                            break;
                        }
                    }
                }
            } else {
                HideScriptEditor();
                sub_property.Notify(null);
            }
            picturebox.Invalidate();
            treeview.Focus();
            if (obj is RoomScriptSection) {
                return;
            }
            try {
                await AutoOpenSelectedInVstoolsAsync(obj, requestId);
            } catch (Exception ex) {
                ShowDefaultDetails();
                Logger.Fail("Failed to update embedded vstools preview: " + ex.Message);
            }
        }

        private void OnTreeClick(object sender, TreeNodeMouseClickEventArgs e) {
            if (e.Button == MouseButtons.Right) {
                treeview.SelectedNode = node = e.Node;
                object obj = GetSelectedObject();
                switch (node.Text) {
                case "Zone":
                    menu_insert.Enabled = false;
                    menu_remove.Enabled = false;
                    break;
                case "Rooms":
                case "Actors":
                case "Images":
                    menu_insert.Enabled = true;
                    menu_remove.Enabled = false;
                    break;
                default:
                    menu_insert.Enabled = false;
                    menu_remove.Enabled = !(obj is ActorSeqAnimation);
                    break;
                }
                bool isInMemory = obj is InMemory;
                menu_import.Enabled = isInMemory;
                menu_export.Enabled = isInMemory;
                menu.Show(Cursor.Position);
            }
            treeview.Focus();
        }

        private void OnMenuImport(object sender, EventArgs e) {
            if (node != null) {
                InMemory obj = Model.Get(node.Name) as InMemory;
                if (obj != null) {
                    ofd.Title = "Import File";
                    ofd.Filter = obj.GetExportFilter();
                    ofd.FileName = obj.GetExportName();
                    if (ofd.ShowDialog() == DialogResult.OK) {
                        obj.ImportRaw(ofd.FileName);
                    }
                }
            }
        }

        private void OnMenuExport(object sender, EventArgs e) {
            if (node != null) {
                InMemory obj = Model.Get(node.Name) as InMemory;
                if (obj != null) {
                    sfd.Title = "Export File";
                    sfd.Filter = obj.GetExportFilter();
                    sfd.FileName = obj.GetExportName();
                    if (sfd.ShowDialog() == DialogResult.OK) {
                        obj.ExportRaw(sfd.FileName);
                    }
                }
            }
        }

        private void OnMenuInsert(object sender, EventArgs e) {
            if (node != null) {
                TreeNode child = null;
                switch (node.Text) {
                case "Rooms":  child = new TreeNode("Room", 1, 1);  break;
                case "Actors": child = new TreeNode("Actor", 2, 2); break;
                case "Images": child = new TreeNode("Image", 3, 3); break;
                }

                if (child != null) {
                    node.Nodes.Add(child);
                    node.Expand();
                }
            }
        }

        private void OnMenuRemove(object sender, EventArgs e) {
            if (node != null) {
                switch (node.Text) {
                case "Zone": break;
                case "Rooms": break;
                case "Actors": break;
                case "Images": break;
                default:
                    TreeNode parent = node.Parent;
                    parent.Nodes.Remove(node);
                    break;
                }
            }
        }

        private void OnComboBoxSelect(object sender, EventArgs e) {
            object obj = combobox.SelectedItem;
            if (obj != null) {
                string key = combobox.GetItemText(obj);
                if (key.Length > 0) {
                    if (!zones.ContainsKey(key)) {
                        return;
                    }

                    string url = zones[key];
                    if (!Model.zones.ContainsKey(url)) {
                        return;
                    }
                    zone = Model.zones[url];
                    Config.LastOpenedZndFileName = key;
                    zone.OpenZone(treeview);
                }
            }
        }

        private void TryRestoreLastOpenedZone() {
            string lastOpenedZnd = Config.LastOpenedZndFileName;
            if (string.IsNullOrWhiteSpace(lastOpenedZnd)) {
                return;
            }

            if (!zones.ContainsKey(lastOpenedZnd)) {
                return;
            }

            combobox.SelectedItem = lastOpenedZnd;
        }

        private void OnPaintPictureBox(object sender, PaintEventArgs e) {
            if (texture2d != null) {
                double aspect = (double)texture2d.Width / (double)texture2d.Height;

                int ax = texture2d.Width*2;
                int ay = texture2d.Height;
                int bx = picturebox.Size.Width;
                int by = picturebox.Size.Height;
                int cx = picturebox.Location.X + bx/2;
                int cy = picturebox.Location.Y + by/2;

                int x1,y1,x2,y2;
                if (by*aspect <= bx) {
                    x1 = (int)(cx - by*aspect/2);
                    y1 = (int)(cy - by/2);
                    x2 = (int)(by*aspect);
                    y2 = (int)(by);
                } else {
                    x1 = (int)(cx - bx/2);
                    y1 = (int)(cy - bx/aspect/2);
                    x2 = (int)(bx);
                    y2 = (int)(bx/aspect);
                }

                Rectangle rect = new Rectangle(x1,y1,x2,y2);
                e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
                e.Graphics.DrawImage(texture2d, rect);
            }
        }

        private object GetSelectedObject() {
            if (node == null) {
                return null;
            }
            return Model.Get(node.Name);
        }

        private bool CanOpenSelectedInVstools() {
            return VstoolsBridge.CanLaunch(GetSelectedObject());
        }

        private async Task AutoOpenSelectedInVstoolsAsync(object obj, int requestId) {
            if (!VstoolsBridge.CanLaunch(obj)) {
                ShowDefaultDetails();
                return;
            }

            string url;
            string error;
            if (!VstoolsBridge.TryGetLaunchUrl(obj, out url, out error)) {
                ShowDefaultDetails();
                Logger.Fail(error);
                return;
            }

            try {
                await EnsureEmbeddedVstoolsAsync();
                if (embeddedVstools == null) {
                    ShowDefaultDetails();
                    return;
                }
                if (requestId != autoOpenRequestId) {
                    return;
                }
                ShowEmbeddedVstools();
                embeddedVstools.Source = new Uri(url);
            } catch {
                if (embeddedVstoolsDisabled) {
                    if (requestId == autoOpenRequestId) {
                        ShowDefaultDetails();
                        VstoolsBridge.Open(obj);
                    }
                    return;
                }
                throw;
            }
        }

        private async Task EnsureEmbeddedVstoolsAsync() {
            if (embeddedVstoolsDisabled) {
                return;
            }

            if (embeddedVstools == null) {
                embeddedVstools = new WebView2();
                embeddedVstools.Dock = DockStyle.Fill;
                vstoolsPanel.Controls.Add(embeddedVstools);
            }

            if (embeddedVstools.CoreWebView2 == null) {
                try {
                    await embeddedVstools.EnsureCoreWebView2Async();
                    ConfigureEmbeddedVstools(embeddedVstools.CoreWebView2);
                } catch {
                    embeddedVstoolsDisabled = true;
                    vstoolsPanel.Controls.Clear();
                    embeddedVstools.Dispose();
                    embeddedVstools = null;
                    ShowDefaultDetails();
                    Logger.Warn("WebView2 could not be initialized. Install the Microsoft Edge WebView2 Runtime to use the docked viewer.");
                }
            }
        }

        private void ShowDefaultDetails() {
            if (texture2d != null) {
                ShowTexturePreview();
                return;
            }
            ShowDetailsText();
        }

        private void ShowDetailsText() {
            textbox.Visible = true;
            textbox.BringToFront();
            picturebox.Visible = false;
            vstoolsPanel.Visible = false;
            if (scriptEditorPanel != null) {
                scriptEditorPanel.Visible = false;
            }
        }

        private void ShowEmbeddedVstools() {
            vstoolsPanel.Visible = true;
            vstoolsPanel.BringToFront();
            picturebox.Visible = false;
            textbox.Visible = false;
            if (scriptEditorPanel != null) {
                scriptEditorPanel.Visible = false;
            }
        }

        private void ShowTexturePreview() {
            picturebox.Visible = true;
            picturebox.BringToFront();
            textbox.Visible = false;
            vstoolsPanel.Visible = false;
            if (scriptEditorPanel != null) {
                scriptEditorPanel.Visible = false;
            }
        }

        private void InitializeScriptEditor() {
            scriptEditorPanel = new Panel();
            scriptEditorPanel.Dock = DockStyle.Fill;
            scriptEditorPanel.Padding = new Padding(6);
            scriptEditorPanel.Visible = false;

            scriptEditorGrid = new DataGridView();
            scriptEditorGrid.Dock = DockStyle.Fill;
            scriptEditorGrid.AllowUserToAddRows = false;
            scriptEditorGrid.AllowUserToDeleteRows = false;
            scriptEditorGrid.AllowUserToResizeRows = false;
            scriptEditorGrid.AutoGenerateColumns = false;
            scriptEditorGrid.BackgroundColor = SystemColors.Window;
            scriptEditorGrid.BorderStyle = BorderStyle.FixedSingle;
            scriptEditorGrid.EditMode = DataGridViewEditMode.EditOnEnter;
            scriptEditorGrid.MultiSelect = false;
            scriptEditorGrid.RowHeadersVisible = false;
            scriptEditorGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;

            DataGridViewButtonColumn opsColumn = new DataGridViewButtonColumn();
            opsColumn.Name = ScriptOpsColumnName;
            opsColumn.HeaderText = "";
            opsColumn.Text = "Ops";
            opsColumn.UseColumnTextForButtonValue = true;
            opsColumn.Width = 52;

            DataGridViewComboBoxColumn opcodeColumn = new DataGridViewComboBoxColumn();
            opcodeColumn.Name = ScriptOpcodeColumnName;
            opcodeColumn.HeaderText = "Opcode";
            opcodeColumn.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
            opcodeColumn.FlatStyle = FlatStyle.Flat;
            opcodeColumn.Width = 180;
            opcodeColumn.ValueType = typeof(RoomScriptOpcodeChoice);

            DataGridViewTextBoxColumn valuesColumn = new DataGridViewTextBoxColumn();
            valuesColumn.Name = ScriptValuesColumnName;
            valuesColumn.HeaderText = "Values";
            valuesColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            scriptEditorGrid.Columns.Add(opsColumn);
            scriptEditorGrid.Columns.Add(opcodeColumn);
            scriptEditorGrid.Columns.Add(valuesColumn);
            scriptEditorGrid.CurrentCellDirtyStateChanged += OnScriptEditorGridCurrentCellDirtyStateChanged;
            scriptEditorGrid.CellValueChanged += OnScriptEditorGridCellValueChanged;
            scriptEditorGrid.CellEndEdit += OnScriptEditorGridCellEndEdit;
            scriptEditorGrid.CellContentClick += OnScriptEditorGridCellContentClick;
            scriptEditorGrid.DataError += delegate(object sender, DataGridViewDataErrorEventArgs e) {
                e.ThrowException = false;
            };

            scriptEditorEmptyPanel = new Panel();
            scriptEditorEmptyPanel.Dock = DockStyle.Fill;

            scriptEditorAddButton = new Button();
            scriptEditorAddButton.Text = "Add Opcode";
            scriptEditorAddButton.AutoSize = true;
            scriptEditorAddButton.Location = new Point(8, 8);
            scriptEditorAddButton.Click += OnScriptEditorAddOpcode;
            scriptEditorEmptyPanel.Controls.Add(scriptEditorAddButton);

            scriptEditorPanel.Controls.Add(scriptEditorGrid);
            scriptEditorPanel.Controls.Add(scriptEditorEmptyPanel);
            detailPanel.Controls.Add(scriptEditorPanel);
        }

        private void ShowScriptEditor(RoomScriptSection section) {
            try {
                activeScriptSection = section;
                RefreshScriptNodeText(section);
                RenderScriptEditor();
                scriptEditorPanel.Visible = true;
                scriptEditorPanel.BringToFront();
                picturebox.Visible = false;
                vstoolsPanel.Visible = false;
                textbox.Visible = false;
            } catch (Exception ex) {
                HideScriptEditor();
                ShowScriptEditorError("Failed to open script section.\r\n\r\n" + ex.ToString());
                textbox.Text = ex.ToString();
                ShowDetailsText();
            }
        }

        private void HideScriptEditor() {
            activeScriptSection = null;
            if (scriptEditorPanel != null) {
                scriptEditorPanel.Visible = false;
            }
        }

        private void RenderScriptEditor() {
            if ((scriptEditorPanel == null) || (scriptEditorGrid == null) || (scriptEditorEmptyPanel == null) || (activeScriptSection == null)) {
                return;
            }

            scriptEditorRefreshing = true;
            try {
                IList<RoomScriptOpcodeEntry> entries = activeScriptSection.Opcodes;
                List<RoomScriptOpcodeChoice> opcodeChoices = activeScriptSection.OpcodeChoices.ToList();

                scriptEditorGrid.Rows.Clear();
                foreach (RoomScriptOpcodeEntry entry in entries) {
                    int rowIndex = scriptEditorGrid.Rows.Add();
                    DataGridViewRow row = scriptEditorGrid.Rows[rowIndex];
                    DataGridViewComboBoxCell opcodeCell = row.Cells[ScriptOpcodeColumnName] as DataGridViewComboBoxCell;
                    if (opcodeCell != null) {
                        opcodeCell.Items.Clear();
                        foreach (RoomScriptOpcodeChoice choice in opcodeChoices) {
                            opcodeCell.Items.Add(choice);
                        }
                        opcodeCell.Value = FindOpcodeChoice(opcodeChoices, entry.Opcode);
                    }
                    row.Cells[ScriptValuesColumnName].Value = entry.ValuesHex;
                }

                bool hasEntries = entries.Count > 0;
                scriptEditorGrid.Visible = hasEntries;
                scriptEditorEmptyPanel.Visible = !hasEntries;
            } finally {
                scriptEditorRefreshing = false;
            }
        }

        private void OnScriptEditorAddOpcode(object sender, EventArgs e) {
            if (activeScriptSection == null) {
                return;
            }

            string error;
            if (!activeScriptSection.TryAddOpcode(activeScriptSection.Opcodes.Count, 0x00, out error)) {
                ShowScriptEditorError(error);
                return;
            }
            RefreshActiveScriptEditor();
        }

        private void OnScriptEditorGridCurrentCellDirtyStateChanged(object sender, EventArgs e) {
            if ((scriptEditorGrid == null) || !scriptEditorGrid.IsCurrentCellDirty) {
                return;
            }
            if (scriptEditorGrid.CurrentCell is DataGridViewComboBoxCell) {
                scriptEditorGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void OnScriptEditorGridCellValueChanged(object sender, DataGridViewCellEventArgs e) {
            if (scriptEditorRefreshing || (activeScriptSection == null)) {
                return;
            }
            if ((e.RowIndex < 0) || (e.ColumnIndex < 0)) {
                return;
            }
            if (scriptEditorGrid.Columns[e.ColumnIndex].Name != ScriptOpcodeColumnName) {
                return;
            }

            object rawValue = scriptEditorGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
            if (rawValue == null) {
                return;
            }

            byte opcode;
            if (!TryGetOpcodeValue(rawValue, out opcode)) {
                RefreshActiveScriptEditor();
                return;
            }
            byte[] args = new byte[Math.Max(0, activeScriptSection.GetArgumentCount(opcode))];
            string error;
            if (!activeScriptSection.TryUpdateOpcode(e.RowIndex, opcode, args, out error)) {
                ShowScriptEditorError(error);
                RefreshActiveScriptEditor();
                return;
            }
            RefreshActiveScriptEditor();
        }

        private void OnScriptEditorGridCellEndEdit(object sender, DataGridViewCellEventArgs e) {
            if (scriptEditorRefreshing || (activeScriptSection == null)) {
                return;
            }
            if ((e.RowIndex < 0) || (e.ColumnIndex < 0)) {
                return;
            }
            if (scriptEditorGrid.Columns[e.ColumnIndex].Name != ScriptValuesColumnName) {
                return;
            }
            if (e.RowIndex >= activeScriptSection.Opcodes.Count) {
                return;
            }

            RoomScriptOpcodeEntry entry = activeScriptSection.Opcodes[e.RowIndex];
            string text = Convert.ToString(scriptEditorGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value, CultureInfo.InvariantCulture);
            byte[] args;
            string parseError;
            if (!TryParseOpcodeArgs(text, activeScriptSection.GetArgumentCount(entry.Opcode), out args, out parseError)) {
                ShowScriptEditorError(parseError);
                RefreshActiveScriptEditor();
                return;
            }

            if (!entry.Args.SequenceEqual(args)) {
                string error;
                if (!activeScriptSection.TryUpdateOpcode(e.RowIndex, entry.Opcode, args, out error)) {
                    ShowScriptEditorError(error);
                    RefreshActiveScriptEditor();
                    return;
                }
                RefreshActiveScriptEditor();
            }
        }

        private void OnScriptEditorGridCellContentClick(object sender, DataGridViewCellEventArgs e) {
            if ((activeScriptSection == null) || (e.RowIndex < 0) || (e.ColumnIndex < 0)) {
                return;
            }

            string columnName = scriptEditorGrid.Columns[e.ColumnIndex].Name;
            if (columnName == ScriptOpsColumnName) {
                ShowScriptOpsMenu(e.RowIndex, e.ColumnIndex);
            }
        }

        private void ShowScriptOpsMenu(int rowIndex, int columnIndex) {
            if ((activeScriptSection == null) || (scriptEditorGrid == null)) {
                return;
            }

            ContextMenuStrip context = new ContextMenuStrip();

            ToolStripMenuItem moveUp = new ToolStripMenuItem("Move Up");
            moveUp.Enabled = rowIndex > 0;
            moveUp.Click += delegate(object sender, EventArgs e) {
                string error;
                if (!activeScriptSection.TryMoveOpcode(rowIndex, rowIndex - 1, out error)) {
                    ShowScriptEditorError(error);
                    return;
                }
                RefreshActiveScriptEditor();
            };

            ToolStripMenuItem moveDown = new ToolStripMenuItem("Move Down");
            moveDown.Enabled = rowIndex < (activeScriptSection.Opcodes.Count - 1);
            moveDown.Click += delegate(object sender, EventArgs e) {
                string error;
                if (!activeScriptSection.TryMoveOpcode(rowIndex, rowIndex + 1, out error)) {
                    ShowScriptEditorError(error);
                    return;
                }
                RefreshActiveScriptEditor();
            };

            ToolStripMenuItem addAbove = new ToolStripMenuItem("Add Above");
            addAbove.Click += delegate(object sender, EventArgs e) {
                string error;
                if (!activeScriptSection.TryAddOpcode(rowIndex, 0x00, out error)) {
                    ShowScriptEditorError(error);
                    return;
                }
                RefreshActiveScriptEditor();
            };

            ToolStripMenuItem addBelow = new ToolStripMenuItem("Add Below");
            addBelow.Click += delegate(object sender, EventArgs e) {
                string error;
                if (!activeScriptSection.TryAddOpcode(rowIndex + 1, 0x00, out error)) {
                    ShowScriptEditorError(error);
                    return;
                }
                RefreshActiveScriptEditor();
            };

            ToolStripMenuItem delete = new ToolStripMenuItem("Delete");
            delete.Click += delegate(object sender, EventArgs e) {
                string error;
                if (!activeScriptSection.TryDeleteOpcode(rowIndex, out error)) {
                    ShowScriptEditorError(error);
                    return;
                }
                RefreshActiveScriptEditor();
            };

            context.Items.Add(moveUp);
            context.Items.Add(moveDown);
            context.Items.Add(new ToolStripSeparator());
            context.Items.Add(addAbove);
            context.Items.Add(addBelow);
            context.Items.Add(new ToolStripSeparator());
            context.Items.Add(delete);
            context.Closed += delegate(object sender, ToolStripDropDownClosedEventArgs e) {
                context.Dispose();
            };

            Rectangle rect = scriptEditorGrid.GetCellDisplayRectangle(columnIndex, rowIndex, true);
            context.Show(scriptEditorGrid, new Point(rect.Left, rect.Bottom));
        }

        private void RefreshActiveScriptEditor() {
            if (activeScriptSection == null) {
                return;
            }
            RefreshScriptNodeText(activeScriptSection);
            RenderScriptEditor();
        }

        private static RoomScriptOpcodeChoice FindOpcodeChoice(IList<RoomScriptOpcodeChoice> choices, byte opcode) {
            if (choices == null) {
                return null;
            }
            foreach (RoomScriptOpcodeChoice choice in choices) {
                if ((choice != null) && (choice.Opcode == opcode)) {
                    return choice;
                }
            }
            return null;
        }

        private void RefreshScriptNodeText(RoomScriptSection section) {
            if ((node == null) || (section == null)) {
                return;
            }
            if (ReferenceEquals(Model.Get(node.Name), section)) {
                node.Text = section.GetDisplayText();
            }
        }

        private bool TryParseOpcodeArgs(string text, int expectedCount, out byte[] args, out string error) {
            args = new byte[0];
            string trimmed = (text ?? "").Trim();
            if (trimmed.Length == 0) {
                if (expectedCount == 0) {
                    error = null;
                    return true;
                }
                error = "This opcode expects " + expectedCount + " byte(s).";
                return false;
            }

            string[] pieces = trimmed
                .Replace(",", " ")
                .Replace(";", " ")
                .Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            List<byte> values = new List<byte>();
            foreach (string piece in pieces) {
                string token = piece.Trim();
                if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) {
                    token = token.Substring(2);
                }

                byte value;
                if (!byte.TryParse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value)) {
                    error = "Could not parse byte value: " + piece;
                    return false;
                }
                values.Add(value);
            }

            if (values.Count != expectedCount) {
                error = "Expected " + expectedCount + " byte(s), but got " + values.Count + ".";
                return false;
            }

            args = values.ToArray();
            error = null;
            return true;
        }

        private static bool TryGetOpcodeValue(object rawValue, out byte opcode) {
            RoomScriptOpcodeChoice choice = rawValue as RoomScriptOpcodeChoice;
            if (choice != null) {
                opcode = choice.Opcode;
                return true;
            }

            if (rawValue is byte) {
                opcode = (byte)rawValue;
                return true;
            }

            if (rawValue != null) {
                try {
                    opcode = Convert.ToByte(rawValue, CultureInfo.InvariantCulture);
                    return true;
                } catch {
                }
            }

            opcode = 0;
            return false;
        }

        private void ShowScriptEditorError(string error) {
            if (string.IsNullOrEmpty(error)) {
                return;
            }
            Logger.Fail(error);
            MessageBox.Show(error, "Script Editor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private static void ConfigureEmbeddedVstools(CoreWebView2 core) {
            if (core == null) {
                return;
            }

            core.Settings.AreDefaultContextMenusEnabled = true;
            core.Settings.AreDevToolsEnabled = true;
            core.Settings.IsStatusBarEnabled = false;
            core.Settings.IsZoomControlEnabled = true;
        }

        private static string BuildSectionSummary(ActorModelSection section) {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(section.GetText());
            sb.AppendLine("Container: " + section.GetRec().GetFileName());
            sb.AppendLine("Offset: 0x" + section.GetPos().ToString("X8"));
            sb.AppendLine("Length: 0x" + section.GetLen().ToString("X8") + " (" + section.GetLen() + ")");
            sb.AppendLine("Export: " + section.GetExportName());
            return sb.ToString().TrimEnd();
        }
    }
}
