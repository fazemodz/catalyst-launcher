using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Catalyst_Launcher.Dialogs;

public partial class DocViewerWindow : Window
{
    private DocViewerWindow() { InitializeComponent(); }

    public static void Show(string title, string filePath, Window owner)
    {
        string content = File.Exists(filePath)
            ? File.ReadAllText(filePath)
            : $"# {title}\n\nThis documentation page has not been published yet.\n\nCheck back in a future Catalyst release.";
        ShowContent(title, content, owner);
    }

    public static void ShowContent(string title, string markdown, Window owner)
    {
        var win = new DocViewerWindow { Owner = owner };
        win.WindowTitleText.Text = title;
        win.DocViewer.Document = BuildDocument(markdown);
        win.ShowDialog();
    }

    private static FlowDocument BuildDocument(string markdown)
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Bahnschrift, Segoe UI"),
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(0xC8, 0xDC, 0xE7)),
            Background = new SolidColorBrush(Color.FromRgb(0x0A, 0x1C, 0x28)),
            PagePadding = new Thickness(48, 28, 48, 48),
        };

        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        int i = 0;

        while (i < lines.Length)
        {
            string line = lines[i].TrimEnd();

            if (line.StartsWith("```"))
            {
                var sb = new System.Text.StringBuilder();
                i++;
                while (i < lines.Length && !lines[i].TrimEnd().StartsWith("```"))
                {
                    sb.AppendLine(lines[i]);
                    i++;
                }
                doc.Blocks.Add(new Paragraph(new Run(sb.ToString().TrimEnd('\r', '\n')))
                {
                    FontFamily = new FontFamily("Cascadia Mono, Consolas"),
                    FontSize = 12,
                    Background = new SolidColorBrush(Color.FromRgb(0x07, 0x16, 0x20)),
                    Foreground = new SolidColorBrush(Color.FromRgb(0x4F, 0xA4, 0x8D)),
                    Padding = new Thickness(16, 10, 16, 10),
                    Margin = new Thickness(0, 6, 0, 12),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x40, 0x55)),
                    BorderThickness = new Thickness(1),
                });
                i++;
                continue;
            }
            if (line.StartsWith("# "))
            {
                doc.Blocks.Add(new Paragraph(new Run(line[2..]))
                {
                    FontSize = 26,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xE4, 0xEF, 0xF5)),
                    Margin = new Thickness(0, 8, 0, 16),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x40, 0x55)),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Padding = new Thickness(0, 0, 0, 10),
                });
            }
            else if (line.StartsWith("## "))
            {
                doc.Blocks.Add(new Paragraph(new Run(line[3..]))
                {
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xDF, 0xA2, 0x4B)),
                    Margin = new Thickness(0, 22, 0, 6),
                });
            }
            else if (line.StartsWith("### "))
            {
                doc.Blocks.Add(new Paragraph(new Run(line[4..]))
                {
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xE4, 0xEF, 0xF5)),
                    Margin = new Thickness(0, 14, 0, 4),
                });
            }
            else if (line.StartsWith("- ") || line.StartsWith("* "))
            {
                var list = new List
                {
                    MarkerStyle = TextMarkerStyle.Square,
                    Margin = new Thickness(16, 4, 0, 8),
                    Padding = new Thickness(16, 0, 0, 0),
                };
                while (i < lines.Length && (lines[i].TrimEnd().StartsWith("- ") || lines[i].TrimEnd().StartsWith("* ")))
                {
                    string item = lines[i].TrimEnd()[2..];
                    var para = new Paragraph { Margin = new Thickness(0, 2, 0, 2) };
                    foreach (var inl in Inlines(item)) para.Inlines.Add(inl);
                    list.ListItems.Add(new ListItem(para));
                    i++;
                }
                doc.Blocks.Add(list);
                continue;
            }
            else if (line.Length > 2 && char.IsDigit(line[0]))
            {
                int dot = line.IndexOf('.');
                if (dot > 0 && int.TryParse(line[..dot], out _))
                {
                    var list = new List
                    {
                        MarkerStyle = TextMarkerStyle.Decimal,
                        Margin = new Thickness(16, 4, 0, 8),
                        Padding = new Thickness(16, 0, 0, 0),
                    };
                    while (i < lines.Length)
                    {
                        string nl = lines[i].TrimEnd();
                        int d = nl.IndexOf('.');
                        if (d < 0 || !int.TryParse(nl[..d], out _)) break;
                        string item = nl.Length > d + 2 ? nl[(d + 2)..] : "";
                        var para = new Paragraph { Margin = new Thickness(0, 2, 0, 2) };
                        foreach (var inl in Inlines(item)) para.Inlines.Add(inl);
                        list.ListItems.Add(new ListItem(para));
                        i++;
                    }
                    doc.Blocks.Add(list);
                    continue;
                }
            }
            else if (line.StartsWith("---") || line.StartsWith("==="))
            {
                doc.Blocks.Add(new Paragraph
                {
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x40, 0x55)),
                    BorderThickness = new Thickness(0, 1, 0, 0),
                    Margin = new Thickness(0, 12, 0, 12),
                });
            }
            else if (!string.IsNullOrWhiteSpace(line))
            {
                var para = new Paragraph { Margin = new Thickness(0, 3, 0, 3) };
                foreach (var inl in Inlines(line)) para.Inlines.Add(inl);
                doc.Blocks.Add(para);
            }

            i++;
        }

        return doc;
    }

    private static IEnumerable<Inline> Inlines(string text)
    {
        var result = new List<Inline>();
        int pos = 0;

        while (pos < text.Length)
        {
            char c = text[pos];
            if (c == '`')
            {
                int end = text.IndexOf('`', pos + 1);
                if (end > pos)
                {
                    result.Add(new Run(text[(pos + 1)..end])
                    {
                        FontFamily = new FontFamily("Cascadia Mono, Consolas"),
                        FontSize = 12,
                        Background = new SolidColorBrush(Color.FromRgb(0x07, 0x16, 0x20)),
                        Foreground = new SolidColorBrush(Color.FromRgb(0x4F, 0xA4, 0x8D)),
                    });
                    pos = end + 1;
                    continue;
                }
            }
            else if (c == '*' && pos + 1 < text.Length && text[pos + 1] == '*')
            {
                int end = text.IndexOf("**", pos + 2, StringComparison.Ordinal);
                if (end > pos)
                {
                    result.Add(new Run(text[(pos + 2)..end])
                    {
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(0xE4, 0xEF, 0xF5)),
                    });
                    pos = end + 2;
                    continue;
                }
            }
            else if (c == '[')
            {
                int closeBracket = text.IndexOf(']', pos + 1);
                if (closeBracket > pos && closeBracket + 1 < text.Length && text[closeBracket + 1] == '(')
                {
                    int closeParen = text.IndexOf(')', closeBracket + 2);
                    if (closeParen > closeBracket)
                    {
                        result.Add(new Run(text[(pos + 1)..closeBracket])
                        {
                            Foreground = new SolidColorBrush(Color.FromRgb(0x6F, 0xB6, 0xD8)),
                        });
                        pos = closeParen + 1;
                        continue;
                    }
                }
            }
            int next = text.Length;
            for (int j = pos + 1; j < text.Length; j++)
            {
                char ch = text[j];
                if (ch == '`' || ch == '*' || ch == '[') { next = j; break; }
            }
            result.Add(new Run(text[pos..next]));
            pos = next;
        }

        return result;
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
