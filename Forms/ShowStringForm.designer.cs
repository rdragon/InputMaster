namespace InputMaster.Forms
{
  partial class ShowStringForm
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
      this.Rtb = new InputMaster.Forms.RichTextBoxPlus();
      this.SuspendLayout();
      // 
      // Rtb
      // 
      this.Rtb.Dock = System.Windows.Forms.DockStyle.Fill;
      this.Rtb.Location = new System.Drawing.Point(0, 0);
      this.Rtb.Name = "Rtb";
      this.Rtb.ReadOnly = true;
      this.Rtb.Size = new System.Drawing.Size(1184, 766);
      this.Rtb.TabIndex = 0;
      this.Rtb.Text = "";
      this.Rtb.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Rtb_KeyDown);
      // 
      // TextForm
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(1184, 766);
      this.Controls.Add(this.Rtb);
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
      this.Name = "TextForm";
      this.Text = "Message - InputMaster";
      this.TopMost = true;
      this.ResumeLayout(false);

    }

    #endregion

    private InputMaster.Forms.RichTextBoxPlus Rtb;

  }
}
