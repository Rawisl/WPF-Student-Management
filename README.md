## 📂 Cấu trúc dự án (MVVM)
- **Models**: Chứa các lớp thực thể dữ liệu (HocSinh, Lop,...) - Định dạng .cs (class thông thường)
- **ViewModels**: Xử lý logic nghiệp vụ và liên kết dữ liệu (Data Binding) - Định dạng .cs (class sử dụng thư viện CommunityToolkit)
- **Views**: Chứa các màn hình chính (...View) được load vào Content Area.
- **Components**: Chứa các UserControl dùng chung (...UC) và các Popup (...Dialog).
- **Services**: Các lớp kết nối Database SQL Server - Định dạng .cs (file xử lý trực tiếp DB)
- **Resources**: Chứa Style Material Design và các icon của ứng dụng - Định dạng .xaml (Resouce Dictionary), .png, .svg,.., các Converter (ví dụ: RoleToVisibilityConverter).
- **Helpers** Các Converter (ví dụ: RoleToVisibilityConverter), helpers dùng chung
