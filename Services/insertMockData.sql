-- 1. Insert Roles (Strictly following CHK_Role_RoleName)
INSERT INTO Role (RoleID, RoleName) VALUES
(1, N'Học sinh'), 
(2, N'IT Admin'), 
(3, N'Hiệu trưởng'),
(4, N'GVBM'), 
(5, N'GVCN'), 
(6, N'Giáo vụ');

-- 2. Insert Parameters (Strictly following the provided image)
INSERT INTO Parameter (ParameterID, ParameterName, Value) VALUES
(1, 'MinAge', 15),
(2, 'MaxAge', 20),
(3, 'MaxClassSize', 40),
(4, 'NumPassingGrade', 5),
(5, 'RegularScoreCoefficient', 1),
(6, 'MidtermScoreCoefficient', 2),
(7, 'FinalScoreCoefficient', 3);

-- 3. Insert Subjects (Strictly following CHK_Subject_SubjectName)
INSERT INTO Subject (SubjectID, SubjectName, GradeType, IsDeleted) VALUES
(1, N'Toán', 'Score', 0), 
(2, N'Lý', 'Score', 0), 
(3, N'Hóa', 'Score', 0),
(4, N'Sinh', 'Score', 0), 
(5, N'Sử', 'Score', 0), 
(6, N'Địa', 'Score', 0),
(7, N'Văn', 'Score', 0), 
(8, N'Đạo Đức', 'PassFail', 0), 
(9, N'Thể Dục', 'PassFail', 0);

-- 4. Insert Accounts (Updated Teacher Usernames & 40 Student Accounts)
INSERT INTO Account (AccountID, RoleID, Username, PasswordHash, IsRequiredChangePassword, IsActive) VALUES
-- Admin & Staff
(1, 2, 'admin_system', 'admin123', 1, 1),
(2, 3, 'ht_daott', 'principal123', 1, 1),
(3, 6, 'gv_vulv', 'staff123', 1, 1),
-- Homeroom Teachers / GVCN 
(4, 5, 'gv_canpv', 'teacher123', 1, 1),
(5, 5, 'gv_nguht', 'teacher123', 1, 1),
(6, 5, 'gv_khoavv', 'teacher123', 1, 1),
(7, 5, 'gv_hoadt', 'teacher123', 1, 1),
(8, 5, 'gv_sinhbv', 'teacher123', 1, 1),
(9, 5, 'gv_sudt', 'teacher123', 1, 1),
(10, 5, 'gv_diahv', 'teacher123', 1, 1),
(11, 5, 'gv_anhnt', 'teacher123', 1, 1),
(12, 5, 'gv_bachdv', 'teacher123', 1, 1),
-- Subject Teachers / GVBM 
(13, 4, 'gv_duclt', 'teacher123', 1, 1),
(14, 4, 'gv_lucmv', 'teacher123', 1, 1),
-- 40 Students (hs_[fullname][ddMMyy])
(15, 1, 'hs_nguyenvanan100110', 'student123', 1, 1),
(16, 1, 'hs_tranthibinh150210', 'student123', 1, 1),
(17, 1, 'hs_lehoangcuong200310', 'student123', 1, 1),
(18, 1, 'hs_phamducduy250410', 'student123', 1, 1),
(19, 1, 'hs_vothiyen300510', 'student123', 1, 1),
(20, 1, 'hs_danghaidang050610', 'student123', 1, 1),
(21, 1, 'hs_buingocgiau100710', 'student123', 1, 1),
(22, 1, 'hs_dominhhieu150810', 'student123', 1, 1),
(23, 1, 'hs_honhatkhang200910', 'student123', 1, 1),
(24, 1, 'hs_lygialinh251010', 'student123', 1, 1),
(25, 1, 'hs_maiphuongmai011110', 'student123', 1, 1),
(26, 1, 'hs_ngotiennam051210', 'student123', 1, 1),
(27, 1, 'hs_phanbaoNgoc120110', 'student123', 1, 1),
(28, 1, 'hs_hoangminhquan180210', 'student123', 1, 1),
(29, 1, 'hs_vuquocson220310', 'student123', 1, 1),
(30, 1, 'hs_trinhanhtu280410', 'student123', 1, 1),
(31, 1, 'hs_daothanhuy020510', 'student123', 1, 1),
(32, 1, 'hs_nguyenhaiha080610', 'student123', 1, 1),
(33, 1, 'hs_tranvanloc140710', 'student123', 1, 1),
(34, 1, 'hs_lethithu200810', 'student123', 1, 1),
(35, 1, 'hs_phamcongvinh100109', 'student123', 1, 1),
(36, 1, 'hs_vongoctram150209', 'student123', 1, 1),
(37, 1, 'hs_dangquanghuy220309', 'student123', 1, 1),
(38, 1, 'hs_buituananh280409', 'student123', 1, 1),
(39, 1, 'hs_dothanhthao050509', 'student123', 1, 1),
(40, 1, 'hs_hovanbinh120609', 'student123', 1, 1),
(41, 1, 'hs_lytuyetmai180709', 'student123', 1, 1),
(42, 1, 'hs_maixuankien250809', 'student123', 1, 1),
(43, 1, 'hs_ngothithuy010909', 'student123', 1, 1),
(44, 1, 'hs_phanvietdung101009', 'student123', 1, 1),
(45, 1, 'hs_hoangkimngan151109', 'student123', 1, 1),
(46, 1, 'hs_vuminhkhoi201209', 'student123', 1, 1),
(47, 1, 'hs_trinhgiahuy050109', 'student123', 1, 1),
(48, 1, 'hs_daothuhuong120209', 'student123', 1, 1),
(49, 1, 'hs_nguyentrongnhan180309', 'student123', 1, 1),
(50, 1, 'hs_tranthimong100108', 'student123', 1, 1),
(51, 1, 'hs_ledinhphuc150208', 'student123', 1, 1),
(52, 1, 'hs_phamthanhdat220308', 'student123', 1, 1),
(53, 1, 'hs_vothanhphong280408', 'student123', 1, 1),
(54, 1, 'hs_dangthiminh050508', 'student123', 1, 1);

