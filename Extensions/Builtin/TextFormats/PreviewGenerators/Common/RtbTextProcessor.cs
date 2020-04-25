using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Clipbox.Extensions.BuiltIn.TextFormats.PreviewGenerators.Common
{
    public class RtbTextProcessor
    {
        private readonly char[] extraSeparatorChars = new char[]
                        { '\t', '\n', '\r' };

        private readonly string _enterSymbol;
        private readonly string _spaceSymbol;
        private readonly string _tabSymbol;

        public RtbTextProcessor()
        {
            _enterSymbol = GetUnicodeCharacter("21B5"); //U+2BA0
            _spaceSymbol = GetUnicodeCharacter("02FD");
            _tabSymbol = GetUnicodeCharacter("2B7E");
        }

        public Rect GetRectOfLastCharOfTheLongestLine(RichTextBox rtb)
        {
            //string text = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd).Text;
            //string[] lines = text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            //int number = 0;
            //int longestLine = lines.Select(l => new { l.Length, Number = number++}).OrderBy(l => l.Length).Last().Number;

            //rtb.CaretPosition.DocumentStart.Line
            //TextPointer tp = rtb.CaretPosition.DocumentStart.GetLineStartPosition(longestLine + 1).GetInsertionPosition(LogicalDirection.Backward);

            //Rect charRect = (tp ?? rtb.CaretPosition.DocumentEnd).GetCharacterRect(LogicalDirection.Forward);

            //return charRect;

            return new Rect
            {
                Width = 150,
                Height = 50
            };
        }

        public Rect GetRectOfFirstActualCharacter(RichTextBox rtb)
        {
            string text = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd).Text;

            int firstCharOffset = text.TakeWhile(c => char.IsSeparator(c) || extraSeparatorChars.Contains(c)).Count();
            TextPointer tp = rtb.CaretPosition.DocumentStart.GetPositionAtOffset(firstCharOffset);

            Rect charRect = tp.GetCharacterRect(LogicalDirection.Forward);

            return charRect;
        }

        private class TextRangeSequence
        {
            public string Symbol { get; }

            public List<TextRange> Ranges { get; set; }

            public bool Finalized { get; set; }

            public TextRangeSequence(string symbol)
            {
                Symbol = symbol;
                Ranges = new List<TextRange>();
            }
        }

        public enum SeparatorChars
        {
            Enter,
            Space,
            Tab
        }

        public void AdjustSpacing(RichTextBox rtb)
        {
            foreach (Paragraph p in rtb.Document.Blocks)
            {
                p.Margin = new Thickness(0);
                p.LineHeight = 2;
            }
        }

        public void TrimSeparatorCharSeries(RichTextBox rtb, SeparatorChars sepChar)
        {
            TextPointer charStart = rtb.Document.ContentStart;
            TextPointer charEnd = rtb.Document.ContentStart.GetNextInsertionPosition(LogicalDirection.Forward);
            TextRange tr;

            List<TextRangeSequence> sequences = new List<TextRangeSequence>();


            string lastText = string.Empty;
            while ((tr = new TextRange(charStart, charEnd)) != null)
            {
                if (tr.Text != string.Empty)
                {
                    string text = tr.Text;
                    var sequence = sequences.FirstOrDefault(s => s.Symbol == text && !s.Finalized);
                    if (sequence != null && text == lastText)
                    {
                        sequence.Ranges.Add(tr);
                    }
                    else
                    {
                        if (sequence != null)
                        {
                            sequence.Finalized = true;
                        }
                        var newSequence = new TextRangeSequence(text);
                        newSequence.Ranges.Add(tr);
                        sequences.Add(newSequence);
                    }
                    charStart = charEnd;

                    lastText = text;
                }


                charEnd = charEnd.GetNextInsertionPosition(LogicalDirection.Forward);
                if (charEnd == null)
                {
                    break;
                }
            }

            foreach (var sequence in sequences)
            {
                if (sepChar == SeparatorChars.Enter && sequence.Symbol == "\r\n" && sequence.Ranges.Count > 1)
                {
                    ShrinkSymbols(sequence.Symbol, $"[{_enterSymbol}+]{Environment.NewLine}", sequence);
                }

                else if (sepChar == SeparatorChars.Space && sequence.Symbol == " " && sequence.Ranges.Count > 3)
                {
                    ShrinkSymbols(sequence.Symbol, $"[{_spaceSymbol}+]", sequence);
                }

                else if (sepChar == SeparatorChars.Tab && sequence.Symbol == "\t" && sequence.Ranges.Count > 0)
                {
                    ShrinkSymbols(sequence.Symbol, $"[{_tabSymbol}+]", sequence);
                }
            }
        }


        private string GetUnicodeCharacter(string unicodeCode)
        {
            int enterValue = int.Parse(unicodeCode, System.Globalization.NumberStyles.HexNumber);
            return char.ConvertFromUtf32(enterValue).ToString();
        }

        private void ShrinkSymbols(string strToReplace, string replacementStr, TextRangeSequence sequence)
        {
            // So, in order to change the text in the richtextbox, 
            // it turns out that I have to create a new TextRange and not assign it anywhere to the textbox,
            // but just leave it "dangling" instead, and then set its
            // text property to whatever I want to be shown in the textbox...
            // It seems that it knows where to "post" because it has the start and end text pointers.
            // Thanks to https://stackoverflow.com/questions/13041350/automatic-replacement-of-text-in-wpf-richtextbox
            // How did I not think of it myself... :(
            TextPointer newRangeStart = sequence.Ranges.First().Start;
            TextPointer newRangeEnd = sequence.Ranges.Last().End;
            TextRange newRange = new TextRange(newRangeStart, newRangeEnd)
            {
                Text = replacementStr
            };
            newRange.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Brown);
            double textSize = (double)sequence.Ranges.First().GetPropertyValue(TextElement.FontSizeProperty);
            newRange.ApplyPropertyValue(TextElement.FontSizeProperty, textSize + 4);
        }
    }
}
