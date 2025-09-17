using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RevoltUltimate.Shared.Behaviors
{
    public static class PlaceholderBehavior
    {
        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.RegisterAttached(
                "PlaceholderText",
                typeof(string),
                typeof(PlaceholderBehavior),
                new PropertyMetadata(string.Empty, OnPlaceholderTextChanged));

        public static readonly DependencyProperty PlaceholderForegroundProperty =
            DependencyProperty.RegisterAttached(
                "PlaceholderForeground",
                typeof(Brush),
                typeof(PlaceholderBehavior),
                new PropertyMetadata(Brushes.Gray));

        public static string GetPlaceholderText(DependencyObject obj)
        {
            return (string)obj.GetValue(PlaceholderTextProperty);
        }

        public static void SetPlaceholderText(DependencyObject obj, string value)
        {
            obj.SetValue(PlaceholderTextProperty, value);
        }

        public static Brush GetPlaceholderForeground(DependencyObject obj)
        {
            return (Brush)obj.GetValue(PlaceholderForegroundProperty);
        }

        public static void SetPlaceholderForeground(DependencyObject obj, Brush value)
        {
            obj.SetValue(PlaceholderForegroundProperty, value);
        }

        private static void OnPlaceholderTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                textBox.GotFocus += (sender, args) =>
                {
                    if (textBox.Text == GetPlaceholderText(textBox))
                    {
                        textBox.Text = string.Empty;
                        textBox.Foreground = Brushes.White; // Restore normal foreground
                    }
                };

                textBox.LostFocus += (sender, args) =>
                {
                    if (string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        textBox.Text = GetPlaceholderText(textBox);
                        textBox.Foreground = GetPlaceholderForeground(textBox);
                    }
                };

                // Initialize placeholder text
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = GetPlaceholderText(textBox);
                    textBox.Foreground = GetPlaceholderForeground(textBox);
                }
            }
        }
    }
}
