using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_Student_Management.Views;

namespace WPF_Student_Management.Helpers
{
    /* =========================================================================
    * Component này dùng để thay thế MessageBox mặc định của Windows. 
    * 1. Báo Lỗi (Đỏ):         NotificationHelper.ShowError("Nội dung lỗi...");
    * 2. Thành Công (Xanh):    NotificationHelper.ShowSuccess("Lưu thành công...");
    * 3. Cảnh Báo (Cam):       NotificationHelper.ShowWarning("Cẩn thận nha...");
    * 4. Hộp thoại Xác nhận (Có nút OK / Hủy, trả về true/false):
    * bool isOK = NotificationHelper.ShowConfirm("Bạn có chắc chắn xóa?");
    * if (isOK) { /* Code xử lý khi bấm OK */
    /*========================================================================= */

    public static class NotificationHelper
    {
        public static void ShowError(string message)
        {
            var msgBox = new MaterialMessageBox("Lỗi", message, MsgType.Error);
            msgBox.ShowDialog();
        }

        public static void ShowSuccess(string message)
        {
            var msgBox = new MaterialMessageBox("Thành công", message, MsgType.Success);
            msgBox.ShowDialog();
        }

        public static void ShowWarning(string message)
        {
            var msgBox = new MaterialMessageBox("Cảnh báo", message, MsgType.Warning);
            msgBox.ShowDialog();
        }

        public static bool ShowConfirm(string message)
        {
            var msgBox = new MaterialMessageBox("Xác nhận", message, MsgType.Confirm);
            msgBox.ShowDialog();
            return msgBox.Result; // Trả về true nếu bấm OK, false nếu bấm Hủy
        }
    }
}
