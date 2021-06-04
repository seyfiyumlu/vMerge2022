namespace alexbegh.vMerge.Options
{
    partial class WinFormProfilesPage
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.wpfControl = new System.Windows.Forms.Integration.ElementHost();
            this.SuspendLayout();
            // 
            // wpfControl
            // 
            this.wpfControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.wpfControl.Location = new System.Drawing.Point(0, 0);
            this.wpfControl.Name = "wpfControl";
            this.wpfControl.Size = new System.Drawing.Size(150, 150);
            this.wpfControl.TabIndex = 0;
            this.wpfControl.Text = "wpfControl";
            this.wpfControl.Child = null;
            // 
            // WinFormProfilesPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.wpfControl);
            this.Name = "WinFormProfilesPage";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Integration.ElementHost wpfControl;
    }
}
