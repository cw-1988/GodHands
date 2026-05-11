namespace GodHands {
    partial class ZndEditor {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.treeview = new System.Windows.Forms.TreeView();
            this.combobox = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.property = new System.Windows.Forms.PropertyGrid();
            this.detailPanel = new System.Windows.Forms.Panel();
            this.picturebox = new System.Windows.Forms.PictureBox();
            this.vstoolsPanel = new System.Windows.Forms.Panel();
            this.textbox = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.detailPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picturebox)).BeginInit();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.tableLayoutPanel1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Size = new System.Drawing.Size(629, 438);
            this.splitContainer1.SplitterDistance = 199;
            this.splitContainer1.TabIndex = 1;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.treeview, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.combobox, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(199, 438);
            this.tableLayoutPanel1.TabIndex = 1;
            // 
            // treeview
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.treeview, 2);
            this.treeview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeview.Location = new System.Drawing.Point(3, 31);
            this.treeview.Name = "treeview";
            this.treeview.Size = new System.Drawing.Size(193, 404);
            this.treeview.TabIndex = 0;
            this.treeview.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.OnTreeSelect);
            this.treeview.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.OnTreeClick);
            // 
            // combobox
            // 
            this.combobox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.combobox.FormattingEnabled = true;
            this.combobox.Location = new System.Drawing.Point(51, 3);
            this.combobox.Name = "combobox";
            this.combobox.Size = new System.Drawing.Size(145, 21);
            this.combobox.TabIndex = 1;
            this.combobox.SelectedIndexChanged += new System.EventHandler(this.OnComboBoxSelect);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(42, 28);
            this.label1.TabIndex = 2;
            this.label1.Text = "Zone";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.property);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.detailPanel);
            this.splitContainer2.Size = new System.Drawing.Size(426, 438);
            this.splitContainer2.SplitterDistance = 224;
            this.splitContainer2.TabIndex = 0;
            // 
            // property
            // 
            this.property.Dock = System.Windows.Forms.DockStyle.Fill;
            this.property.Location = new System.Drawing.Point(0, 0);
            this.property.Name = "property";
            this.property.PropertySort = System.Windows.Forms.PropertySort.Categorized;
            this.property.Size = new System.Drawing.Size(224, 438);
            this.property.TabIndex = 0;
            // 
            // detailPanel
            // 
            this.detailPanel.Controls.Add(this.picturebox);
            this.detailPanel.Controls.Add(this.vstoolsPanel);
            this.detailPanel.Controls.Add(this.textbox);
            this.detailPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.detailPanel.Location = new System.Drawing.Point(0, 0);
            this.detailPanel.Name = "detailPanel";
            this.detailPanel.Size = new System.Drawing.Size(198, 438);
            this.detailPanel.TabIndex = 0;
            // 
            // picturebox
            // 
            this.picturebox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picturebox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picturebox.Location = new System.Drawing.Point(0, 0);
            this.picturebox.Name = "picturebox";
            this.picturebox.Size = new System.Drawing.Size(198, 438);
            this.picturebox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picturebox.TabIndex = 2;
            this.picturebox.TabStop = false;
            this.picturebox.Paint += new System.Windows.Forms.PaintEventHandler(this.OnPaintPictureBox);
            // 
            // vstoolsPanel
            // 
            this.vstoolsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.vstoolsPanel.Location = new System.Drawing.Point(0, 0);
            this.vstoolsPanel.Name = "vstoolsPanel";
            this.vstoolsPanel.Size = new System.Drawing.Size(198, 438);
            this.vstoolsPanel.TabIndex = 0;
            // 
            // textbox
            // 
            this.textbox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textbox.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textbox.Location = new System.Drawing.Point(0, 0);
            this.textbox.Multiline = true;
            this.textbox.Name = "textbox";
            this.textbox.ReadOnly = true;
            this.textbox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textbox.Size = new System.Drawing.Size(198, 438);
            this.textbox.TabIndex = 0;
            // 
            // ZndEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Name = "ZndEditor";
            this.Size = new System.Drawing.Size(629, 438);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.detailPanel.ResumeLayout(false);
            this.detailPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picturebox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TreeView treeview;
        private System.Windows.Forms.ComboBox combobox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.TextBox textbox;
        private System.Windows.Forms.PropertyGrid property;
        private System.Windows.Forms.PictureBox picturebox;
        private System.Windows.Forms.Panel detailPanel;
        private System.Windows.Forms.Panel vstoolsPanel;

    }
}
