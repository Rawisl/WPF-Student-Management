using System.Windows;
using System.Windows.Controls;

namespace WPF_Student_Management.Helpers
{
    public static class PasswordHelper
    {
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.RegisterAttached("Password", typeof(string), typeof(PasswordHelper),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPasswordPropertyChanged));

        public static readonly DependencyProperty AttachProperty =
            DependencyProperty.RegisterAttached("Attach", typeof(bool), typeof(PasswordHelper),
                new PropertyMetadata(false, Attach));

        public static void SetAttach(DependencyObject dp, bool value) => dp.SetValue(AttachProperty, value);
        public static bool GetAttach(DependencyObject dp) => (bool)dp.GetValue(AttachProperty);

        public static string GetPassword(DependencyObject dp) => (string)dp.GetValue(PasswordProperty);
        public static void SetPassword(DependencyObject dp, string value) => dp.SetValue(PasswordProperty, value);

        private static bool _isUpdating;

        private static void Attach(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                if ((bool)e.OldValue) passwordBox.PasswordChanged -= PasswordChanged;
                if ((bool)e.NewValue) passwordBox.PasswordChanged += PasswordChanged;
            }
        }

        private static void PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                _isUpdating = true;
                SetPassword(passwordBox, passwordBox.Password);
                _isUpdating = false;
            }
        }

        private static void OnPasswordPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is PasswordBox passwordBox && !_isUpdating)
            {
                passwordBox.PasswordChanged -= PasswordChanged;
                passwordBox.Password = (string)e.NewValue ?? string.Empty;
                passwordBox.PasswordChanged += PasswordChanged;
            }
        }
    }
}