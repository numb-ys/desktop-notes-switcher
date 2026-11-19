using System;
using System.Drawing;
using System.Windows.Forms;

internal sealed class LanguageWindow : Form
{
    private readonly LanguageCard _chinese;
    private readonly LanguageCard _english;

    public string Language { get; private set; }

    public LanguageWindow(bool preferChinese)
    {
        Text = "安装语言 / Setup language";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        ClientSize = new Size(468, 218);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = SystemFonts.MessageBoxFont;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(245, 247, 250);
        ForeColor = Color.FromArgb(33, 44, 56);

        var prompt = new Label
        {
            Text = "请选择安装向导和软件界面的语言。\r\nChoose the language for setup and the app.",
            Location = new Point(20, 20),
            Size = new Size(428, 44)
        };
        _chinese = new LanguageCard("简体中文", "Chinese (Simplified)", new Point(20, 73), Font);
        _english = new LanguageCard("English", "英文", new Point(240, 73), Font);
        _chinese.Click += (sender, args) => SelectLanguage(true);
        _english.Click += (sender, args) => SelectLanguage(false);

        var cancel = new Button
        {
            Text = "取消 / Cancel", DialogResult = DialogResult.Cancel,
            Location = new Point(221, 167), Size = new Size(97, 33)
        };
        var next = new Button
        {
            Text = "继续 / Continue", DialogResult = DialogResult.OK,
            Location = new Point(326, 167), Size = new Size(122, 33)
        };
        Controls.AddRange(new Control[] { prompt, _chinese, _english, cancel, next });
        AcceptButton = next;
        CancelButton = cancel;
        SelectLanguage(preferChinese);
    }

    private void SelectLanguage(bool chinese)
    {
        Language = chinese ? "chs" : "en";
        _chinese.SetSelected(chinese);
        _english.SetSelected(!chinese);
    }

    private sealed class LanguageCard : Panel
    {
        private readonly CheckBox _checked;
        private readonly Font _titleFont;
        private bool _selected;

        public LanguageCard(string title, string translation, Point position, Font font)
        {
            Location = position;
            Size = new Size(208, 76);
            Cursor = Cursors.Hand;
            Font = font;
            _titleFont = new Font(font, FontStyle.Bold);
            _checked = new CheckBox
            {
                AutoCheck = false,
                AccessibleName = title,
                Location = new Point(15, 19),
                Size = new Size(22, 22)
            };
            var name = new Label { Text = title, Font = _titleFont, Location = new Point(43, 19), AutoSize = true };
            var details = new Label
            {
                Text = translation, ForeColor = Color.FromArgb(80, 92, 108),
                Location = new Point(43, 43), AutoSize = true
            };
            _checked.Click += (sender, args) => OnClick(args);
            name.Click += (sender, args) => OnClick(args);
            details.Click += (sender, args) => OnClick(args);
            Controls.AddRange(new Control[] { _checked, name, details });
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            _checked.Checked = selected;
            BackColor = selected ? Color.FromArgb(230, 240, 252) : Color.White;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs args)
        {
            base.OnPaint(args);
            using (var pen = new Pen(_selected ? Color.FromArgb(37, 99, 161) : Color.FromArgb(188, 197, 208)))
                args.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                _titleFont.Dispose();
        }
    }
}
