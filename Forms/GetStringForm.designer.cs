namespace InputMaster.Forms
{
  partial class GetStringForm
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
      this.Button = new System.Windows.Forms.Button();
      this.RichTextBox = new InputMaster.Forms.RichTextBoxPlus();
      this.SuspendLayout();
      // 
      // Button
      // 
      this.Button.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.Button.DialogResult = System.Windows.Forms.DialogResult.OK;
      this.Button.Location = new System.Drawing.Point(834, 712);
      this.Button.Name = "Button";
      this.Button.Size = new System.Drawing.Size(75, 23);
      this.Button.TabIndex = 1;
      this.Button.Text = "OK";
      this.Button.UseVisualStyleBackColor = true;
      this.Button.Click += new System.EventHandler(this.Button_Click);
      // 
      // RichTextBox
      // 
      this.RichTextBox.AcceptsTab = true;
      this.RichTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.RichTextBox.AutoWordSelection = true;
      this.RichTextBox.BackColor = System.Drawing.Color.White;
      this.RichTextBox.Font = new System.Drawing.Font("Consolas", 11F);
      this.RichTextBox.ForeColor = System.Drawing.Color.Black;
      this.RichTextBox.Location = new System.Drawing.Point(12, 12);
      this.RichTextBox.Name = "RichTextBox";
      this.RichTextBox.Size = new System.Drawing.Size(897, 694);
      this.RichTextBox.TabIndex = 0;
      this.RichTextBox.Text = "";
      this.RichTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.RichTextBox_KeyDown);
      // 
      // GetStringForm
      // 
      this.AcceptButton = this.Button;
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(921, 747);
      this.Controls.Add(this.Button);
      this.Controls.Add(this.RichTextBox);
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
      this.Name = "GetStringForm";
      this.ShowIcon = false;
      this.ShowInTaskbar = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
      this.Text = "GetStringForm";
      this.TopMost = true;
      this.Shown += new System.EventHandler(this.GetStringForm_Shown);
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.Button Button;
    private RichTextBoxPlus RichTextBox;
  }
}
