-- Cleanup: Drop tables in reverse order of dependencies
DROP TABLE IF EXISTS Application;
DROP TABLE IF EXISTS Score;
DROP TABLE IF EXISTS TeachingAssignment;
DROP TABLE IF EXISTS ClassPlacement;
DROP TABLE IF EXISTS Class;
DROP TABLE IF EXISTS Student;
DROP TABLE IF EXISTS Employee;
DROP TABLE IF EXISTS Account;
DROP TABLE IF EXISTS Subject;
DROP TABLE IF EXISTS Role;
DROP TABLE IF EXISTS Parameter;
DROP SEQUENCE IF EXISTS Seq_Student_ID;
GO

-- Cleanup: Drop tables in reverse order of dependencies
-- 1. Parameters & Roles
CREATE TABLE Parameter (
    ParameterID INT IDENTITY(1,1) PRIMARY KEY,
    ParameterName VARCHAR(100) NOT NULL,
    Value DECIMAL(10,2) NOT NULL
);

CREATE TABLE Role (
    RoleID INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(100) NOT NULL,
    CONSTRAINT CHK_Role_RoleName CHECK (RoleName IN (N'Học sinh', N'IT Admin', N'Hiệu trưởng', N'GVBM', N'GVCN', N'Giáo vụ'))
);

-- 2. Account & Employee
CREATE TABLE Account (
    AccountID INT IDENTITY(1,1) PRIMARY KEY,
    RoleID INT NOT NULL,
    Username VARCHAR(100) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    IsRequiredChangePassword BIT DEFAULT 1,
    IsActive BIT DEFAULT 1,
    CONSTRAINT FK_Account_Role FOREIGN KEY (RoleID) REFERENCES Role(RoleID)
);

CREATE TABLE Employee (
    EmployeeID INT IDENTITY(1,1) PRIMARY KEY,
    AccountID INT UNIQUE NOT NULL,
    FullName NVARCHAR(255) NOT NULL,
    Gender NVARCHAR(50),
    Specialization NVARCHAR(255),
    Email VARCHAR(255) UNIQUE,
    HireDate DATE,
    HometownAddress NVARCHAR(255),
    PhoneNumber VARCHAR(20),
    NationalID VARCHAR(50) UNIQUE,
    Status NVARCHAR(50),
    CONSTRAINT FK_Employee_Account FOREIGN KEY (AccountID) REFERENCES Account(AccountID)
);

-- 3. Student (Special String ID Logic)
CREATE SEQUENCE Seq_Student_ID AS INT START WITH 1 INCREMENT BY 1;
GO

CREATE TABLE Student (
    StudentID VARCHAR(10) PRIMARY KEY DEFAULT ('hs25' + RIGHT('0000' + CAST(NEXT VALUE FOR Seq_Student_ID AS VARCHAR(10)), 4)),
    AccountID INT UNIQUE NOT NULL,
    FullName NVARCHAR(255) NOT NULL,
    Gender NVARCHAR(50),
    DateOfBirth DATE,
    PhoneNumber VARCHAR(20),
    Email VARCHAR(255) UNIQUE,
    Address NVARCHAR(255),
    FamilyBackground NVARCHAR(MAX) CONSTRAINT CHK_Student_FamilyBackground CHECK (FamilyBackground IN (N'Bình thường',N'Khó khăn')),
    GuardianName NVARCHAR(255),
    GuardianPhoneNumber VARCHAR(20),
    Status NVARCHAR(50),
    CONSTRAINT FK_Student_Account FOREIGN KEY (AccountID) REFERENCES Account(AccountID)
);

-- 4. Academic Structure
CREATE TABLE Subject (
    SubjectID INT IDENTITY(1,1) PRIMARY KEY,
    SubjectName NVARCHAR(100) NOT NULL,
    GradeType VARCHAR(50) NOT NULL, 
    IsDeleted BIT DEFAULT 0,
);

CREATE TABLE Class (
    ClassID INT IDENTITY(1,1) PRIMARY KEY,
    ClassName NVARCHAR(50) NOT NULL,
    Grade INT NOT NULL,
    ClassSize INT DEFAULT 0,
    HomeroomTeacherID INT,
    IsLocked BIT DEFAULT 0,
    CONSTRAINT CHK_Class_Grade CHECK (Grade IN (10, 11, 12)),
    CONSTRAINT FK_Class_Employee FOREIGN KEY (HomeroomTeacherID) REFERENCES Employee(EmployeeID)
);

-- 5. Links & Metrics
CREATE TABLE ClassPlacement (
    StudentID VARCHAR(10),
    ClassID INT,
    CONSTRAINT PK_ClassPlacement PRIMARY KEY (StudentID, ClassID),
    CONSTRAINT FK_ClassPlacement_Student FOREIGN KEY (StudentID) REFERENCES Student(StudentID),
    CONSTRAINT FK_ClassPlacement_Class FOREIGN KEY (ClassID) REFERENCES Class(ClassID)
);

