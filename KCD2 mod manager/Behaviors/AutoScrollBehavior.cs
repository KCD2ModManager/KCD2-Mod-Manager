using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace KCD2_mod_manager.Behaviors
{
    /// <summary>
    /// Attached Behavior für automatisches horizontales Scrollen in TextBoxen ohne sichtbare ScrollBars
    /// Verwendet für SearchBar: Scrollt automatisch, damit Cursor immer sichtbar bleibt
    /// Verhalten wie WinUI/VS SearchBox: Automatisches Scrollen beim Schreiben und bei Cursorbewegung
    /// </summary>
    public class AutoScrollBehavior
    {
        public static readonly DependencyProperty EnableAutoScrollProperty =
            DependencyProperty.RegisterAttached(
                "EnableAutoScroll",
                typeof(bool),
                typeof(AutoScrollBehavior),
                new PropertyMetadata(false, OnEnableAutoScrollChanged));

        public static bool GetEnableAutoScroll(DependencyObject obj)
        {
            return (bool)obj.GetValue(EnableAutoScrollProperty);
        }

        public static void SetEnableAutoScroll(DependencyObject obj, bool value)
        {
            obj.SetValue(EnableAutoScrollProperty, value);
        }

        private static void OnEnableAutoScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                if ((bool)e.NewValue)
                {
                    // Event-Handler hinzufügen
                    textBox.TextChanged += TextBox_TextChanged;
                    textBox.SelectionChanged += TextBox_SelectionChanged;
                    textBox.Loaded += TextBox_Loaded;
                    textBox.SizeChanged += TextBox_SizeChanged;
                }
                else
                {
                    // Event-Handler entfernen
                    textBox.TextChanged -= TextBox_TextChanged;
                    textBox.SelectionChanged -= TextBox_SelectionChanged;
                    textBox.Loaded -= TextBox_Loaded;
                    textBox.SizeChanged -= TextBox_SizeChanged;
                }
            }
        }

        private static void TextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Initial scrollen, falls Text bereits vorhanden
                ScrollToCaret(textBox);
            }
        }

        private static void TextBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Scrollen, wenn TextBox-Größe geändert wird
                ScrollToCaret(textBox);
            }
        }

        private static void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Scrollt automatisch zum Cursor, wenn Text geändert wird
                // Verwende Input-Priority für sofortiges Scrollen beim Tippen
                textBox.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => 
                {
                    // Zweite Verzögerung für sichereres Timing nach Layout-Update
                    textBox.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => ScrollToCaret(textBox)));
                }));
            }
        }

        private static void TextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Scrollt automatisch zum Cursor, wenn Cursor bewegt wird (Pfeiltasten, Mausklick)
                textBox.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => 
                {
                    // Zweite Verzögerung für sichereres Timing nach Layout-Update
                    textBox.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => ScrollToCaret(textBox)));
                }));
            }
        }

        /// <summary>
        /// Scrollt die TextBox horizontal, damit der Cursor immer sichtbar bleibt
        /// Verhalten wie WinUI/VS SearchBox: Automatisches Scrollen beim Schreiben und bei Cursorbewegung
        /// </summary>
        private static void ScrollToCaret(TextBox textBox)
        {
            try
            {
                // Finde den internen ScrollViewer
                var scrollViewer = GetScrollViewer(textBox);
                if (scrollViewer == null)
                {
                    return;
                }
                
                var text = textBox.Text ?? string.Empty;
                var caretIndex = textBox.CaretIndex;
                
                // Wenn kein Text vorhanden, scroll zum Anfang
                if (string.IsNullOrEmpty(text))
                {
                    scrollViewer.ScrollToHorizontalOffset(0);
                    return;
                }
                
                // Stelle sicher, dass ScrollViewer bereit ist
                if (scrollViewer.ViewportWidth <= 0)
                {
                    return;
                }
                
                // Berechne die Breite des Textes bis zum Cursor
                var textBeforeCaret = text.Substring(0, Math.Min(caretIndex, text.Length));
                
                if (!string.IsNullOrEmpty(textBeforeCaret))
                {
                    // Verwende FormattedText für präzise Breitenberechnung
                    var formattedText = new FormattedText(
                        textBeforeCaret,
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface(textBox.FontFamily, textBox.FontStyle, textBox.FontWeight, textBox.FontStretch),
                        textBox.FontSize,
                        Brushes.Black,
                        VisualTreeHelper.GetDpi(textBox).PixelsPerDip);
                    
                    var textWidth = formattedText.Width;
                    var currentOffset = scrollViewer.HorizontalOffset;
                    var viewportWidth = scrollViewer.ViewportWidth;
                    var margin = 20.0; // Margin für bessere Sichtbarkeit
                    
                    // Berechne die Position des Cursors relativ zum sichtbaren Bereich
                    // textWidth ist die absolute Position des Cursors vom Textanfang
                    // currentOffset ist die aktuelle Scroll-Position
                    // relativeX ist die Position des Cursors relativ zum sichtbaren Bereich (0 = links, viewportWidth = rechts)
                    var relativeX = textWidth - currentOffset;
                    
                    // Wenn Cursor links außerhalb des sichtbaren Bereichs ist
                    if (relativeX < margin)
                    {
                        // Scroll nach links, damit Cursor sichtbar wird
                        var newOffset = Math.Max(0, textWidth - margin);
                        scrollViewer.ScrollToHorizontalOffset(newOffset);
                    }
                    // Wenn Cursor rechts außerhalb des sichtbaren Bereichs ist
                    else if (relativeX > viewportWidth - margin)
                    {
                        // Scroll nach rechts, damit Cursor sichtbar wird
                        var newOffset = textWidth - viewportWidth + margin;
                        // Stelle sicher, dass wir nicht über das Maximum hinaus scrollen
                        var maxOffset = scrollViewer.ScrollableWidth;
                        if (maxOffset > 0)
                        {
                            newOffset = Math.Min(maxOffset, newOffset);
                        }
                        scrollViewer.ScrollToHorizontalOffset(newOffset);
                    }
                    // Wenn Cursor am Ende des Textes ist und noch Platz nach rechts ist, scroll zum Maximum
                    else if (caretIndex >= text.Length && scrollViewer.ScrollableWidth > 0)
                    {
                        var maxOffset = scrollViewer.ScrollableWidth;
                        if (currentOffset < maxOffset - 10) // Nur scrollen, wenn nicht bereits am Ende
                        {
                            scrollViewer.ScrollToHorizontalOffset(maxOffset);
                        }
                    }
                }
                else
                {
                    // Wenn kein Text vor dem Cursor, scroll zum Anfang
                    scrollViewer.ScrollToHorizontalOffset(0);
                }
            }
            catch
            {
                // Fehler beim Scrollen ignorieren (z. B. wenn TextBox noch nicht vollständig geladen ist)
            }
        }

        /// <summary>
        /// Findet den internen ScrollViewer einer TextBox
        /// WICHTIG: TextBox verwendet intern einen ScrollViewer namens "PART_ContentHost"
        /// </summary>
        private static ScrollViewer? GetScrollViewer(TextBox textBox)
        {
            // Versuche zuerst über Template.FindName (schneller und zuverlässiger)
            if (textBox.Template != null)
            {
                var scrollViewer = textBox.Template.FindName("PART_ContentHost", textBox) as ScrollViewer;
                if (scrollViewer != null)
                {
                    return scrollViewer;
                }
            }
            
            // Fallback: Versuche, den ScrollViewer über VisualTree zu finden
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(textBox); i++)
            {
                var child = VisualTreeHelper.GetChild(textBox, i);
                if (child is ScrollViewer scrollViewer)
                {
                    return scrollViewer;
                }
                
                // Rekursiv suchen
                var nestedScrollViewer = GetScrollViewerRecursive(child);
                if (nestedScrollViewer != null)
                {
                    return nestedScrollViewer;
                }
            }
            
            return null;
        }

        private static ScrollViewer? GetScrollViewerRecursive(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is ScrollViewer scrollViewer)
                {
                    return scrollViewer;
                }
                
                var nested = GetScrollViewerRecursive(child);
                if (nested != null)
                {
                    return nested;
                }
            }
            
            return null;
        }
    }
}
