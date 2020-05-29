using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Clipbox.Extensions.BuiltIn.TextFormats.PreviewGenerators.Common
{
    public class RtfProcessor
    {
        private readonly Dictionary<SpacingChars, string> _replacementChars;

        private int replacementCharColorRtfNumber = -1;
        private int _replacementCharFontSize = 0;


        private string _originalRtf;
        private string _resultRtf;

        private readonly string _replacementCharsRtfColorStr;
        
        public RtfProcessor(Color replacementCharsColor, int replacementCharsFontSizeHalfPoints)
        {
            _replacementChars = GetVisibleSpacingReplacementChars();

            // create an rtf color stirng
            _replacementCharsRtfColorStr = $"\\red{replacementCharsColor.R}\\green{replacementCharsColor.G}\\blue{replacementCharsColor.B}";

            _replacementCharFontSize = replacementCharsFontSizeHalfPoints;
        }

        public void ScaleFontSize(double scaleFactor)
        {
            _resultRtf = new Regex(@"(\\fs)(\d+)(\s*)", RegexOptions.IgnoreCase | RegexOptions.Multiline).Replace((_resultRtf ?? _originalRtf),
                m => m.Groups[1].Value + Math.Round(int.Parse(m.Groups[2].Value) * scaleFactor).ToString() + m.Groups[3].Value);

            // TODO: do I need the second regex?
            _resultRtf = new Regex(@"(\\b)(\d+)(\s*)", RegexOptions.IgnoreCase | RegexOptions.Multiline)
                .Replace((_resultRtf), m => m.Groups[1].Value + Math.Round(int.Parse(m.Groups[2].Value) * scaleFactor).ToString() + m.Groups[3].Value);
        }


        public void LoadFromRtf(string rtf)
        {
            _originalRtf = rtf;
            AdjustRtfColorTable();
        }

        public bool TryLoadFromText(string text, int fontSizeInHalfPoints)
        {
            // since languages like Chinese require the use of specific fonts,
            // it will be much easier and safer if we delegate the creation of
            // the Rtf to the RichTextBox and process it here afterwords
            if (ContainsUnicodeCharacter(text))
                return false;

            text = text
                .Replace("\\",@"\\")
                .Replace("\t", @"\tab ").Replace("\r\n", "\\par\r\n")
                .Replace("{", @"\{")
                .Replace("}", @"\}");

            StringBuilder newRtfSb = new StringBuilder();
            newRtfSb.AppendLine(@"{\rtf1\ansi\ansicpg1252\deff0\nouicompat\deflang2057{\fonttbl{\f0\fnil\fcharset0 Calibri;}}");
            newRtfSb.AppendLine($@"{{\colortbl ;{_replacementCharsRtfColorStr};}}");
            newRtfSb.AppendLine(@"{\*\generator Clipbox X.X}\viewkind4\uc1 ");
            newRtfSb.Append($@"\pard\sa200\sl276\slmult1\f0\fs{fontSizeInHalfPoints}\lang9 ");
            newRtfSb.Append(text);
            newRtfSb.AppendLine(@"\par");
            newRtfSb.Append("}");

            replacementCharColorRtfNumber = 1; // the only color that we added to the color table above
            _originalRtf = newRtfSb.ToString();

            return true;
        }

        public bool ContainsUnicodeCharacter(string input)
        {
            const int MaxAnsiCode = 255;

            return input.Any(c => c > MaxAnsiCode);
        }

        public string GetProcessedRtf()
        {
            if (_resultRtf == null)
            {
                throw new InvalidOperationException("No processing has been done yet.");
            }

            return _resultRtf;
        }

        private Dictionary<SpacingChars, string> GetVisibleSpacingReplacementChars()
        {
            return new Dictionary<SpacingChars, string>
            {
                { SpacingChars.LineBreak, char.ConvertFromUtf32(0x21B5) },
                { SpacingChars.Tab, char.ConvertFromUtf32(0x2B7E) },
                { SpacingChars.Space, char.ConvertFromUtf32(0x02FD) }
            };
        }

        public void ReplaceSpacingChar(SpacingChars spacingChar)
        {
            if (replacementCharColorRtfNumber == -1)
            {
                throw new InvalidOperationException("Spacing char color has not been set.");
            }

            string regex = "";
            switch (spacingChar)
            {
                case SpacingChars.LineBreak:
                    regex = @"((\\par|\\line)[\r|\n|\s]+){2,}";
                    break;
                case SpacingChars.Tab:
                    regex = @"(\\tab\s*){2,}";
                    break;
                case SpacingChars.Space:
                    regex = @"(\s){5,}";
                    break;
            }

            Regex rg = new Regex(regex, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            _resultRtf = rg.Replace((_resultRtf ?? _originalRtf),
                m => $@"{{\cf{replacementCharColorRtfNumber}\fs{_replacementCharFontSize} {Escape(_replacementChars[spacingChar])}+}}");

        }

        public void SetSpacingCharFontSize(int fontSizeInHalfPoints)
        {
            _replacementCharFontSize = fontSizeInHalfPoints;
        }
        
        private void AdjustRtfColorTable()
        {
            // find the color table
            Match m = new Regex(@"{\\colortbl\s+(.+?)(?=})").Match(_originalRtf);
            if (m.Success && m.Groups.Count == 2)
            {
                int position = 0;
                foreach (string colorStr in m.Groups[1].Value.Trim('{', '}').Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (colorStr.Trim().Equals(_replacementCharsRtfColorStr))
                    {
                        replacementCharColorRtfNumber = position;
                        return;
                    }
                    position++;
                }

                // find the last color in the table
                int insertIndex = 0;
                for (int i = m.Index + m.Length; i > m.Index; i--)
                {
                    if (_originalRtf[i] == ' ')
                    {
                        continue;
                    }

                    if (_originalRtf[i] == ';')
                    {
                        insertIndex = i + 1;
                        break;
                    }

                    if (char.IsDigit(_originalRtf[i]))
                    {
                        insertIndex = i + 2;
                        _resultRtf = _originalRtf.Insert(i + 1, ";");
                        break;
                    }
                }

                if (insertIndex == -1)
                {
                    throw new FormatException("The color table seems to be malformatted.");
                }

                _resultRtf = (_resultRtf ?? _originalRtf).Insert(insertIndex, _replacementCharsRtfColorStr + ";");

                replacementCharColorRtfNumber = position + 1;
            }
            else
            {
                string[] headerPartsRegex = new string[]
                {
                    @"{\\filetbl(\s|\W)+",
                    @"{\\fonttbl(\s|\W)+",
                    @"\\deflang\d+"
                };
                // TODO: if a color table is not present (it is entirely possible), add one
                int endIndex = -1;

                foreach (string regex in headerPartsRegex)
                {

                    Match ftm = new Regex(regex).Match(_originalRtf);
                    if (ftm.Success)
                    {
                        if (regex == headerPartsRegex[headerPartsRegex.Length - 1])
                        {
                            endIndex = ftm.Index + ftm.Length;
                        }
                        else
                        {
                            endIndex = GetBlockEndIndex(ftm.Index);
                        }
                        break;
                    }
                }

                if (endIndex == -1)
                {
                    throw new FormatException("Could not find a suitable place to insert the newly created color table.");
                }

                _resultRtf = (_resultRtf ?? _originalRtf).Insert(endIndex, $@"{{\colortbl;{_replacementCharsRtfColorStr};}}");
                replacementCharColorRtfNumber = 1;
            }
        }

        public int GetBlockEndIndex(int startIndex)
        {
            if (_originalRtf[startIndex] != '{')
            {
                throw new ArgumentException($"{nameof(startIndex)} is at the block start.");
            }

            int numberOfOpeningBrackets = 0;
            for (int i = startIndex; i < _originalRtf.Length; i++)
            {
                char c = _originalRtf[i];

                if (c == '{')
                {
                    numberOfOpeningBrackets++;
                }
                else if (c == '}')
                {
                    numberOfOpeningBrackets--;
                }

                if (numberOfOpeningBrackets == 0)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// This is a modified version of Yongtao Wang's answer in this thread:
        /// https://stackoverflow.com/questions/36766994/escape-plain-text-of-different-charsets-to-rtf-formatting
        /// Modifications: Some special characters are ommitted, others are replaced by the appropriate Rtf keyword.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private string Escape(string s)
        {
            if (s == null) return s;

            var sb = new StringBuilder();
            foreach (char c in s)
            {
                if (c == 0x9)
                {
                    sb.Append(@"\tab ");
                }
                else if (c == 0x10) // ignore \r, take just \n, since even Macs nowadays do not use the carriage returns
                {
                    sb.Append(@"\line ");
                }
                else if (c >= 0x20 && c < 0x80)
                {
                    if (c == '\\' || c == '{' || c == '}')
                    {
                        sb.Append('\\');
                    }
                    sb.Append(c);
                }
                else if (/*c < 0x20 || */ (c >= 0x80 && c <= 0xFF))
                {
                    sb.Append($"\\'{((byte)c).ToString("X")}");
                }
                //else
                //{
                //    sb.Append($"\\u{(short)c}?");
                //}
                else if (c > 0xFF)
                {
                    sb.Append($"\\u{(short)c}?");
                }
            }
            return sb.ToString();
        }

        public enum SpacingChars
        {
            LineBreak,
            Tab,
            Space
        }
    }
}
