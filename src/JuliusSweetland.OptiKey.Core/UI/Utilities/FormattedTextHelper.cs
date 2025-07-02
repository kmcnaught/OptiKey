using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace JuliusSweetland.OptiKey.UI.Utilities
{
    /// <summary>
    /// Attached property helper for TextBlocks that allows parsing of markup text
    /// with {bold} and {highlight} tags and converts them to formatted Inline elements.
    /// </summary>
    public static class FormattedTextHelper
    {
        public static readonly DependencyProperty FormattedTextProperty =
            DependencyProperty.RegisterAttached(
                "FormattedText",
                typeof(string),
                typeof(FormattedTextHelper),
                new PropertyMetadata(null, OnFormattedTextChanged));

        /// <summary>
        /// Gets the formatted text for the specified TextBlock.
        /// </summary>
        /// <param name="obj">The TextBlock to get the formatted text from.</param>
        /// <returns>The formatted text string.</returns>
        public static string GetFormattedText(DependencyObject obj)
        {
            return (string)obj.GetValue(FormattedTextProperty);
        }

        /// <summary>
        /// Sets the formatted text for the specified TextBlock.
        /// </summary>
        /// <param name="obj">The TextBlock to set the formatted text on.</param>
        /// <param name="value">The formatted text string with markup.</param>
        public static void SetFormattedText(DependencyObject obj, string value)
        {
            obj.SetValue(FormattedTextProperty, value);
        }

        private static void OnFormattedTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBlock textBlock && e.NewValue is string formattedText)
            {
                textBlock.Inlines.Clear();
                
                if (string.IsNullOrEmpty(formattedText))
                    return;

                ParseAndAddInlines(textBlock, formattedText);
            }
        }

        private static void ParseAndAddInlines(TextBlock textBlock, string text)
        {
            // Regular expression to match {bold}...{/bold} and {highlight}...{/highlight} patterns
            var regex = new Regex(@"\{(bold|highlight)\}(.*?)\{/\1\}", RegexOptions.Singleline);
            var lastIndex = 0;

            foreach (Match match in regex.Matches(text))
            {
                // Add any text before the current match as a normal run
                if (match.Index > lastIndex)
                {
                    var beforeText = text.Substring(lastIndex, match.Index - lastIndex);
                    if (!string.IsNullOrEmpty(beforeText))
                    {
                        textBlock.Inlines.Add(new Run(beforeText));
                    }
                }

                // Create the formatted run based on the tag type
                var tagType = match.Groups[1].Value;
                var content = match.Groups[2].Value;
                var run = new Run(content);

                switch (tagType)
                {
                    case "bold":
                        run.FontWeight = FontWeights.Bold;
                        break;
                    case "highlight":
                        run.Foreground = new SolidColorBrush(Colors.Green);
                        break;
                }

                textBlock.Inlines.Add(run);
                lastIndex = match.Index + match.Length;
            }

            // Add any remaining text after the last match
            if (lastIndex < text.Length)
            {
                var remainingText = text.Substring(lastIndex);
                if (!string.IsNullOrEmpty(remainingText))
                {
                    textBlock.Inlines.Add(new Run(remainingText));
                }
            }

            // If no matches were found, add the entire text as a single run
            if (lastIndex == 0)
            {
                textBlock.Inlines.Add(new Run(text));
            }
        }
    }
}