-- 5. Insert Employees (Linking strictly to Accounts 1 through 14)
INSERT INTO Employee (EmployeeID, AccountID, FullName, Gender, Specialization, Email, HireDate, HometownAddress, PhoneNumber, NationalID, Status) VALUES
(1, 1, N'Nguyễn Văn Trị', N'Nam', N'Công nghệ thông tin', 'admin@gmail.mock', '2020-01-15', N'Hà Nội', '0901234560', '001090123450', 'Active'),
(2, 2, N'Trần Thị Đạo', N'Nữ', N'Quản lý giáo dục', 'hieutruong@gmail.mock', '2010-05-15', N'TP.HCM', '0901234561', '079190123451', 'Active'),
(3, 3, N'Lê Văn Vụ', N'Nam', N'Hành chính', 'giaovu@gmail.mock', '2015-08-20', N'Đà Nẵng', '0901234562', '048090123452', 'Active'),
(4, 4, N'Phạm Văn Cán', N'Nam', N'Toán học', 'gvcn10a1@gmail.mock', '2018-09-01', N'TP.HCM', '0901234563', '079090123453', 'Active'),
(5, 5, N'Hoàng Thị Ngữ', N'Nữ', N'Ngữ Văn', 'gvcn10a2@gmail.mock', '2019-09-01', N'Cần Thơ', '0901234564', '092190123454', 'Active'),
(6, 6, N'Vũ Văn Khoa', N'Nam', N'Vật Lý', 'gvcn10a3@gmail.mock', '2017-09-01', N'Hải Phòng', '0901234565', '031090123455', 'Active'),
(7, 7, N'Đặng Thị Hóa', N'Nữ', N'Hóa học', 'gvcn10a4@gmail.mock', '2020-09-01', N'Huế', '0901234566', '046190123456', 'Active'),
(8, 8, N'Bùi Văn Sinh', N'Nam', N'Sinh học', 'gvcn11a1@gmail.mock', '2021-09-01', N'Đồng Nai', '0901234567', '075090123457', 'Active'),
(9, 9, N'Đỗ Thị Sử', N'Nữ', N'Lịch sử', 'gvcn11a2@gmail.mock', '2016-09-01', N'Bình Dương', '0901234568', '074190123458', 'Active'),
(10, 10, N'Hồ Văn Địa', N'Nam', N'Địa lý', 'gvcn11a3@gmail.mock', '2015-09-01', N'Long An', '0901234569', '080090123459', 'Active'),
(11, 11, N'Ngô Thị Anh', N'Nữ', N'Ngoại ngữ', 'gvcn12a1@gmail.mock', '2018-09-01', N'Tiền Giang', '0901234570', '082190123460', 'Active'),
(12, 12, N'Dương Văn Bách', N'Nam', N'Toán học', 'gvcn12a2@gmail.mock', '2019-09-01', N'Bến Tre', '0901234571', '083090123461', 'Active'),
(13, 13, N'Lý Thị Đức', N'Nữ', N'Giáo dục công dân', 'gvbm_dao_duc@gmail.mock', '2022-09-01', N'Vũng Tàu', '0901234572', '077190123462', 'Active'),
(14, 14, N'Mai Văn Lực', N'Nam', N'Giáo dục thể chất', 'gvbm_the_duc@gmail.mock', '2021-09-01', N'Tây Ninh', '0901234573', '072090123463', 'Active');

