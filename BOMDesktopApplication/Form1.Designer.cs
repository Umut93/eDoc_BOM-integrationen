using System;
using System.Windows.Forms;

namespace Fujitsu.eDoc.BOMApplicationDesktopApp
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.CreateBOMCaseBtn = new System.Windows.Forms.Button();
            this.helloWorldLabel = new System.Windows.Forms.Label();
            this.Count = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.BOMCaseIDLabel = new System.Windows.Forms.Label();
            this.BOMCaseIDtextBox = new System.Windows.Forms.TextBox();
            this.CertificatecomboBox = new System.Windows.Forms.ComboBox();
            this.CertificateLabel = new System.Windows.Forms.Label();
            //this.CVRLabel = new System.Windows.Forms.Label();
            //this.CvrTextBox = new System.Windows.Forms.TextBox();
            this.CertificateTestbtn = new System.Windows.Forms.Button();
            this.ServerURLComboBox = new System.Windows.Forms.ComboBox();
            this.ServerURLLabel = new System.Windows.Forms.Label();
            this.SubmissionNumber = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ApplicationID = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.BOMCaseID = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.SubmissionTime = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.listView1 = new System.Windows.Forms.ListView();
            this.SearchBomCasebtn = new System.Windows.Forms.Button();
            this.BomcaseNumberlabel = new System.Windows.Forms.Label();
            this.BomCaseNumberValLabel = new System.Windows.Forms.Label();
            this.SubmittedRangeLabel = new System.Windows.Forms.Label();
            this.SubmittedRangeValLabel = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.ClerarListBtn = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // linkLabel1
            // 
            this.linkLabel1.AutoSize = true;
            this.linkLabel1.Location = new System.Drawing.Point(127, 228);
            this.linkLabel1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(0, 13);
            this.linkLabel1.TabIndex = 0;
            this.linkLabel1.TabStop = true;
            // 
            // CreateBOMCaseBtn
            // 
            this.CreateBOMCaseBtn.Location = new System.Drawing.Point(892, 557);
            this.CreateBOMCaseBtn.Margin = new System.Windows.Forms.Padding(2);
            this.CreateBOMCaseBtn.Name = "CreateBOMCaseBtn";
            this.CreateBOMCaseBtn.Size = new System.Drawing.Size(121, 28);
            this.CreateBOMCaseBtn.TabIndex = 2;
            this.CreateBOMCaseBtn.Text = "Create BOM case";
            this.CreateBOMCaseBtn.UseVisualStyleBackColor = true;
            this.CreateBOMCaseBtn.Click += new System.EventHandler(this.CreateBOMCase_Click);
            // 
            // helloWorldLabel
            // 
            this.helloWorldLabel.AutoSize = true;
            this.helloWorldLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.helloWorldLabel.Location = new System.Drawing.Point(95, 20);
            this.helloWorldLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.helloWorldLabel.Name = "helloWorldLabel";
            this.helloWorldLabel.Size = new System.Drawing.Size(316, 26);
            this.helloWorldLabel.TabIndex = 3;
            this.helloWorldLabel.Text = "Manual creation of a BOM case";
            // 
            // Count
            // 
            this.Count.AutoSize = true;
            this.Count.ForeColor = System.Drawing.SystemColors.MenuHighlight;
            this.Count.Location = new System.Drawing.Point(97, 423);
            this.Count.Name = "Count";
            this.Count.Size = new System.Drawing.Size(0, 13);
            this.Count.TabIndex = 0;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.ForeColor = System.Drawing.SystemColors.MenuHighlight;
            this.label2.Location = new System.Drawing.Point(97, 423);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(0, 13);
            this.label2.TabIndex = 0;
            // 
            // BOMCaseIDLabel
            // 
            this.BOMCaseIDLabel.AutoSize = true;
            this.BOMCaseIDLabel.Location = new System.Drawing.Point(39, 72);
            this.BOMCaseIDLabel.Name = "BOMCaseIDLabel";
            this.BOMCaseIDLabel.Size = new System.Drawing.Size(69, 13);
            this.BOMCaseIDLabel.TabIndex = 8;
            this.BOMCaseIDLabel.Text = "BOMCaseID:";
            this.BOMCaseIDLabel.UseMnemonic = false;
            // 
            // BOMCaseIDtextBox
            // 
            this.BOMCaseIDtextBox.Cursor = System.Windows.Forms.Cursors.SizeAll;
            this.BOMCaseIDtextBox.Enabled = false;
            this.BOMCaseIDtextBox.Location = new System.Drawing.Point(114, 69);
            this.BOMCaseIDtextBox.Name = "BOMCaseIDtextBox";
            this.BOMCaseIDtextBox.Size = new System.Drawing.Size(249, 20);
            this.BOMCaseIDtextBox.TabIndex = 9;
            this.BOMCaseIDtextBox.Text = "[GUID]";
            this.BOMCaseIDtextBox.TextChanged += new System.EventHandler(this.BOMCaseIDtextBox_TextChanged);
            // 
            // CertificatecomboBox
            // 
            this.CertificatecomboBox.Enabled = false;
            this.CertificatecomboBox.FormattingEnabled = true;
            this.CertificatecomboBox.Location = new System.Drawing.Point(561, 72);
            this.CertificatecomboBox.Name = "CertificatecomboBox";
            this.CertificatecomboBox.Size = new System.Drawing.Size(259, 21);
            this.CertificatecomboBox.TabIndex = 10;
            this.CertificatecomboBox.DropDownStyle = ComboBoxStyle.DropDownList;

            this.CertificatecomboBox.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // CertificateLabel
            // 
            this.CertificateLabel.AutoSize = true;
            this.CertificateLabel.Location = new System.Drawing.Point(492, 76);
            this.CertificateLabel.Name = "CertificateLabel";
            this.CertificateLabel.Size = new System.Drawing.Size(54, 13);
            this.CertificateLabel.TabIndex = 11;
            this.CertificateLabel.Text = "Certificate";
            this.CertificateLabel.UseMnemonic = false;
            // 
            // CVRLabel
            // 
            //this.CVRLabel.AutoSize = true;
            //this.CVRLabel.Location = new System.Drawing.Point(548, 15);
            //this.CVRLabel.Name = "CVRLabel";
            //this.CVRLabel.Size = new System.Drawing.Size(32, 13);
            //this.CVRLabel.TabIndex = 13;
            //this.CVRLabel.Text = "CVR:";
            //this.CVRLabel.UseMnemonic = false;
            // 
            // CvrTextBox
            // 
            //this.CvrTextBox.Cursor = System.Windows.Forms.Cursors.SizeAll;
            //this.CvrTextBox.Location = new System.Drawing.Point(586, 12);
            //this.CvrTextBox.Name = "CvrTextBox";
            //this.CvrTextBox.Size = new System.Drawing.Size(121, 20);
            //this.CvrTextBox.TabIndex = 14;
            // 
            // CertificateTestbtn
            // 
            this.CertificateTestbtn.Enabled = false;
            this.CertificateTestbtn.Location = new System.Drawing.Point(609, 111);
            this.CertificateTestbtn.Name = "CertificateTestbtn";
            this.CertificateTestbtn.Size = new System.Drawing.Size(174, 23);
            this.CertificateTestbtn.TabIndex = 15;
            this.CertificateTestbtn.Text = "Test certificate";
            this.CertificateTestbtn.UseVisualStyleBackColor = true;
            this.CertificateTestbtn.Click += new System.EventHandler(this.CertificateTestbtn_Click);
            // 
            // ServerURLComboBox
            // 
            this.ServerURLComboBox.FormattingEnabled = true;
            this.ServerURLComboBox.Items.AddRange(new object[] {
             "<none>",
            "TEST: https://service-es.bygogmiljoe.dk/",
            "PROD: https://service.bygogmiljoe.dk/"});
            this.ServerURLComboBox.Location = new System.Drawing.Point(561, 45);
            this.ServerURLComboBox.Name = "ServerURLComboBox";
            this.ServerURLComboBox.Size = new System.Drawing.Size(259, 21);
            this.ServerURLComboBox.TabIndex = 16;
            ServerURLComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            this.ServerURLComboBox.SelectedIndexChanged += new System.EventHandler(this.ServerURLComboBox_SelectedIndexChanged);
            // 
            // ServerURLLabel
            // 
            this.ServerURLLabel.AutoSize = true;
            this.ServerURLLabel.Location = new System.Drawing.Point(492, 53);
            this.ServerURLLabel.Name = "ServerURLLabel";
            this.ServerURLLabel.Size = new System.Drawing.Size(63, 13);
            this.ServerURLLabel.TabIndex = 17;
            this.ServerURLLabel.Text = "ServerURL:";
            this.ServerURLLabel.UseMnemonic = false;
            // 
            // SubmissionNumber
            // 
            this.SubmissionNumber.Text = "SubmissionNumber";
            this.SubmissionNumber.Width = 120;
            // 
            // ApplicationID
            // 
            this.ApplicationID.Text = "ApplicationID";
            this.ApplicationID.Width = 120;
            // 
            // BOMCaseID
            // 
            this.BOMCaseID.Text = "BOMCaseID";
            this.BOMCaseID.Width = 85;
            // 
            // SubmissionTime
            // 
            this.SubmissionTime.Text = "SubmissionTime";
            this.SubmissionTime.Width = 120;
            // 
            // listView1
            // 
            this.listView1.CheckBoxes = true;
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.SubmissionNumber,
            this.ApplicationID,
            this.BOMCaseID,
            this.SubmissionTime});
            this.listView1.Location = new System.Drawing.Point(12, 244);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(771, 277);
            this.listView1.TabIndex = 12;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            this.listView1.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.listView1_ItemChecked);
            // 
            // SearchBomCasebtn
            // 
            this.SearchBomCasebtn.Enabled = false;
            this.SearchBomCasebtn.Location = new System.Drawing.Point(42, 106);
            this.SearchBomCasebtn.Margin = new System.Windows.Forms.Padding(2);
            this.SearchBomCasebtn.Name = "SearchBomCasebtn";
            this.SearchBomCasebtn.Size = new System.Drawing.Size(121, 28);
            this.SearchBomCasebtn.TabIndex = 18;
            this.SearchBomCasebtn.Text = "Search BOM case";
            this.SearchBomCasebtn.UseVisualStyleBackColor = true;
            this.SearchBomCasebtn.Click += new System.EventHandler(this.SearchBomCasebtn_Click);
            // 
            // BomcaseNumberlabel
            // 
            this.BomcaseNumberlabel.AutoSize = true;
            this.BomcaseNumberlabel.Location = new System.Drawing.Point(42, 140);
            this.BomcaseNumberlabel.Name = "BomcaseNumberlabel";
            this.BomcaseNumberlabel.Size = new System.Drawing.Size(95, 13);
            this.BomcaseNumberlabel.TabIndex = 19;
            this.BomcaseNumberlabel.Text = "BOMCaseNumber:";
            // 
            // BomCaseNumberValLabel
            // 
            this.BomCaseNumberValLabel.AutoSize = true;
            this.BomCaseNumberValLabel.Location = new System.Drawing.Point(143, 140);
            this.BomCaseNumberValLabel.Name = "BomCaseNumberValLabel";
            this.BomCaseNumberValLabel.Size = new System.Drawing.Size(0, 13);
            this.BomCaseNumberValLabel.TabIndex = 20;
            // 
            // SubmittedRangeLabel
            // 
            this.SubmittedRangeLabel.AutoSize = true;
            this.SubmittedRangeLabel.Location = new System.Drawing.Point(42, 164);
            this.SubmittedRangeLabel.Name = "SubmittedRangeLabel";
            this.SubmittedRangeLabel.Size = new System.Drawing.Size(130, 13);
            this.SubmittedRangeLabel.TabIndex = 21;
            this.SubmittedRangeLabel.Text = "Submission range in eDoc";
            // 
            // SubmittedRangeValLabel
            // 
            this.SubmittedRangeValLabel.AutoSize = true;
            this.SubmittedRangeValLabel.Location = new System.Drawing.Point(168, 164);
            this.SubmittedRangeValLabel.Name = "SubmittedRangeValLabel";
            this.SubmittedRangeValLabel.Size = new System.Drawing.Size(0, 13);
            this.SubmittedRangeValLabel.TabIndex = 22;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(11, 215);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(494, 26);
            this.label1.TabIndex = 23;
            this.label1.Text = "Previous submitted applications (missing in eDoc)";
            // 
            // ClerarListBtn
            // 
            this.ClerarListBtn.Location = new System.Drawing.Point(788, 308);
            this.ClerarListBtn.Margin = new System.Windows.Forms.Padding(2);
            this.ClerarListBtn.Name = "ClerarListBtn";
            this.ClerarListBtn.Size = new System.Drawing.Size(121, 28);
            this.ClerarListBtn.TabIndex = 24;
            this.ClerarListBtn.Text = "Clear list";
            this.ClerarListBtn.UseVisualStyleBackColor = true;
            this.ClerarListBtn.Click += new System.EventHandler(this.ClerarListBtn_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1388, 765);
            this.Controls.Add(this.ClerarListBtn);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.SubmittedRangeValLabel);
            this.Controls.Add(this.SubmittedRangeLabel);
            this.Controls.Add(this.BomCaseNumberValLabel);
            this.Controls.Add(this.BomcaseNumberlabel);
            this.Controls.Add(this.SearchBomCasebtn);
            this.Controls.Add(this.ServerURLLabel);
            this.Controls.Add(this.ServerURLComboBox);
            this.Controls.Add(this.CertificateTestbtn);
            //this.Controls.Add(this.CvrTextBox);
            //this.Controls.Add(this.CVRLabel);
            this.Controls.Add(this.listView1);
            this.Controls.Add(this.CertificateLabel);
            this.Controls.Add(this.CertificatecomboBox);
            this.Controls.Add(this.BOMCaseIDtextBox);
            this.Controls.Add(this.BOMCaseIDLabel);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.Count);
            this.Controls.Add(this.helloWorldLabel);
            this.Controls.Add(this.CreateBOMCaseBtn);
            this.Controls.Add(this.linkLabel1);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.Button CreateBOMCaseBtn;
        private System.Windows.Forms.Label helloWorldLabel;
        private System.Windows.Forms.Label Count;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label BOMCaseIDLabel;
        private System.Windows.Forms.TextBox BOMCaseIDtextBox;
        private System.Windows.Forms.ComboBox CertificatecomboBox;
        private System.Windows.Forms.Label CertificateLabel;
        //private System.Windows.Forms.Label CVRLabel;
        //private System.Windows.Forms.TextBox CvrTextBox;
        private System.Windows.Forms.Button CertificateTestbtn;
        private System.Windows.Forms.ComboBox ServerURLComboBox;
        private System.Windows.Forms.Label ServerURLLabel;
        private System.Windows.Forms.ColumnHeader SubmissionNumber;
        private System.Windows.Forms.ColumnHeader ApplicationID;
        private System.Windows.Forms.ColumnHeader BOMCaseID;
        private System.Windows.Forms.ColumnHeader SubmissionTime;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.Button SearchBomCasebtn;
        private System.Windows.Forms.Label BomcaseNumberlabel;
        private System.Windows.Forms.Label BomCaseNumberValLabel;
        private System.Windows.Forms.Label SubmittedRangeLabel;
        private System.Windows.Forms.Label SubmittedRangeValLabel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button ClerarListBtn;
    }
}

