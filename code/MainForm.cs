using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace SchoolPrintPro
{
    //マウス操作
    [DllImport("user32.dll")]
    private static extern int SendMessage(IntPtr hwnd, int Msg, int wParam, int lParam);
    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HTCAPTION = 0x2;

    private List<PrintElement> elements = new List<PrintElement>();
    private PrintElement selectedElement = null;
    private float zoom = 1.0f;
    private PointF offset = new PointF(50, 50);
    private Point lastMousePos;
    private bool isPanning = false;

    public MainForm()
    {
        this.Text = "Professional Print Engine";
        this.Size = new Size(1280, 800);
        this.DoubleBuffered = true;
        this.keyPreview = true;

        //イベント
        this.MouseDown += OnMouseDown; 
        this.MouseMove += OnMouseMove;
        this.MouseUp += OnMouseUp;
        this.MouseWheel += OnMouseWheel;
        this.DragEnter += (s, e) => { if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; };
        this.DragDrop += OnDragDrop;
        this.KeyDown += (s, e) => { if (e.KeyCode == Keys.Space) isPanning = true; };
        this.KeyUp += (s, e) => { if (e.KeyCode == Keys.Space) isPanning = false; };
        this.Paint += OnPaint;
        //UIボタン
        var pnl = new Panel { Dock = DockStyle.Left, Width = 150, BackColor = Color.FromArgb(30, 30, 30) };
        AddBtn(pnl, "TEXT", (s, e) => AddElement(new PrintElement { Type = EType.Text, Content = "New Question", Rect = new RectangleF(100, 100, 200, 50) }));
        AddBtn(pnl, "BOX", (s, e) => AddElement(new PrintElement { Type = EType.Box, Rect = new RectangleF(100, 100, 100, 100) }));
        AddBtn(pnl, "PRINT/PDF", (s, e) => StartPrint());
        this.Controls.Add(pnl);
         private void AddBtn(Control p, string txt, EventHandler ev)
        {
            var b = new Button { Text = txt, Dock = DockStyle.Top, Height = 60, FlatStyle = FlatStyle.Flat, ForeColor = Color.White };
            b.Click += ev;
            p.Controls.Add(b);
        }

        private void AddElement(PrintElement el) { elements.Add(el); selectedElement = el; Invalidate(); }

        private void OnDragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files)
            {
                try {
                    var img = Image.FromFile(file);
                    AddElement(new PrintElement { Type = EType.Image, Img = img, Rect = new RectangleF(100, 100, img.Width/2, img.Height/2) });
                } catch { /* Ignore non-image */ }
            }
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TranslateTransform(offset.X, offset.Y);
            e.Graphics.ScaleTransform(zoom, zoom);

            // A4 Canvas
            e.Graphics.FillRectangle(Brushes.White, 0, 0, 827, 1169);
            e.Graphics.DrawRectangle(Pens.Black, 0, 0, 827, 1169);

            foreach (var el in elements)
            {
                if (el.Type == EType.Text) e.Graphics.DrawString(el.Content, this.Font, Brushes.Black, el.Rect);
                else if (el.Type == EType.Box) e.Graphics.DrawRectangle(Pens.Black, el.Rect.X, el.Rect.Y, el.Rect.Width, el.Rect.Height);
                else if (el.Type == EType.Image && el.Img != null) e.Graphics.DrawImage(el.Img, el.Rect);
                
                if (el == selectedElement) e.Graphics.DrawRectangle(Pens.Red, el.Rect.X, el.Rect.Y, el.Rect.Width, el.Rect.Height);
            }
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (isPanning) { lastMousePos = e.Location; return; }
            
            var p = ApplyInverse(e.Location);
            selectedElement = elements.FindLast(x => x.Rect.Contains(p));
            if (selectedElement != null) lastMousePos = e.Location;
            Invalidate();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (isPanning && e.Button == MouseButtons.Left)
            {
                offset.X += (e.X - lastMousePos.X);
                offset.Y += (e.Y - lastMousePos.Y);
                lastMousePos = e.Location;
                Invalidate();
                return;
            }

            if (selectedElement != null && e.Button == MouseButtons.Left)
            {
                var p1 = ApplyInverse(lastMousePos);
                var p2 = ApplyInverse(e.Location);
                selectedElement.Rect.X += (p2.X - p1.X);
                selectedElement.Rect.Y += (p2.Y - p1.Y);
                lastMousePos = e.Location;
                Invalidate();
            }
        }

        private void OnMouseUp(object sender, MouseEventArgs e) => lastMousePos = Point.Empty;

        private void OnMouseWheel(object sender, MouseEventArgs e)
        {
            float oldZoom = zoom;
            zoom += e.Delta > 0 ? 0.1f : -0.1f;
            if (zoom < 0.1f) zoom = 0.1f;
            Invalidate();
        }

        private PointF ApplyInverse(Point p) => new PointF((p.X - offset.X) / zoom, (p.Y - offset.Y) / zoom);

        private void StartPrint()
        {
            var pd = new PrintDocument();
            pd.PrintPage += (s, e) => {
                foreach (var el in elements) {
                    if (el.Type == EType.Text) e.Graphics.DrawString(el.Content, this.Font, Brushes.Black, el.Rect);
                    else if (el.Type == EType.Box) e.Graphics.DrawRectangle(Pens.Black, el.Rect.X, el.Rect.Y, el.Rect.Width, el.Rect.Height);
                    else if (el.Type == EType.Image) e.Graphics.DrawImage(el.Img, el.Rect);
                }
            };
            var dlg = new PrintDialog { Document = pd };
            if (dlg.ShowDialog() == DialogResult.OK) pd.Print();
        }

        private enum EType { Text, Box, Image }
        private class PrintElement {
            public EType Type;
            public string Content;
            public RectangleF Rect;
            public Image Img;
        }
    }
}
