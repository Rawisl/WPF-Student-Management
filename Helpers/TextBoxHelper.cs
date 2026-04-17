using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WPF_Student_Management.Helpers
{
    public class TextBoxHelper
    {
        private static readonly DependencyProperty LastValueProperty =
            DependencyProperty.RegisterAttached("LastValue", typeof(string), typeof(TextBoxHelper));

        // --- 1. THUỘC TÍNH CHỈ NHẬP SỐ NGUYÊN ---
        public static readonly DependencyProperty IsNumericOnlyProperty =
            DependencyProperty.RegisterAttached("IsNumericOnly", typeof(bool), typeof(TextBoxHelper), new PropertyMetadata(false, OnIsNumericOnlyChanged));

        public static void SetIsNumericOnly(UIElement element, bool value) => element.SetValue(IsNumericOnlyProperty, value);
        public static bool GetIsNumericOnly(UIElement element) => (bool)element.GetValue(IsNumericOnlyProperty);

        private static void OnIsNumericOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                if ((bool)e.NewValue) textBox.PreviewTextInput += BlockNonNumeric;
                else textBox.PreviewTextInput -= BlockNonNumeric;
            }
        }

        private static void BlockNonNumeric(object sender, TextCompositionEventArgs e)
        {
            e.Handled = new Regex("[^0-9]+").IsMatch(e.Text);
        }

        // --- 2. THUỘC TÍNH NHẬP SỐ THẬP PHÂN ---
        public static readonly DependencyProperty IsDecimalOnlyProperty =
            DependencyProperty.RegisterAttached("IsDecimalOnly", typeof(bool), typeof(TextBoxHelper), new PropertyMetadata(false, OnIsDecimalOnlyChanged));

        public static void SetIsDecimalOnly(UIElement element, bool value) => element.SetValue(IsDecimalOnlyProperty, value);
        public static bool GetIsDecimalOnly(UIElement element) => (bool)element.GetValue(IsDecimalOnlyProperty);

        private static void OnIsDecimalOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                if ((bool)e.NewValue) textBox.PreviewTextInput += BlockNonDecimal;
                else textBox.PreviewTextInput -= BlockNonDecimal;
            }
        }

        private static void BlockNonDecimal(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9.]+");
            if (regex.IsMatch(e.Text))
            {
                e.Handled = true;
                return;
            }

            if (sender is TextBox textBox && e.Text == ".")
            {
                if (textBox.Text.Contains("."))
                    e.Handled = true;
            }
        }

        // --- 3. LOGIC LƯU VÀ KHÔI PHỤC NẾU RỖNG ---

        private static void SaveOldValue(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Khi chuột click vào TextBox, lập tức copy giá trị hiện tại giấu vào LastValueProperty
                textBox.SetValue(LastValueProperty, textBox.Text);
            }
        }

        private static void RevertIfEmpty(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Khi chuột click ra ngoài, nếu thấy TextBox bị bỏ trống (hoặc toàn dấu cách)
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    NotificationHelper.ShowWarning("Ô nhập số không được để trống!\nHệ thống đã khôi phục lại giá trị cũ.");

                    // Lôi giá trị đã giấu ra
                    string oldValue = (string)textBox.GetValue(LastValueProperty);

                    // Khôi phục lại. Nếu trước đó nó vốn dĩ đã rỗng thì để mặc định là "0"
                    textBox.Text = string.IsNullOrEmpty(oldValue) ? "0" : oldValue;
                }
            }
        }
    }
}