-- 6. Insert Classes (Matches the image matrix exactly. Triggers will auto-update ClassSize upon ClassPlacement)
-- Note: HomeroomTeacherID explicitly links to the GVCN employees to satisfy TRG_Class_VerifyHomeroomTeacherRole.
INSERT INTO Class (ClassID, ClassName, Grade, ClassSize, HomeroomTeacherID) VALUES
(1, '10A1', 10, 0, 4),
(2, '10A2', 10, 0, 5),
(3, '10A3', 10, 0, 6),
(4, '10A4', 10, 0, 7),
(5, '11A1', 11, 0, 8),
(6, '11A2', 11, 0, 9),
(7, '11A3', 11, 0, 10),
(8, '12A1', 12, 0, 11),
(9, '12A2', 12, 0, 12);

-- 7. Insert Students (40 Entries Appended)
INSERT INTO Student (StudentID, AccountID, FullName, Gender, DateOfBirth, PhoneNumber, Email, Address, FamilyBackground, GuardianName, GuardianPhoneNumber, Status) VALUES
-- Grade 10 (DOB: 2010)
(1, 15, N'Nguyễn Văn An', N'Nam', '2010-01-10', '0912345101', 'nguyenvanan100110@gmail.mock', N'Quận 1, TP.HCM', N'Gia đình cơ bản', N'Nguyễn Văn Ánh', '0987654101', 'Active'),
(2, 16, N'Trần Thị Bình', N'Nữ', '2010-02-15', '0912345102', 'tranthibinh150210@gmail.mock', N'Quận 3, TP.HCM', N'Gia đình khá giả', N'Trần Văn Biên', '0987654102', 'Active'),
(3, 17, N'Lê Hoàng Cường', N'Nam', '2010-03-20', '0912345103', 'lehoangcuong200310@gmail.mock', N'Quận 4, TP.HCM', N'Hộ nghèo', N'Lê Thị Cúc', '0987654103', 'Active'),
(4, 18, N'Phạm Đức Duy', N'Nam', '2010-04-25', '0912345104', 'phamducduy250410@gmail.mock', N'Quận 5, TP.HCM', N'Gia đình cơ bản', N'Phạm Văn Dũng', '0987654104', 'Active'),
(5, 19, N'Võ Thị Yến', N'Nữ', '2010-05-30', '0912345105', 'vothiyen300510@gmail.mock', N'Quận 7, TP.HCM', N'Gia đình cơ bản', N'Võ Văn Yên', '0987654105', 'Active'),
(6, 20, N'Đặng Hải Đăng', N'Nam', '2010-06-05', '0912345106', 'danghaidang050610@gmail.mock', N'Quận 8, TP.HCM', N'Gia đình khá giả', N'Đặng Thị Đào', '0987654106', 'Active'),
(7, 21, N'Bùi Ngọc Giàu', N'Nữ', '2010-07-10', '0912345107', 'buingocgiau100710@gmail.mock', N'Quận 10, TP.HCM', N'Gia đình cơ bản', N'Bùi Văn Gấm', '0987654107', 'Active'),
(8, 22, N'Đỗ Minh Hiếu', N'Nam', '2010-08-15', '0912345108', 'dominhhieu150810@gmail.mock', N'Quận 11, TP.HCM', N'Gia đình cơ bản', N'Đỗ Thị Hiền', '0987654108', 'Active'),
(9, 23, N'Hồ Nhật Khang', N'Nam', '2010-09-20', '0912345109', 'honhatkhang200910@gmail.mock', N'Tân Bình, TP.HCM', N'Gia đình cơ bản', N'Hồ Văn Khánh', '0987654109', 'Active'),
(10, 24, N'Lý Gia Linh', N'Nữ', '2010-10-25', '0912345110', 'lygialinh251010@gmail.mock', N'Gò Vấp, TP.HCM', N'Hộ nghèo', N'Lý Thị Lan', '0987654110', 'Active'),
(11, 25, N'Mai Phương Mai', N'Nữ', '2010-11-01', '0912345111', 'maiphuongmai011110@gmail.mock', N'Tân Phú, TP.HCM', N'Gia đình khá giả', N'Mai Văn Mẫn', '0987654111', 'Active'),
(12, 26, N'Ngô Tiến Nam', N'Nam', '2010-12-05', '0912345112', 'ngotiennam051210@gmail.mock', N'Phú Nhuận, TP.HCM', N'Gia đình cơ bản', N'Ngô Thị Nga', '0987654112', 'Active'),
(13, 27, N'Phan Bảo Ngọc', N'Nữ', '2010-01-12', '0912345113', 'phanbaongoc120110@gmail.mock', N'Bình Thạnh, TP.HCM', N'Gia đình cơ bản', N'Phan Văn Nghĩa', '0987654113', 'Active'),
(14, 28, N'Hoàng Minh Quân', N'Nam', '2010-02-18', '0912345114', 'hoangminhquan180210@gmail.mock', N'Bình Tân, TP.HCM', N'Gia đình khá giả', N'Hoàng Thị Quyên', '0987654114', 'Active'),
(15, 29, N'Vũ Quốc Sơn', N'Nam', '2010-03-22', '0912345115', 'vuquocson220310@gmail.mock', N'Thủ Đức, TP.HCM', N'Gia đình cơ bản', N'Vũ Văn Sáng', '0987654115', 'Active'),
(16, 30, N'Trịnh Anh Tú', N'Nam', '2010-04-28', '0912345116', 'trinhanhtu280410@gmail.mock', N'Nhà Bè, TP.HCM', N'Hộ nghèo', N'Trịnh Thị Thủy', '0987654116', 'Active'),
(17, 31, N'Đào Thanh Uyển', N'Nữ', '2010-05-02', '0912345117', 'daothanhuyen020510@gmail.mock', N'Hóc Môn, TP.HCM', N'Gia đình cơ bản', N'Đào Văn Uy', '0987654117', 'Active'),
(18, 32, N'Nguyễn Hải Hà', N'Nữ', '2010-06-08', '0912345118', 'nguyenhaiha080610@gmail.mock', N'Bình Chánh, TP.HCM', N'Gia đình khá giả', N'Nguyễn Văn Hùng', '0987654118', 'Active'),
(19, 33, N'Trần Văn Lộc', N'Nam', '2010-07-14', '0912345119', 'tranvanloc140710@gmail.mock', N'Quận 1, TP.HCM', N'Gia đình cơ bản', N'Trần Thị Liên', '0987654119', 'Active'),
(20, 34, N'Lê Thị Thu', N'Nữ', '2010-08-20', '0912345120', 'lethithu200810@gmail.mock', N'Quận 3, TP.HCM', N'Gia đình cơ bản', N'Lê Văn Thắng', '0987654120', 'Active'),
-- Grade 11 (DOB: 2009)
(21, 35, N'Phạm Công Vinh', N'Nam', '2009-01-10', '0912345121', 'phamcongvinh100109@gmail.mock', N'Quận 4, TP.HCM', N'Gia đình cơ bản', N'Phạm Thị Vân', '0987654121', 'Active'),
(22, 36, N'Võ Ngọc Trâm', N'Nữ', '2009-02-15', '0912345122', 'vongoctram150209@gmail.mock', N'Quận 5, TP.HCM', N'Hộ nghèo', N'Võ Văn Triết', '0987654122', 'Active'),
(23, 37, N'Đặng Quang Huy', N'Nam', '2009-03-22', '0912345123', 'dangquanghuy220309@gmail.mock', N'Quận 7, TP.HCM', N'Gia đình khá giả', N'Đặng Thị Hoa', '0987654123', 'Active'),
(24, 38, N'Bùi Tuấn Anh', N'Nam', '2009-04-28', '0912345124', 'buituananh280409@gmail.mock', N'Quận 8, TP.HCM', N'Gia đình cơ bản', N'Bùi Văn Tú', '0987654124', 'Active'),
(25, 39, N'Đỗ Thanh Thảo', N'Nữ', '2009-05-05', '0912345125', 'dothanhthao050509@gmail.mock', N'Quận 10, TP.HCM', N'Gia đình cơ bản', N'Đỗ Thị Tâm', '0987654125', 'Active'),
(26, 40, N'Hồ Văn Bình', N'Nam', '2009-06-12', '0912345126', 'hovanbinh120609@gmail.mock', N'Quận 11, TP.HCM', N'Hộ nghèo', N'Hồ Thị Bích', '0987654126', 'Active'),
(27, 41, N'Lý Tuyết Mai', N'Nữ', '2009-07-18', '0912345127', 'lytuyetmai180709@gmail.mock', N'Tân Bình, TP.HCM', N'Gia đình khá giả', N'Lý Văn Mười', '0987654127', 'Active'),
(28, 42, N'Mai Xuân Kiên', N'Nam', '2009-08-25', '0912345128', 'maixuankien250809@gmail.mock', N'Gò Vấp, TP.HCM', N'Gia đình cơ bản', N'Mai Thị Kim', '0987654128', 'Active'),
(29, 43, N'Ngô Thị Thủy', N'Nữ', '2009-09-01', '0912345129', 'ngothithuy010909@gmail.mock', N'Tân Phú, TP.HCM', N'Gia đình cơ bản', N'Ngô Văn Thái', '0987654129', 'Active'),
(30, 44, N'Phan Việt Dũng', N'Nam', '2009-10-10', '0912345130', 'phanvietdung101009@gmail.mock', N'Phú Nhuận, TP.HCM', N'Gia đình khá giả', N'Phan Thị Diệp', '0987654130', 'Active'),
(31, 45, N'Hoàng Kim Ngân', N'Nữ', '2009-11-15', '0912345131', 'hoangkimngan151109@gmail.mock', N'Bình Thạnh, TP.HCM', N'Gia đình cơ bản', N'Hoàng Văn Ngữ', '0987654131', 'Active'),
(32, 46, N'Vũ Minh Khôi', N'Nam', '2009-12-20', '0912345132', 'vuminhkhoi201209@gmail.mock', N'Bình Tân, TP.HCM', N'Hộ nghèo', N'Vũ Thị Khuyên', '0987654132', 'Active'),
(33, 47, N'Trịnh Gia Huy', N'Nam', '2009-01-05', '0912345133', 'trinhgiahuy050109@gmail.mock', N'Thủ Đức, TP.HCM', N'Gia đình cơ bản', N'Trịnh Văn Hoàng', '0987654133', 'Active'),
(34, 48, N'Đào Thu Hương', N'Nữ', '2009-02-12', '0912345134', 'daothuhuong120209@gmail.mock', N'Nhà Bè, TP.HCM', N'Gia đình khá giả', N'Đào Thị Hạnh', '0987654134', 'Active'),
(35, 49, N'Nguyễn Trọng Nhân', N'Nam', '2009-03-18', '0912345135', 'nguyentrongnhan180309@gmail.mock', N'Hóc Môn, TP.HCM', N'Gia đình cơ bản', N'Nguyễn Văn Nghĩa', '0987654135', 'Active'),
-- Grade 12 (DOB: 2008)
(36, 50, N'Trần Thị Mộng', N'Nữ', '2008-01-10', '0912345136', 'tranthimong100108@gmail.mock', N'Bình Chánh, TP.HCM', N'Gia đình cơ bản', N'Trần Văn Mẫn', '0987654136', 'Active'),
(37, 51, N'Lê Đình Phúc', N'Nam', '2008-02-15', '0912345137', 'ledinhphuc150208@gmail.mock', N'Quận 1, TP.HCM', N'Gia đình khá giả', N'Lê Thị Phương', '0987654137', 'Active'),
(38, 52, N'Phạm Thành Đạt', N'Nam', '2008-03-22', '0912345138', 'phamthanhdat220308@gmail.mock', N'Quận 3, TP.HCM', N'Hộ nghèo', N'Phạm Văn Đồng', '0987654138', 'Active'),
(39, 53, N'Võ Thanh Phong', N'Nam', '2008-04-28', '0912345139', 'vothanhphong280408@gmail.mock', N'Quận 4, TP.HCM', N'Gia đình cơ bản', N'Võ Thị Phụng', '0987654139', 'Active'),
(40, 54, N'Đặng Thị Minh', N'Nữ', '2008-05-05', '0912345140', 'dangthiminh050508@gmail.mock', N'Quận 5, TP.HCM', N'Gia đình cơ bản', N'Đặng Văn Mạnh', '0987654140', 'Active');