CREATE TABLE TeachingAssignment (
    EmployeeID INT,
    ClassID INT,
    SubjectID INT,
    CONSTRAINT PK_TeachingAssignment PRIMARY KEY (EmployeeID, ClassID, SubjectID),
    CONSTRAINT FK_TeachingAssignment_Employee FOREIGN KEY (EmployeeID) REFERENCES Employee(EmployeeID),
    CONSTRAINT FK_TeachingAssignment_Class FOREIGN KEY (ClassID) REFERENCES Class(ClassID),
    CONSTRAINT FK_TeachingAssignment_Subject FOREIGN KEY (SubjectID) REFERENCES Subject(SubjectID)
);

CREATE TABLE Score (
    ScoreID INT IDENTITY(1,1) PRIMARY KEY,
    StudentID VARCHAR(10) NOT NULL,
    SubjectID INT NOT NULL,
    RegularTestScore DECIMAL(5,2),
    MidTermScore DECIMAL(5,2),
    FinalTermScore DECIMAL(5,2),
    AverageScore DECIMAL(5,2),
    CONSTRAINT FK_Score_Student FOREIGN KEY (StudentID) REFERENCES Student(StudentID),
    CONSTRAINT FK_Score_Subject FOREIGN KEY (SubjectID) REFERENCES Subject(SubjectID)
);

CREATE TABLE Application (
    RequestID INT IDENTITY(1,1) PRIMARY KEY,
    StudentID VARCHAR(10) NOT NULL,
    CreatedByTeacherID INT,
    NewClassID INT,
    RequestType VARCHAR(50) NOT NULL, 
    Reason NVARCHAR(MAX),
    FeedbackNote NVARCHAR(MAX),
    Status VARCHAR(50) DEFAULT 'Pending', 
    RespondedAt DATETIME, 
    CONSTRAINT CHK_Application_RequestType CHECK (RequestType IN ('ClassTransfer', 'DropOut')),
    CONSTRAINT CHK_Application_Status CHECK (Status IN ('Pending', 'Executed', 'Rejected')),
    CONSTRAINT FK_Application_Student FOREIGN KEY (StudentID) REFERENCES Student(StudentID),
    CONSTRAINT FK_Application_Employee FOREIGN KEY (CreatedByTeacherID) REFERENCES Employee(EmployeeID),
    CONSTRAINT FK_Application_Class FOREIGN KEY (NewClassID) REFERENCES Class(ClassID)
);

-- Triggers initialization ----------------------------------------

-- Trigger to verify Homeroom Teacher role is exactly GVCN
GO
CREATE TRIGGER TRG_Class_VerifyHomeroomTeacherRole
ON Class
AFTER INSERT, UPDATE
AS
BEGIN
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN Employee e ON i.HomeroomTeacherID = e.EmployeeID
        JOIN Account a ON e.AccountID = a.AccountID
        JOIN Role r ON a.RoleID = r.RoleID
        WHERE r.RoleName != N'GVCN'
    )
    BEGIN
        RAISERROR('Assigned homeroom teacher must have the role GVCN.', 16, 1);
        ROLLBACK TRANSACTION;
    END
END;
GO

-- Trigger to ensure teaching assignments belong to GVBM or GVCN
CREATE TRIGGER TRG_TeachingAssignment_VerifyTeacherRole
ON TeachingAssignment
AFTER INSERT, UPDATE
AS
BEGIN
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN Employee e ON i.EmployeeID = e.EmployeeID
        JOIN Account a ON e.AccountID = a.AccountID
        JOIN Role r ON a.RoleID = r.RoleID
        WHERE r.RoleName NOT IN (N'GVBM', N'GVCN')
    )
    BEGIN
        RAISERROR('Teaching assignment requires GVBM or GVCN role.', 16, 1);
        ROLLBACK TRANSACTION;
    END
END;
GO

-- Trigger to prevent a student from existing in multiple classes at the same time
CREATE TRIGGER TRG_ClassPlacement_PreventMultipleClasses
ON ClassPlacement
AFTER INSERT, UPDATE
AS
BEGIN
    IF EXISTS (
        SELECT StudentID
        FROM ClassPlacement
        WHERE StudentID IN (SELECT StudentID FROM inserted)
        GROUP BY StudentID
        HAVING COUNT(ClassID) > 1
    )
    BEGIN
        RAISERROR('A student cannot be enrolled in multiple classes simultaneously.', 16, 1);
        ROLLBACK TRANSACTION;
    END
END;
GO

-- Trigger to enforce maximum class size based on system parameters and update ClassSize count
CREATE TRIGGER TRG_ClassPlacement_EnforceMaxClassSize
ON ClassPlacement
AFTER INSERT
AS
BEGIN
    DECLARE @MaxClassSize INT;
    SELECT @MaxClassSize = CAST(Value AS INT) FROM Parameter WHERE ParameterName = 'MaxClassSize';

    IF EXISTS (
        SELECT i.ClassID
        FROM inserted i
        JOIN ClassPlacement cp ON i.ClassID = cp.ClassID
        GROUP BY i.ClassID
        HAVING COUNT(cp.StudentID) > @MaxClassSize
    )
    BEGIN
        RAISERROR('Class assignment exceeds the maximum allowed student limit.', 16, 1);
        ROLLBACK TRANSACTION;
    END
    ELSE
    BEGIN
        UPDATE Class
        SET ClassSize = (SELECT COUNT(*) FROM ClassPlacement WHERE ClassID = Class.ClassID)
        WHERE ClassID IN (SELECT ClassID FROM inserted);
    END
