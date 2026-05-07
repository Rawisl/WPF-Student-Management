using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace WPF_Student_Management.Helpers
{
    public class DataGridHelper
    {
        public static readonly DependencyProperty EnableFastEntryProperty =
            DependencyProperty.RegisterAttached("EnableFastEntry", typeof(bool), typeof(DataGridHelper), new PropertyMetadata(false, OnEnableFastEntryChanged));

        public static void SetEnableFastEntry(UIElement element, bool value) => element.SetValue(EnableFastEntryProperty, value);
        public static bool GetEnableFastEntry(UIElement element) => (bool)element.GetValue(EnableFastEntryProperty);

        private static void OnEnableFastEntryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid dataGrid)
            {
                if ((bool)e.NewValue)
                {
                    dataGrid.CurrentCellChanged += DataGrid_CurrentCellChanged;
                    dataGrid.PreviewKeyDown += DataGrid_PreviewKeyDown;
                }
                else
                {
                    dataGrid.CurrentCellChanged -= DataGrid_CurrentCellChanged;
                    dataGrid.PreviewKeyDown -= DataGrid_PreviewKeyDown;
                }
            }
        }

        private static void DataGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                // Đợi 1 nhịp siêu nhỏ để WPF chuyển ô xong, rồi ÉP BẬT EDIT MODE
                dataGrid.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (dataGrid.CurrentColumn != null && !dataGrid.CurrentColumn.IsReadOnly)
                    {
                        dataGrid.BeginEdit();
                    }
                }), DispatcherPriority.Background);
            }
        }

        private static void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                // 1. BẮT BÀI NÚT ENTER (Nhảy xuống dưới)
                if (e.Key == Key.Enter)
                {
                    e.Handled = true; // Chặn Enter mặc định
                    dataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                    dataGrid.CommitEdit(DataGridEditingUnit.Row, true);

                    // Bắn tín hiệu giả lập phím mũi tên đi xuống
                    var args = new KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0, Key.Down)
                    {
                        RoutedEvent = Keyboard.KeyDownEvent
                    };
                    dataGrid.RaiseEvent(args);
                    return;
                }

                // 2. BẮT BÀI 4 NÚT MŨI TÊN (Giải phóng con trỏ khỏi TextBox)
                if (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Right)
                {
                    // Ngay khi bấm mũi tên, ép lưu điểm và ĐÓNG TextBox lại
                    dataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                    dataGrid.CommitEdit(DataGridEditingUnit.Row, true);

                    // KHÔNG gán e.Handled = true ở đây!
                    // Lợi dụng cơ chế của WPF: Vì TextBox đã bị đóng, sự kiện phím mũi tên
                    // sẽ được trả về cho DataGrid. Thằng DataGrid sẽ tự động làm nhiệm vụ chuyển ô!
                }
            }
        }
    }
}