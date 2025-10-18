namespace OllamaChat
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            txtChat = new RichTextBox();
            panel1 = new Panel();
            btnClearChat = new Button();
            chkStream = new CheckBox();
            btnSend = new Button();
            txtPrompt = new TextBox();
            cmbModels = new ComboBox();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // txtChat
            // 
            txtChat.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtChat.Location = new Point(13, 12);
            txtChat.Name = "txtChat";
            txtChat.Size = new Size(866, 522);
            txtChat.TabIndex = 0;
            txtChat.Text = "";
            // 
            // panel1
            // 
            panel1.Controls.Add(cmbModels);
            panel1.Controls.Add(btnClearChat);
            panel1.Controls.Add(chkStream);
            panel1.Controls.Add(btnSend);
            panel1.Controls.Add(txtPrompt);
            panel1.Dock = DockStyle.Bottom;
            panel1.Location = new Point(0, 540);
            panel1.Name = "panel1";
            panel1.Size = new Size(891, 47);
            panel1.TabIndex = 4;
            // 
            // btnClearChat
            // 
            btnClearChat.Location = new Point(765, 12);
            btnClearChat.Name = "btnClearChat";
            btnClearChat.Size = new Size(114, 23);
            btnClearChat.TabIndex = 7;
            btnClearChat.Text = "Clear Chat History";
            btnClearChat.UseVisualStyleBackColor = true;
            // 
            // chkStream
            // 
            chkStream.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            chkStream.AutoSize = true;
            chkStream.Location = new Point(484, 14);
            chkStream.Name = "chkStream";
            chkStream.Size = new Size(113, 19);
            chkStream.TabIndex = 6;
            chkStream.Text = "Stream response";
            chkStream.UseVisualStyleBackColor = true;
            // 
            // btnSend
            // 
            btnSend.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            btnSend.Location = new Point(403, 12);
            btnSend.Name = "btnSend";
            btnSend.Size = new Size(75, 23);
            btnSend.TabIndex = 5;
            btnSend.Text = "&Send";
            btnSend.UseVisualStyleBackColor = true;
            // 
            // txtPrompt
            // 
            txtPrompt.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            txtPrompt.Location = new Point(12, 12);
            txtPrompt.Name = "txtPrompt";
            txtPrompt.Size = new Size(385, 23);
            txtPrompt.TabIndex = 4;
            // 
            // cmbModels
            // 
            cmbModels.FormattingEnabled = true;
            cmbModels.Location = new Point(603, 12);
            cmbModels.Name = "cmbModels";
            cmbModels.Size = new Size(156, 23);
            cmbModels.TabIndex = 8;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(891, 587);
            Controls.Add(panel1);
            Controls.Add(txtChat);
            Name = "Form1";
            Text = "Ollama Chat";
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private RichTextBox txtChat;
        private Panel panel1;
        private CheckBox chkStream;
        private Button btnSend;
        private TextBox txtPrompt;
        private Button btnClearChat;
        private ComboBox cmbModels;
    }
}
