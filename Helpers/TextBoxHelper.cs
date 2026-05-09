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

        // CHỈ NHẬP CHỮ ĐƯỢC

        public static readonly DependencyProperty IsLettersOnlyProperty =
    DependencyProperty.RegisterAttached("IsLettersOnly", typeof(bool), typeof(TextBoxHelper), new PropertyMetadata(false, OnIsLettersOnlyChanged));

        public static void SetIsLettersOnly(UIElement element, bool value) => element.SetValue(IsLettersOnlyProperty, value);
        public static bool GetIsLettersOnly(UIElement element) => (bool)element.GetValue(IsLettersOnlyProperty);

        private static void OnIsLettersOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                if ((bool)e.NewValue)
                {
                    textBox.PreviewTextInput += BlockSpecialCharactersAndNumber;
                    // Chặn cả việc dán (Paste) ký tự đặc biệt vào TextBox
                    DataObject.AddPastingHandler(textBox, OnPasteLetter);
                }
                else
                {
                    textBox.PreviewTextInput -= BlockSpecialCharactersAndNumber;
                    DataObject.RemovePastingHandler(textBox, OnPasteLetter);
                }
            }
        }

        private static void BlockSpecialCharactersAndNumber(object sender, TextCompositionEventArgs e)
        {
            // Regex này cho phép:
            // a-z, A-Z: Chữ cái Latin
            // \s: Khoảng trắng (Dùng cho tên lớp có dấu cách)
            // Các ký tự Tiếng Việt (Unicode): àáạảã...
            Regex regex = new Regex(@"[^a-zA-Z\sàáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴÈÉẸẺẼÊỀẾỆỂỄÌÍỊỈĨÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠÙÚỤỦŨƯỪỨỰỬỮỲÝỴỶỸĐ]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private static void OnPasteLetter(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                Regex regex = new Regex(@"[^a-zA-Z\sàáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴÈÉẸẺẼÊỀẾỆỂỄÌÍỊỈĨÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠÙÚỤỦŨƯỪỨỰỬỮỲÝỴỶỸĐ]+");
                if (regex.IsMatch(text))
                {
                    e.CancelCommand(); // Hủy lệnh Paste nếu chuỗi chứa ký tự đặc biệt
                }
            }
        }

        // ====================================================================
        // --- 5. THUỘC TÍNH RÀNG BUỘC TỰ ĐỘNG LÀM TRÒN ĐIỂM THEO CHUẨN BGD ---
        // ====================================================================
        public static readonly DependencyProperty IsBgdGradeOnlyProperty =
            DependencyProperty.RegisterAttached("IsBgdGradeOnly", typeof(bool), typeof(TextBoxHelper), new PropertyMetadata(false, OnIsBgdGradeOnlyChanged));

        public static void SetIsBgdGradeOnly(UIElement element, bool value) => element.SetValue(IsBgdGradeOnlyProperty, value);
        public static bool GetIsBgdGradeOnly(UIElement element) => (bool)element.GetValue(IsBgdGradeOnlyProperty);

        private static void OnIsBgdGradeOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                if ((bool)e.NewValue)
                {
                    textBox.PreviewTextInput += BlockNonDecimalGrade;
                    DataObject.AddPastingHandler(textBox, OnPasteGrade);
                    textBox.LostFocus += FormatBgdGrade; // Khi click ra chỗ khác thì tự làm tròn
                }
                else
                {
                    textBox.PreviewTextInput -= BlockNonDecimalGrade;
                    DataObject.RemovePastingHandler(textBox, OnPasteGrade);
                    textBox.LostFocus -= FormatBgdGrade;
                }
            }
        }

        private static void BlockNonDecimalGrade(object sender, TextCompositionEventArgs e)
        {
            // Cho phép nhập số, dấu chấm và dấu phẩy
            Regex regex = new Regex("[^0-9.,]+");
            if (regex.IsMatch(e.Text))
            {
                e.Handled = true;
                return;
            }

            // Chỉ cho phép tồn tại tối đa 1 dấu thập phân (chấm hoặc phẩy)
            if (sender is TextBox textBox && (e.Text == "." || e.Text == ","))
            {
                if (textBox.Text.Contains(".") || textBox.Text.Contains(","))
                    e.Handled = true;
            }
        }

        private static void OnPasteGrade(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                Regex regex = new Regex("[^0-9.,]+");
                if (regex.IsMatch(text))
                {
                    e.CancelCommand();
                }
            }
        }

        private static void FormatBgdGrade(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (string.IsNullOrWhiteSpace(textBox.Text)) return;

                // Chuẩn hóa dấu phẩy (VN) thành dấu chấm (Quốc tế) để Parse không bị lỗi
                string input = textBox.Text.Replace(',', '.');

                if (double.TryParse(input, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double score))
                {
                    // 1. Chặn khoảng điểm từ 0 -> 10
                    if (score < 0) score = 0;
                    if (score > 10) score = 10;

                    // 2. LÀM TRÒN CHUẨN BỘ GIÁO DỤC (AwayFromZero)
                    // (Ví dụ: 7.24 -> 7.2 | 7.25 -> 7.3)
                    score = Math.Round(score, 1, MidpointRounding.AwayFromZero);

                    // 3. Gán lại vào TextBox (Giữ hiển thị chuẩn quốc tế với dấu chấm)
                    textBox.Text = score.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                {
                    // Nếu nhập linh tinh không parse được -> tự xóa trắng
                    textBox.Text = "";
                }
            }
        }

        // ====================================================================
        // --- 6. TỰ ĐỘNG BÔI ĐEN TEXT KHI Ô ĐƯỢC CHỌN (CHUẨN EXCEL) ---
        // ====================================================================
        public static readonly DependencyProperty SelectAllOnFocusProperty =
            DependencyProperty.RegisterAttached("SelectAllOnFocus", typeof(bool), typeof(TextBoxHelper), new PropertyMetadata(false, OnSelectAllOnFocusChanged));

        public static void SetSelectAllOnFocus(UIElement element, bool value) => element.SetValue(SelectAllOnFocusProperty, value);
        public static bool GetSelectAllOnFocus(UIElement element) => (bool)element.GetValue(SelectAllOnFocusProperty);

        private static void OnSelectAllOnFocusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                if ((bool)e.NewValue)
                {
                    textBox.GotKeyboardFocus += TextBox_GotKeyboardFocus;
                }
                else
                {
                    textBox.GotKeyboardFocus -= TextBox_GotKeyboardFocus;
                }
            }
        }

        private static void TextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Phải dùng Dispatcher để đợi WPF render xong mới bôi đen được
                textBox.Dispatcher.BeginInvoke(new Action(() => textBox.SelectAll()));
            }
        }

    }
}