-- 8. Insert ClassPlacements 
-- Distributing 40 students across the 9 classes (IDs 1 through 9)
INSERT INTO ClassPlacement (StudentID, ClassID) VALUES
-- Grade 10 Classes
(1, 1), (2, 1), (3, 1), (4, 1), (5, 1), -- 10A1 (ClassID 1)
(6, 2), (7, 2), (8, 2), (9, 2), (10, 2), -- 10A2 (ClassID 2)
(11, 3), (12, 3), (13, 3), (14, 3), (15, 3), -- 10A3 (ClassID 3)
(16, 4), (17, 4), (18, 4), (19, 4), (20, 4), -- 10A4 (ClassID 4)
-- Grade 11 Classes
(21, 5), (22, 5), (23, 5), (24, 5), (25, 5), -- 11A1 (ClassID 5)
(26, 6), (27, 6), (28, 6), (29, 6), (30, 6), -- 11A2 (ClassID 6)
(31, 7), (32, 7), (33, 7), (34, 7), (35, 7), -- 11A3 (ClassID 7)
-- Grade 12 Classes
(36, 8), (37, 8), (38, 8), -- 12A1 (ClassID 8)
(39, 9), (40, 9); -- 12A2 (ClassID 9)


-- 9. Insert TeachingAssignments
INSERT INTO TeachingAssignment (EmployeeID, ClassID, SubjectID) VALUES
(4, 1, 1),  -- GVCN 10A1 (Emp 4) teaches Toán to 10A1
(5, 2, 7),  -- GVCN 10A2 (Emp 5) teaches Văn to 10A2
(6, 3, 2),  -- GVCN 10A3 (Emp 6) teaches Lý to 10A3
(13, 1, 8), -- GVBM Đạo Đức (Emp 13) teaches 10A1
(14, 5, 9), -- GVBM Thể Dục (Emp 14) teaches 11A1
(8, 5, 4),  -- GVCN 11A1 (Emp 8) teaches Sinh to 11A1
(11, 8, 7), -- GVCN 12A1 (Emp 11) teaches Văn to 12A1
(12, 9, 1); -- GVCN 12A2 (Emp 12) teaches Toán to 12A2