END;
GO

-- Trigger to automatically calculate the Average Score based on the parameter coefficients
CREATE TRIGGER TRG_Score_CalculateAverage
ON Score
AFTER INSERT, UPDATE
AS
BEGIN
    DECLARE @RegCoef DECIMAL(10,2) = (SELECT Value FROM Parameter WHERE ParameterName = 'RegularScoreCoefficient');
    DECLARE @MidCoef DECIMAL(10,2) = (SELECT Value FROM Parameter WHERE ParameterName = 'MidtermScoreCoefficient');
    DECLARE @FinCoef DECIMAL(10,2) = (SELECT Value FROM Parameter WHERE ParameterName = 'FinalScoreCoefficient');

    UPDATE s
    SET AverageScore = 
        ((ISNULL(i.RegularTestScore, 0) * @RegCoef) + 
         (ISNULL(i.MidTermScore, 0) * @MidCoef) + 
         (ISNULL(i.FinalTermScore, 0) * @FinCoef)) 
        / (@RegCoef + @MidCoef + @FinCoef)
    FROM Score s
    JOIN inserted i ON s.ScoreID = i.ScoreID;
END;
GO
GO

--- Mock Data Insertion (Optional)

-- 1. Insert Roles (Implicit IDs: 1 to 6)
INSERT INTO Role (RoleName) VALUES
(N'Học sinh'),   -- 1
(N'IT Admin'),   -- 2
(N'Hiệu trưởng'), -- 3
(N'GVBM'),       -- 4
(N'GVCN'),       -- 5
(N'Giáo vụ');    -- 6

-- 2. Insert Parameters (Implicit IDs: 1 to 7)
INSERT INTO Parameter (ParameterName, Value) VALUES
('MinAge', 15),
('MaxAge', 20),
('MaxClassSize', 40),
('NumPassingGrade', 5),
('RegularScoreCoefficient', 1),
('MidtermScoreCoefficient', 2),
('FinalScoreCoefficient', 3);

-- 3. Insert Subjects (Implicit IDs: 1 to 9)
INSERT INTO Subject (SubjectName, GradeType, IsDeleted) VALUES
(N'Toán', 'Score', 0),    -- 1
(N'Lý', 'Score', 0),      -- 2
(N'Hóa', 'Score', 0),     -- 3
(N'Sinh', 'Score', 0),    -- 4
(N'Sử', 'Score', 0),      -- 5
(N'Địa', 'Score', 0),     -- 6
(N'Văn', 'Score', 0),     -- 7
(N'Đạo Đức', 'PassFail', 0), -- 8
(N'Thể Dục', 'PassFail', 0); -- 9

