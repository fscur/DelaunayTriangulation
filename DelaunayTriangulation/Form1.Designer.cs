namespace Trianglex
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsbMode = new System.Windows.Forms.ToolStripSplitButton();
            this.tsmSelect = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmMove = new System.Windows.Forms.ToolStripMenuItem();
            this.tsbAddMode = new System.Windows.Forms.ToolStripSplitButton();
            this.tsmAddPoints = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.tsmConstrainedEdge = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.tsmClearPoints = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSplitButton2 = new System.Windows.Forms.ToolStripSplitButton();
            this.tsmShowPSLG = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmShowFlippableEdges = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.bentleyOttmannToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbMode,
            this.tsbAddMode,
            this.toolStripSplitButton2,
            this.toolStripSeparator1,
            this.toolStripDropDownButton1});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1019, 25);
            this.toolStrip1.TabIndex = 5;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tsbMode
            // 
            this.tsbMode.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbMode.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmSelect,
            this.tsmMove});
            this.tsbMode.Image = ((System.Drawing.Image)(resources.GetObject("tsbMode.Image")));
            this.tsbMode.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbMode.Name = "tsbMode";
            this.tsbMode.Size = new System.Drawing.Size(32, 22);
            this.tsbMode.Text = "toolStripSplitButton3";
            this.tsbMode.ButtonClick += new System.EventHandler(this.tsbMode_ButtonClick);
            // 
            // tsmSelect
            // 
            this.tsmSelect.Image = ((System.Drawing.Image)(resources.GetObject("tsmSelect.Image")));
            this.tsmSelect.Name = "tsmSelect";
            this.tsmSelect.Size = new System.Drawing.Size(105, 22);
            this.tsmSelect.Text = "Select";
            this.tsmSelect.Click += new System.EventHandler(this.tsmSelect_Click);
            // 
            // tsmMove
            // 
            this.tsmMove.Image = ((System.Drawing.Image)(resources.GetObject("tsmMove.Image")));
            this.tsmMove.Name = "tsmMove";
            this.tsmMove.Size = new System.Drawing.Size(105, 22);
            this.tsmMove.Text = "Move";
            this.tsmMove.Click += new System.EventHandler(this.tsmMove_Click);
            // 
            // tsbAddMode
            // 
            this.tsbAddMode.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbAddMode.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmAddPoints,
            this.toolStripSeparator3,
            this.tsmConstrainedEdge,
            this.toolStripSeparator2,
            this.tsmClearPoints});
            this.tsbAddMode.Image = ((System.Drawing.Image)(resources.GetObject("tsbAddMode.Image")));
            this.tsbAddMode.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbAddMode.Name = "tsbAddMode";
            this.tsbAddMode.Size = new System.Drawing.Size(32, 22);
            this.tsbAddMode.Text = "toolStripSplitButton1";
            this.tsbAddMode.ButtonClick += new System.EventHandler(this.tsbAddMode_ButtonClick);
            // 
            // tsmAddPoints
            // 
            this.tsmAddPoints.CheckOnClick = true;
            this.tsmAddPoints.Image = ((System.Drawing.Image)(resources.GetObject("tsmAddPoints.Image")));
            this.tsmAddPoints.Name = "tsmAddPoints";
            this.tsmAddPoints.Size = new System.Drawing.Size(192, 22);
            this.tsmAddPoints.Text = "Add Points";
            this.tsmAddPoints.Click += new System.EventHandler(this.tsmAddPoints_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(189, 6);
            // 
            // tsmConstrainedEdge
            // 
            this.tsmConstrainedEdge.CheckOnClick = true;
            this.tsmConstrainedEdge.Image = ((System.Drawing.Image)(resources.GetObject("tsmConstrainedEdge.Image")));
            this.tsmConstrainedEdge.Name = "tsmConstrainedEdge";
            this.tsmConstrainedEdge.Size = new System.Drawing.Size(192, 22);
            this.tsmConstrainedEdge.Text = "Add Constrained Edge";
            this.tsmConstrainedEdge.Click += new System.EventHandler(this.tsmConstrainedEdge_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(189, 6);
            // 
            // tsmClearPoints
            // 
            this.tsmClearPoints.Image = ((System.Drawing.Image)(resources.GetObject("tsmClearPoints.Image")));
            this.tsmClearPoints.Name = "tsmClearPoints";
            this.tsmClearPoints.Size = new System.Drawing.Size(192, 22);
            this.tsmClearPoints.Text = "Clear";
            this.tsmClearPoints.Click += new System.EventHandler(this.tsmClearPoints_Click);
            // 
            // toolStripSplitButton2
            // 
            this.toolStripSplitButton2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripSplitButton2.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmShowPSLG,
            this.tsmShowFlippableEdges});
            this.toolStripSplitButton2.Image = ((System.Drawing.Image)(resources.GetObject("toolStripSplitButton2.Image")));
            this.toolStripSplitButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripSplitButton2.Name = "toolStripSplitButton2";
            this.toolStripSplitButton2.Size = new System.Drawing.Size(32, 22);
            this.toolStripSplitButton2.Text = "toolStripSplitButton2";
            // 
            // tsmShowPSLG
            // 
            this.tsmShowPSLG.CheckOnClick = true;
            this.tsmShowPSLG.Image = ((System.Drawing.Image)(resources.GetObject("tsmShowPSLG.Image")));
            this.tsmShowPSLG.Name = "tsmShowPSLG";
            this.tsmShowPSLG.Size = new System.Drawing.Size(188, 22);
            this.tsmShowPSLG.Text = "Show PSLG";
            this.tsmShowPSLG.Click += new System.EventHandler(this.tsmShowPSLG_Click);
            // 
            // tsmShowFlippableEdges
            // 
            this.tsmShowFlippableEdges.CheckOnClick = true;
            this.tsmShowFlippableEdges.Name = "tsmShowFlippableEdges";
            this.tsmShowFlippableEdges.Size = new System.Drawing.Size(188, 22);
            this.tsmShowFlippableEdges.Text = "Show Flippable Edges";
            this.tsmShowFlippableEdges.Click += new System.EventHandler(this.tsmShowFlippableEdges_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripDropDownButton1
            // 
            this.toolStripDropDownButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1,
            this.toolStripMenuItem2,
            this.bentleyOttmannToolStripMenuItem});
            this.toolStripDropDownButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripDropDownButton1.Image")));
            this.toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            this.toolStripDropDownButton1.Size = new System.Drawing.Size(29, 22);
            this.toolStripDropDownButton1.Text = "toolStripDropDownButton1";
            this.toolStripDropDownButton1.ToolTipText = "Triangulate";
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(191, 22);
            this.toolStripMenuItem1.Text = "Delaunay";
            this.toolStripMenuItem1.Click += new System.EventHandler(this.tsmDelaunay_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(191, 22);
            this.toolStripMenuItem2.Text = "Conforming Delaunay";
            this.toolStripMenuItem2.Click += new System.EventHandler(this.tsmConformingDelaunay_Click);
            // 
            // bentleyOttmannToolStripMenuItem
            // 
            this.bentleyOttmannToolStripMenuItem.Name = "bentleyOttmannToolStripMenuItem";
            this.bentleyOttmannToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
            this.bentleyOttmannToolStripMenuItem.Text = "Bentley Ottmann";
            this.bentleyOttmannToolStripMenuItem.Click += new System.EventHandler(this.bentleyOttmannToolStripMenuItem_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.ClientSize = new System.Drawing.Size(1019, 654);
            this.Controls.Add(this.toolStrip1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem2;
        private System.Windows.Forms.ToolStripSplitButton tsbAddMode;
        private System.Windows.Forms.ToolStripMenuItem tsmAddPoints;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem tsmClearPoints;
        private System.Windows.Forms.ToolStripMenuItem tsmConstrainedEdge;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripSplitButton toolStripSplitButton2;
        private System.Windows.Forms.ToolStripMenuItem tsmShowPSLG;
        private System.Windows.Forms.ToolStripMenuItem tsmShowFlippableEdges;
        private System.Windows.Forms.ToolStripSplitButton tsbMode;
        private System.Windows.Forms.ToolStripMenuItem tsmSelect;
        private System.Windows.Forms.ToolStripMenuItem tsmMove;
        private System.Windows.Forms.ToolStripMenuItem bentleyOttmannToolStripMenuItem;
    }
}

