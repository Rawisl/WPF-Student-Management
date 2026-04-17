using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_Student_Management.Views;

namespace WPF_Student_Management.Helpers
{
    /* =========================================================================
 * HƯỚNG DẪN SỬ DỤNG DÀNH CHO TEAM:
 * Component này dùng để thay thế MessageBox mặc định của Windows. 
 * Giao diện đã được style chuẩn Material Design. Anh em gọi như sau nhé:
 * * 1. Báo Lỗi (Đỏ):         NotificationHelper.ShowError("Nội dung lỗi...");
 * 2. Thành Công (Xanh):    NotificationHelper.ShowSuccess("Lưu thành công...");
 * 3. Cảnh Báo (Cam):       NotificationHelper.ShowWarning("Cẩn thận nha...");
 * * 4. Hộp thoại Xác nhận (Có nút OK / Hủy, trả về true/false):
 * bool isChonOK = NotificationHelper.ShowConfirm("Bạn có chắc chắn xóa?");
 * if (isChonOK) { /* Code xử lý khi bấm OK */
    /*========================================================================= */

    public static class NotificationHelper
    {
        // 1. Thông báo LỖI (Màu đỏ)
        public static void ShowError(string message)
        {
            var msgBox = new MaterialMessageBox("Lỗi", message, MsgType.Error);
            msgBox.ShowDialog();
        }

        // 2. Thông báo THÀNH CÔNG (Màu xanh lá)
        public static void ShowSuccess(string message)
        {
            var msgBox = new MaterialMessageBox("Thành công", message, MsgType.Success);
            msgBox.ShowDialog();
        }

        // 3. Thông báo CẢNH BÁO (Màu cam)
        public static void ShowWarning(string message)
        {
            var msgBox = new MaterialMessageBox("Cảnh báo", message, MsgType.Warning);
            msgBox.ShowDialog();
        }

        // 4. Hộp thoại XÁC NHẬN (Có nút Yes/No - Trả về true/false)
        public static bool ShowConfirm(string message)
        {
            var msgBox = new MaterialMessageBox("Xác nhận", message, MsgType.Confirm);
            msgBox.ShowDialog();
            return msgBox.Result; // Trả về true nếu bấm OK, false nếu bấm Hủy
        }
    }
}
