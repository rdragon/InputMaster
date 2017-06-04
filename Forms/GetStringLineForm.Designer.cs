using System.ComponentModel;
using System.Windows.Forms;

namespace InputMaster.Forms
{
  partial class GetStringLineForm
  {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private IContainer components = null;

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
      this.TextBox = new System.Windows.Forms.TextBox();
      this.SuspendLayout();
      // 
      // Button
      // 
      this.Button.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.Button.Location = new System.Drawing.Point(644, 12);
      this.Button.Name = "Button";
      this.Button.Size = new System.Drawing.Size(75, 24);
      this.Button.TabIndex = 1;
      this.Button.Text = "OK";
      this.Button.UseVisualStyleBackColor = true;
      this.Button.Click += new System.EventHandler(this.Button_Click);
      // 
      // TextBox
      // 
      this.TextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
      | System.Windows.Forms.AnchorStyles.Right)));
      this.TextBox.BackColor = System.Drawing.Color.White;
      this.TextBox.Font = new System.Drawing.Font("Consolas", 11F);
      this.TextBox.ForeColor = System.Drawing.Color.Black;
      this.TextBox.Location = new System.Drawing.Point(12, 13);
      this.TextBox.Name = "TextBox";
      this.TextBox.Size = new System.Drawing.Size(626, 25);
      this.TextBox.TabIndex = 0;
      this.TextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TextBox_KeyDown);
      // 
      // GetStringLineForm
      // 
      this.AcceptButton = this.Button;
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(731, 51);
      this.Controls.Add(this.TextBox);
      this.Controls.Add(this.Button);
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
      this.Name = "GetStringLineForm";
      this.ShowIcon = false;
      this.ShowInTaskbar = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
      this.Text = "GetStringLineForm";
      this.TopMost = true;
      this.Shown += new System.EventHandler(this.GetStringLineForm_Shown);
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private Button Button;
    private TextBox TextBox;
  }
}