-- 10. Insert Scores (Generating random plausible scores for various students)
INSERT INTO Score (ScoreID, StudentID, SubjectID, RegularTestScore, MidTermScore, FinalTermScore) VALUES
(1, 1, 1, 8.5, 9.0, 8.0),   -- Student 1, Toán
(2, 2, 1, 6.0, 7.5, 7.0),   -- Student 2, Toán
(3, 6, 7, 7.0, 8.0, 8.5),   -- Student 6, Văn
(4, 11, 2, 9.0, 9.5, 9.0),  -- Student 11, Lý
(5, 21, 4, 8.0, 7.0, 8.0),  -- Student 21, Sinh
(6, 22, 9, 10.0, 10.0, 10.0),-- Student 22, Thể Dục
(7, 36, 7, 7.5, 8.0, 8.5),  -- Student 36, Văn
(8, 39, 1, 9.5, 9.0, 9.5);  -- Student 39, Toán


-- 11. Insert Applications
INSERT INTO Application (RequestID, StudentID, CreatedByTeacherID, NewClassID, RequestType, Reason, FeedbackNote, Status, RespondedAt) VALUES
(1, 1, 4, 2, 'ClassTransfer', N'Chuyển sang lớp 10A2 để học cùng anh em họ', NULL, 'Pending', NULL),
(2, 15, 6, NULL, 'DropOut', N'Gia đình chuyển công tác ra nước ngoài', N'Đã xác nhận với phụ huynh', 'Executed', '2024-04-20 09:30:00'),
(3, 25, 8, 6, 'ClassTransfer', N'Không theo kịp chương trình nâng cao', N'Chuyển sang lớp 11A2', 'Executed', '2024-05-15 14:00:00'),
(4, 40, 12, NULL, 'DropOut', N'Lý do sức khỏe', NULL, 'Pending', NULL);