-- 4. Insert Accounts (Implicit IDs: 1 to 54)
-- Passwords are pre-hashed using SHA-256 for: admin123, principal123, staff123, teacher123, student123
INSERT INTO Account (RoleID, Username, PasswordHash, IsRequiredChangePassword, IsActive) VALUES
-- Admin & Staff (Accounts 1-3)
(2, 'admin_system', '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', 1, 1),
(3, 'ht_daott', '3549f22fb8622a6d216ef2dcd592e04ed1f1e604cef032d7e5c425e8e72a878e', 1, 1),
(6, 'gv_vulv', '10176e7b7b24d317acfcf8d2064cfd2f24e154f7b5a96603077d5ef813d6a6b6', 1, 1),
-- Homeroom Teachers / GVCN (Accounts 4-12)
(5, 'gv_canpv', 'cde383eee8ee7a4400adf7a15f716f179a2eb97646b37e089eb8d6d04e663416', 1, 1),
(5, 'gv_nguht', 'cde383eee8ee7a4400adf7a15f716f179a2eb97646b37e089eb8d6d04e663416', 1, 1),
(5, 'gv_khoavv', 'cde383eee8ee7a4400adf7a15f716f179a2eb97646b37e089eb8d6d04e663416', 1, 1),
(5, 'gv_hoadt', 'cde383eee8ee7a4400adf7a15f716f179a2eb97646b37e089eb8d6d04e663416', 1, 1),
(5, 'gv_sinhbv', 'cde383eee8ee7a4400adf7a15f716f179a2eb97646b37e089eb8d6d04e663416', 1, 1),
(5, 'gv_sudt', 'cde383eee8ee7a4400adf7a15f716f179a2eb97646b37e089eb8d6d04e663416', 1, 1),
(5, 'gv_diahv', 'cde383eee8ee7a4400adf7a15f716f179a2eb97646b37e089eb8d6d04e663416', 1, 1),
(5, 'gv_anhnt', 'cde383eee8ee7a4400adf7a15f716f179a2eb97646b37e089eb8d6d04e663416', 1, 1),
(5, 'gv_bachdv', 'cde383eee8ee7a4400adf7a15f716f179a2eb97646b37e089eb8d6d04e663416', 1, 1),
-- Subject Teachers / GVBM (Accounts 13-14)
(4, 'gv_duclt', 'cde383eee8ee7a4400adf7a15f716f179a2eb97646b37e089eb8d6d04e663416', 1, 1),
(4, 'gv_lucmv', 'cde383eee8ee7a4400adf7a15f716f179a2eb97646b37e089eb8d6d04e663416', 1, 1),
-- 40 Students (Accounts 15-54, using 'student123' hash)
(1, 'hs_nguyenvanan100110', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_tranthibinh150210', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_lehoangcuong200310', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_phamducduy250410', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_vothiyen300510', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_danghaidang050610', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_buingocgiau100710', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_dominhhieu150810', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_honhatkhang200910', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_lygialinh251010', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_maiphuongmai011110', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_ngotiennam051210', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_phanbaoNgoc120110', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_hoangminhquan180210', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_vuquocson220310', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_trinhanhtu280410', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_daothanhuy020510', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_nguyenhaiha080610', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_tranvanloc140710', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_lethithu200810', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_phamcongvinh100109', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_vongoctram150209', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_dangquanghuy220309', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_buituananh280409', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_dothanhthao050509', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_hovanbinh120609', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_lytuyetmai180709', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_maixuankien250809', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_ngothithuy010909', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_phanvietdung101009', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_hoangkimngan151109', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_vuminhkhoi201209', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_trinhgiahuy050109', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_daothuhuong120209', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_nguyentrongnhan180309', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_tranthimong100108', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_ledinhphuc150208', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_phamthanhdat220308', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_vothanhphong280408', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_dangthiminh050508', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1);

-- 5. Insert Employees (Implicit IDs: 1 to 14, mapping exactly to Accounts 1-14)
INSERT INTO Employee (AccountID, FullName, Gender, Specialization, Email, HireDate, HometownAddress, PhoneNumber, NationalID, Status) VALUES
(1, N'Nguyễn Văn Trị', N'Nam', N'Công nghệ thông tin', 'admin@gmail.mock', '2020-01-15', N'Hà Nội', '0901234560', '001090123450', 'Active'),
(2, N'Trần Thị Đạo', N'Nữ', N'Quản lý giáo dục', 'hieutruong@gmail.mock', '2010-05-15', N'TP.HCM', '0901234561', '079190123451', 'Active'),
(3, N'Lê Văn Vụ', N'Nam', N'Hành chính', 'giaovu@gmail.mock', '2015-08-20', N'Đà Nẵng', '0901234562', '048090123452', 'Active'),
(4, N'Phạm Văn Cán', N'Nam', N'Toán học', 'gvcn10a1@gmail.mock', '2018-09-01', N'TP.HCM', '0901234563', '079090123453', 'Active'),
(5, N'Hoàng Thị Ngữ', N'Nữ', N'Ngữ Văn', 'gvcn10a2@gmail.mock', '2019-09-01', N'Cần Thơ', '0901234564', '092190123454', 'Active'),
(6, N'Vũ Văn Khoa', N'Nam', N'Vật Lý', 'gvcn10a3@gmail.mock', '2017-09-01', N'Hải Phòng', '0901234565', '031090123455', 'Active'),
(7, N'Đặng Thị Hóa', N'Nữ', N'Hóa học', 'gvcn10a4@gmail.mock', '2020-09-01', N'Huế', '0901234566', '046190123456', 'Active'),
(8, N'Bùi Văn Sinh', N'Nam', N'Sinh học', 'gvcn11a1@gmail.mock', '2021-09-01', N'Đồng Nai', '0901234567', '075090123457', 'Active'),
(9, N'Đỗ Thị Sử', N'Nữ', N'Lịch sử', 'gvcn11a2@gmail.mock', '2016-09-01', N'Bình Dương', '0901234568', '074190123458', 'Active'),
(10, N'Hồ Văn Địa', N'Nam', N'Địa lý', 'gvcn11a3@gmail.mock', '2015-09-01', N'Long An', '0901234569', '080090123459', 'Active'),
(11, N'Ngô Thị Anh', N'Nữ', N'Ngoại ngữ', 'gvcn12a1@gmail.mock', '2018-09-01', N'Tiền Giang', '0901234570', '082190123460', 'Active'),
(12, N'Dương Văn Bách', N'Nam', N'Toán học', 'gvcn12a2@gmail.mock', '2019-09-01', N'Bến Tre', '0901234571', '083090123461', 'Active'),
(13, N'Lý Thị Đức', N'Nữ', N'Giáo dục công dân', 'gvbm_dao_duc@gmail.mock', '2022-09-01', N'Vũng Tàu', '0901234572', '077190123462', 'Active'),
(14, N'Mai Văn Lực', N'Nam', N'Giáo dục thể chất', 'gvbm_the_duc@gmail.mock', '2021-09-01', N'Tây Ninh', '0901234573', '072090123463', 'Active');

-- 6. Insert Classes (Implicit IDs: 1 to 9, linked to Employees 4-12)
INSERT INTO Class (ClassName, Grade, HomeroomTeacherID) VALUES
('10A1', 10, 4), -- 1
('10A2', 10, 5), -- 2
('10A3', 10, 6), -- 3
('10A4', 10, 7), -- 4
('11A1', 11, 8), -- 5
('11A2', 11, 9), -- 6
('11A3', 11, 10),-- 7
('12A1', 12, 11),-- 8
('12A2', 12, 12);-- 9

-- 7. Insert Students (Implicit IDs: hs250001 to hs250040, linked to Accounts 15-54)
INSERT INTO Student (AccountID, FullName, Gender, DateOfBirth, PhoneNumber, Email, Address, FamilyBackground, GuardianName, GuardianPhoneNumber, Status) VALUES
-- Grade 10 (DOB: 2010)
(15, N'Nguyễn Văn An', N'Nam', '2010-01-10', '0912345101', 'nguyenvanan100110@gmail.mock', N'Quận 1, TP.HCM', N'Bình thường', N'Nguyễn Văn Ánh', '0987654101', 'Active'),
(16, N'Trần Thị Bình', N'Nữ', '2010-02-15', '0912345102', 'tranthibinh150210@gmail.mock', N'Quận 3, TP.HCM', N'Bình thường', N'Trần Văn Biên', '0987654102', 'Active'),
(17, N'Lê Hoàng Cường', N'Nam', '2010-03-20', '0912345103', 'lehoangcuong200310@gmail.mock', N'Quận 4, TP.HCM', N'Khó khăn', N'Lê Thị Cúc', '0987654103', 'Active'),
(18, N'Phạm Đức Duy', N'Nam', '2010-04-25', '0912345104', 'phamducduy250410@gmail.mock', N'Quận 5, TP.HCM', N'Bình thường', N'Phạm Văn Dũng', '0987654104', 'Active'),
(19, N'Võ Thị Yến', N'Nữ', '2010-05-30', '0912345105', 'vothiyen300510@gmail.mock', N'Quận 7, TP.HCM', N'Bình thường', N'Võ Văn Yên', '0987654105', 'Active'),
(20, N'Đặng Hải Đăng', N'Nam', '2010-06-05', '0912345106', 'danghaidang050610@gmail.mock', N'Quận 8, TP.HCM', N'Bình thường', N'Đặng Thị Đào', '0987654106', 'Active'),
(21, N'Bùi Ngọc Giàu', N'Nữ', '2010-07-10', '0912345107', 'buingocgiau100710@gmail.mock', N'Quận 10, TP.HCM', N'Bình thường', N'Bùi Văn Gấm', '0987654107', 'Active'),
(22, N'Đỗ Minh Hiếu', N'Nam', '2010-08-15', '0912345108', 'dominhhieu150810@gmail.mock', N'Quận 11, TP.HCM', N'Bình thường', N'Đỗ Thị Hiền', '0987654108', 'Active'),
(23, N'Hồ Nhật Khang', N'Nam', '2010-09-20', '0912345109', 'honhatkhang200910@gmail.mock', N'Tân Bình, TP.HCM', N'Bình thường', N'Hồ Văn Khánh', '0987654109', 'Active'),
(24, N'Lý Gia Linh', N'Nữ', '2010-10-25', '0912345110', 'lygialinh251010@gmail.mock', N'Gò Vấp, TP.HCM', N'Khó khăn', N'Lý Thị Lan', '0987654110', 'Active'),
(25, N'Mai Phương Mai', N'Nữ', '2010-11-01', '0912345111', 'maiphuongmai011110@gmail.mock', N'Tân Phú, TP.HCM', N'Bình thường', N'Mai Văn Mẫn', '0987654111', 'Active'),
(26, N'Ngô Tiến Nam', N'Nam', '2010-12-05', '0912345112', 'ngotiennam051210@gmail.mock', N'Phú Nhuận, TP.HCM', N'Bình thường', N'Ngô Thị Nga', '0987654112', 'Active'),
(27, N'Phan Bảo Ngọc', N'Nữ', '2010-01-12', '0912345113', 'phanbaongoc120110@gmail.mock', N'Bình Thạnh, TP.HCM', N'Bình thường', N'Phan Văn Nghĩa', '0987654113', 'Active'),
(28, N'Hoàng Minh Quân', N'Nam', '2010-02-18', '0912345114', 'hoangminhquan180210@gmail.mock', N'Bình Tân, TP.HCM', N'Bình thường', N'Hoàng Thị Quyên', '0987654114', 'Active'),
(29, N'Vũ Quốc Sơn', N'Nam', '2010-03-22', '0912345115', 'vuquocson220310@gmail.mock', N'Thủ Đức, TP.HCM', N'Bình thường', N'Vũ Văn Sáng', '0987654115', 'Active'),
(30, N'Trịnh Anh Tú', N'Nam', '2010-04-28', '0912345116', 'trinhanhtu280410@gmail.mock', N'Nhà Bè, TP.HCM', N'Khó khăn', N'Trịnh Thị Thủy', '0987654116', 'Active'),
(31, N'Đào Thanh Uyển', N'Nữ', '2010-05-02', '0912345117', 'daothanhuyen020510@gmail.mock', N'Hóc Môn, TP.HCM', N'Bình thường', N'Đào Văn Uy', '0987654117', 'Active'),
(32, N'Nguyễn Hải Hà', N'Nữ', '2010-06-08', '0912345118', 'nguyenhaiha080610@gmail.mock', N'Bình Chánh, TP.HCM', N'Bình thường', N'Nguyễn Văn Hùng', '0987654118', 'Active'),
(33, N'Trần Văn Lộc', N'Nam', '2010-07-14', '0912345119', 'tranvanloc140710@gmail.mock', N'Quận 1, TP.HCM', N'Bình thường', N'Trần Thị Liên', '0987654119', 'Active'),
(34, N'Lê Thị Thu', N'Nữ', '2010-08-20', '0912345120', 'lethithu200810@gmail.mock', N'Quận 3, TP.HCM', N'Bình thường', N'Lê Văn Thắng', '0987654120', 'Active'),
-- Grade 11 (DOB: 2009)
(35, N'Phạm Công Vinh', N'Nam', '2009-01-10', '0912345121', 'phamcongvinh100109@gmail.mock', N'Quận 4, TP.HCM', N'Bình thường', N'Phạm Thị Vân', '0987654121', 'Active'),
(36, N'Võ Ngọc Trâm', N'Nữ', '2009-02-15', '0912345122', 'vongoctram150209@gmail.mock', N'Quận 5, TP.HCM', N'Khó khăn', N'Võ Văn Triết', '0987654122', 'Active'),
(37, N'Đặng Quang Huy', N'Nam', '2009-03-22', '0912345123', 'dangquanghuy220309@gmail.mock', N'Quận 7, TP.HCM', N'Bình thường', N'Đặng Thị Hoa', '0987654123', 'Active'),
(38, N'Bùi Tuấn Anh', N'Nam', '2009-04-28', '0912345124', 'buituananh280409@gmail.mock', N'Quận 8, TP.HCM', N'Bình thường', N'Bùi Văn Tú', '0987654124', 'Active'),
(39, N'Đỗ Thanh Thảo', N'Nữ', '2009-05-05', '0912345125', 'dothanhthao050509@gmail.mock', N'Quận 10, TP.HCM', N'Bình thường', N'Đỗ Thị Tâm', '0987654125', 'Active'),
(40, N'Hồ Văn Bình', N'Nam', '2009-06-12', '0912345126', 'hovanbinh120609@gmail.mock', N'Quận 11, TP.HCM', N'Khó khăn', N'Hồ Thị Bích', '0987654126', 'Active'),
(41, N'Lý Tuyết Mai', N'Nữ', '2009-07-18', '0912345127', 'lytuyetmai180709@gmail.mock', N'Tân Bình, TP.HCM', N'Bình thường', N'Lý Văn Mười', '0987654127', 'Active'),
(42, N'Mai Xuân Kiên', N'Nam', '2009-08-25', '0912345128', 'maixuankien250809@gmail.mock', N'Gò Vấp, TP.HCM', N'Bình thường', N'Mai Thị Kim', '0987654128', 'Active'),
(43, N'Ngô Thị Thủy', N'Nữ', '2009-09-01', '0912345129', 'ngothithuy010909@gmail.mock', N'Tân Phú, TP.HCM', N'Bình thường', N'Ngô Văn Thái', '0987654129', 'Active'),
(44, N'Phan Việt Dũng', N'Nam', '2009-10-10', '0912345130', 'phanvietdung101009@gmail.mock', N'Phú Nhuận, TP.HCM', N'Bình thường', N'Phan Thị Diệp', '0987654130', 'Active'),
(45, N'Hoàng Kim Ngân', N'Nữ', '2009-11-15', '0912345131', 'hoangkimngan151109@gmail.mock', N'Bình Thạnh, TP.HCM', N'Bình thường', N'Hoàng Văn Ngữ', '0987654131', 'Active'),
(46, N'Vũ Minh Khôi', N'Nam', '2009-12-20', '0912345132', 'vuminhkhoi201209@gmail.mock', N'Bình Tân, TP.HCM', N'Khó khăn', N'Vũ Thị Khuyên', '0987654132', 'Active'),
(47, N'Trịnh Gia Huy', N'Nam', '2009-01-05', '0912345133', 'trinhgiahuy050109@gmail.mock', N'Thủ Đức, TP.HCM', N'Bình thường', N'Trịnh Văn Hoàng', '0987654133', 'Active'),
(48, N'Đào Thu Hương', N'Nữ', '2009-02-12', '0912345134', 'daothuhuong120209@gmail.mock', N'Nhà Bè, TP.HCM', N'Bình thường', N'Đào Thị Hạnh', '0987654134', 'Active'),
(49, N'Nguyễn Trọng Nhân', N'Nam', '2009-03-18', '0912345135', 'nguyentrongnhan180309@gmail.mock', N'Hóc Môn, TP.HCM', N'Bình thường', N'Nguyễn Văn Nghĩa', '0987654135', 'Active'),
-- Grade 12 (DOB: 2008)
(50, N'Trần Thị Mộng', N'Nữ', '2008-01-10', '0912345136', 'tranthimong100108@gmail.mock', N'Bình Chánh, TP.HCM', N'Bình thường', N'Trần Văn Mẫn', '0987654136', 'Active'),
(51, N'Lê Đình Phúc', N'Nam', '2008-02-15', '0912345137', 'ledinhphuc150208@gmail.mock', N'Quận 1, TP.HCM', N'Bình thường', N'Lê Thị Phương', '0987654137', 'Active'),
(52, N'Phạm Thành Đạt', N'Nam', '2008-03-22', '0912345138', 'phamthanhdat220308@gmail.mock', N'Quận 3, TP.HCM', N'Khó khăn', N'Phạm Văn Đồng', '0987654138', 'Active'),
(53, N'Võ Thanh Phong', N'Nam', '2008-04-28', '0912345139', 'vothanhphong280408@gmail.mock', N'Quận 4, TP.HCM', N'Bình thường', N'Võ Thị Phụng', '0987654139', 'Active'),
(54, N'Đặng Thị Minh', N'Nữ', '2008-05-05', '0912345140', 'dangthiminh050508@gmail.mock', N'Quận 5, TP.HCM', N'Bình thường', N'Đặng Văn Mạnh', '0987654140', 'Active');

-- 8. Insert ClassPlacements 
INSERT INTO ClassPlacement (StudentID, ClassID) VALUES
-- Grade 10 Classes
('hs250001', 1), ('hs250002', 1), ('hs250003', 1), ('hs250004', 1), ('hs250005', 1), -- 10A1
('hs250006', 2), ('hs250007', 2), ('hs250008', 2), ('hs250009', 2), ('hs250010', 2), -- 10A2
('hs250011', 3), ('hs250012', 3), ('hs250013', 3), ('hs250014', 3), ('hs250015', 3), -- 10A3
('hs250016', 4), ('hs250017', 4), ('hs250018', 4), ('hs250019', 4), ('hs250020', 4), -- 10A4
-- Grade 11 Classes
('hs250021', 5), ('hs250022', 5), ('hs250023', 5), ('hs250024', 5), ('hs250025', 5), -- 11A1
('hs250026', 6), ('hs250027', 6), ('hs250028', 6), ('hs250029', 6), ('hs250030', 6), -- 11A2
('hs250031', 7), ('hs250032', 7), ('hs250033', 7), ('hs250034', 7), ('hs250035', 7), -- 11A3
-- Grade 12 Classes
('hs250036', 8), ('hs250037', 8), ('hs250038', 8), -- 12A1
('hs250039', 9), ('hs250040', 9); -- 12A2

-- 9. Insert TeachingAssignments
INSERT INTO TeachingAssignment (EmployeeID, ClassID, SubjectID) VALUES
(4, 1, 1),  -- GVCN 10A1 (Emp 4) teaches Toán(1) to 10A1(1)
(5, 2, 7),  -- GVCN 10A2 (Emp 5) teaches Văn(7) to 10A2(2)
(6, 3, 2),  -- GVCN 10A3 (Emp 6) teaches Lý(2) to 10A3(3)
(13, 1, 8), -- GVBM Đạo Đức (Emp 13) teaches 10A1(1)
(14, 5, 9), -- GVBM Thể Dục (Emp 14) teaches 11A1(5)
(8, 5, 4),  -- GVCN 11A1 (Emp 8) teaches Sinh(4) to 11A1(5)
(11, 8, 7), -- GVCN 12A1 (Emp 11) teaches Văn(7) to 12A1(8)
(12, 9, 1); -- GVCN 12A2 (Emp 12) teaches Toán(1) to 12A2(9)

-- 10. Insert Scores (Implicit IDs: 1 to 8)
INSERT INTO Score (StudentID, SubjectID, RegularTestScore, MidTermScore, FinalTermScore) VALUES
('hs250001', 1, 8.5, 9.0, 8.0),   
('hs250002', 1, 6.0, 7.5, 7.0),   
('hs250006', 7, 7.0, 8.0, 8.5),   
('hs250011', 2, 9.0, 9.5, 9.0),  
('hs250021', 4, 8.0, 7.0, 8.0),  
('hs250022', 9, 10.0, 10.0, 10.0),
('hs250036', 7, 7.5, 8.0, 8.5),  
('hs250039', 1, 9.5, 9.0, 9.5);  

-- 11. Insert Applications (Implicit IDs: 1 to 4)
INSERT INTO Application (StudentID, CreatedByTeacherID, NewClassID, RequestType, Reason, FeedbackNote, Status, RespondedAt) VALUES
('hs250001', 4, 2, 'ClassTransfer', N'Chuyển sang lớp 10A2 để học cùng anh em họ', NULL, 'Pending', NULL),
('hs250015', 6, NULL, 'DropOut', N'Gia đình chuyển công tác ra nước ngoài', N'Đã xác nhận với phụ huynh', 'Executed', '2024-04-20 09:30:00'),
('hs250025', 8, 6, 'ClassTransfer', N'Không theo kịp chương trình nâng cao', N'Chuyển sang lớp 11A2', 'Executed', '2024-05-15 14:00:00'),
('hs250040', 12, NULL, 'DropOut', N'Lý do sức khỏe', NULL, 'Pending', NULL);


--Data để test cho chức năng lập báo cáo của giáo viên Phạm Văn Cán

-- 1. Xóa điểm cũ của lớp 10A1 (hs250001 đến hs250005) để tránh trùng lặp dữ liệu
DELETE FROM Score WHERE StudentID IN ('hs250001', 'hs250002', 'hs250003', 'hs250004', 'hs250005');

-- 2. Thêm điểm cho hs250001 (Giỏi - Pass toàn bộ)
INSERT INTO Score (StudentID, SubjectID, RegularTestScore, MidTermScore, FinalTermScore) VALUES
('hs250001', 1, 9.0, 9.0, 9.5), ('hs250001', 2, 8.5, 9.0, 9.0), ('hs250001', 3, 9.0, 8.5, 9.0),
('hs250001', 4, 9.5, 9.5, 9.5), ('hs250001', 5, 8.0, 8.5, 8.0), ('hs250001', 6, 9.0, 9.0, 9.0),
('hs250001', 7, 8.5, 8.5, 8.5), ('hs250001', 8, 10.0, 10.0, 10.0), ('hs250001', 9, 10.0, 10.0, 10.0);

-- 3. Thêm điểm cho hs250002 (Khá - Pass toàn bộ)
INSERT INTO Score (StudentID, SubjectID, RegularTestScore, MidTermScore, FinalTermScore) VALUES
('hs250002', 1, 6.0, 6.5, 6.0), ('hs250002', 2, 7.0, 7.0, 7.5), ('hs250002', 3, 6.5, 6.5, 6.0),
('hs250002', 4, 7.5, 8.0, 7.5), ('hs250002', 5, 6.0, 6.0, 6.5), ('hs250002', 6, 6.5, 7.0, 6.5),
('hs250002', 7, 7.0, 6.5, 7.0), ('hs250002', 8, 10.0, 10.0, 10.0), ('hs250002', 9, 10.0, 10.0, 10.0);

-- 4. Thêm điểm cho hs250003 (Trung Bình - Pass toàn bộ, điểm mấp mé 5.0)
INSERT INTO Score (StudentID, SubjectID, RegularTestScore, MidTermScore, FinalTermScore) VALUES
('hs250003', 1, 5.0, 5.5, 5.0), ('hs250003', 2, 5.5, 5.0, 5.5), ('hs250003', 3, 5.0, 5.0, 5.0),
('hs250003', 4, 6.0, 5.5, 6.0), ('hs250003', 5, 5.0, 6.0, 5.5), ('hs250003', 6, 5.5, 5.5, 5.5),
('hs250003', 7, 5.5, 5.0, 5.5), ('hs250003', 8, 10.0, 10.0, 10.0), ('hs250003', 9, 10.0, 10.0, 10.0);

-- 5. Thêm điểm cho hs250004 (RỚT MÔN TOÁN, CÁC MÔN KHÁC ĐẠT CAO) -> Dùng để test DoD 1
INSERT INTO Score (StudentID, SubjectID, RegularTestScore, MidTermScore, FinalTermScore) VALUES
('hs250004', 1, 4.0, 4.5, 4.0), /* <-- Tạch môn Toán (Môn 1) */ 
('hs250004', 2, 7.0, 7.5, 7.0), ('hs250004', 3, 6.5, 7.0, 6.5),
('hs250004', 4, 8.0, 8.5, 8.0), ('hs250004', 5, 7.5, 7.0, 7.5), ('hs250004', 6, 6.0, 6.5, 6.0),
('hs250004', 7, 6.5, 6.0, 6.5), ('hs250004', 8, 10.0, 10.0, 10.0), ('hs250004', 9, 10.0, 10.0, 10.0);

-- 6. Thêm điểm cho hs250005 (Rớt 3 môn: Toán, Lý, Hóa) 
INSERT INTO Score (StudentID, SubjectID, RegularTestScore, MidTermScore, FinalTermScore) VALUES
('hs250005', 1, 3.0, 3.5, 3.0), ('hs250005', 2, 4.0, 4.5, 4.0), ('hs250005', 3, 3.5, 4.0, 3.5),
('hs250005', 4, 6.0, 6.5, 6.0), ('hs250005', 5, 7.0, 7.5, 7.0), ('hs250005', 6, 5.5, 6.0, 5.5),
('hs250005', 7, 6.5, 6.5, 6.5), ('hs250005', 8, 10.0, 10.0, 10.0), ('hs250005', 9, 10.0, 10.0, 10.0);