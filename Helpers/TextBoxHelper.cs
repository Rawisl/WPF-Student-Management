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

        // --- 4. THUỘC TÍNH CHỈ NHẬP CHỮ VÀ SỐ (ALPHA-NUMERIC) ---
        public static readonly DependencyProperty IsAlphaNumericOnlyProperty =
            DependencyProperty.RegisterAttached("IsAlphaNumericOnly", typeof(bool), typeof(TextBoxHelper), new PropertyMetadata(false, OnIsAlphaNumericOnlyChanged));

        public static void SetIsAlphaNumericOnly(UIElement element, bool value) => element.SetValue(IsAlphaNumericOnlyProperty, value);
        public static bool GetIsAlphaNumericOnly(UIElement element) => (bool)element.GetValue(IsAlphaNumericOnlyProperty);

        private static void OnIsAlphaNumericOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                if ((bool)e.NewValue)
                {
                    textBox.PreviewTextInput += BlockSpecialCharacters;
                    // Chặn cả việc dán (Paste) ký tự đặc biệt vào TextBox
                    DataObject.AddPastingHandler(textBox, OnPasteAlphaNumeric);
                }
                else
                {
                    textBox.PreviewTextInput -= BlockSpecialCharacters;
                    DataObject.RemovePastingHandler(textBox, OnPasteAlphaNumeric);
                }
            }
        }

        private static void BlockSpecialCharacters(object sender, TextCompositionEventArgs e)
        {
            // Regex này cho phép:
            // a-z, A-Z: Chữ cái Latin
            // 0-9: Số
            // \s: Khoảng trắng (Dùng cho tên lớp có dấu cách)
            // Các ký tự Tiếng Việt (Unicode): àáạảã...
            // Nếu bro KHÔNG muốn cho phép dấu cách, hãy xóa "\s" trong ngoặc []
            Regex regex = new Regex(@"[^a-zA-Z0-9\sàáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴÈÉẸẺẼÊỀẾỆỂỄÌÍỊỈĨÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠÙÚỤỦŨƯỪỨỰỬỮỲÝỴỶỸĐ]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private static void OnPasteAlphaNumeric(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                Regex regex = new Regex(@"[^a-zA-Z0-9\sàáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴÈÉẸẺẼÊỀẾỆỂỄÌÍỊỈĨÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠÙÚỤỦŨƯỪỨỰỬỮỲÝỴỶỸĐ]+");
                if (regex.IsMatch(text))
                {
                    e.CancelCommand(); // Hủy lệnh Paste nếu chuỗi chứa ký tự đặc biệt
                }
            }
        }
    }
}
