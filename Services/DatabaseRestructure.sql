IF OBJECT_ID('dbo.vw_ClassSize', 'V') IS NOT NULL DROP VIEW dbo.vw_ClassSize;
IF OBJECT_ID('dbo.vw_DeletedSubjects', 'V') IS NOT NULL DROP VIEW dbo.vw_DeletedSubjects;
GO
DROP TABLE IF EXISTS StudentAverage;
DROP TABLE IF EXISTS ClassReport;
DROP TABLE IF EXISTS SubjectReport;
DROP TABLE IF EXISTS Application;
DROP TABLE IF EXISTS Status;
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

-- Parameter & Role
CREATE TABLE Parameter (
    ParameterID   INT IDENTITY(1,1) PRIMARY KEY,
    ParameterName VARCHAR(100) NOT NULL UNIQUE,       
    Value         DECIMAL(10,2) NOT NULL
);

CREATE TABLE Role (
    RoleID   INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(100) NOT NULL UNIQUE,
    CONSTRAINT CHK_Role_RoleName CHECK (
        RoleName IN (N'Học sinh', N'IT Admin', N'Hiệu trưởng', N'GVBM', N'GVCN', N'Giáo vụ')
    )
);

-- Subject  (defined BEFORE Employee so Specialization FK works)
CREATE TABLE Subject (
    SubjectID   INT IDENTITY(1,1) PRIMARY KEY,
    SubjectName NVARCHAR(100) NOT NULL UNIQUE,
    GradeType   VARCHAR(50)   NOT NULL,
    IsDeleted   BIT DEFAULT 0,
    CONSTRAINT CHK_Subject_GradeType CHECK (GradeType IN ('Score', 'PassFail'))
);

-- Account & Employee
CREATE TABLE Account (
    AccountID                 INT IDENTITY(1,1) PRIMARY KEY,
    RoleID                    INT NOT NULL,
    Username                  VARCHAR(100) UNIQUE NOT NULL,
    PasswordHash              VARCHAR(255) NOT NULL,
    IsRequiredChangePassword  BIT DEFAULT 1,
    IsActive                  BIT DEFAULT 1,
    CONSTRAINT FK_Account_Role FOREIGN KEY (RoleID) REFERENCES Role(RoleID)
);

CREATE TABLE Employee (
    EmployeeID              INT IDENTITY(1,1) PRIMARY KEY,
    AccountID               INT UNIQUE NOT NULL,
    FullName                NVARCHAR(255) NOT NULL,
    Gender                  NVARCHAR(50),
    Specialization INT NULL,                  -- FK→Subject; NULL for non-teaching staff
    Email                   VARCHAR(255) UNIQUE,
    HireDate                DATE,
    HometownAddress         NVARCHAR(255),
    PhoneNumber             VARCHAR(20),
    NationalID              VARCHAR(50) UNIQUE,
    Status                  NVARCHAR(50) NOT NULL DEFAULT N'Active',
    CONSTRAINT FK_Employee_Account        FOREIGN KEY (AccountID) REFERENCES Account(AccountID),
    CONSTRAINT FK_Employee_Specialization FOREIGN KEY (Specialization) REFERENCES Subject(SubjectID),
    CONSTRAINT CHK_Employee_Status CHECK (Status IN (N'Active', N'Inactive'))
);

-- Student
CREATE SEQUENCE Seq_Student_ID AS INT START WITH 1 INCREMENT BY 1;
GO

CREATE TABLE Student (
    StudentID            VARCHAR(10) PRIMARY KEY
        DEFAULT ('hs25' + RIGHT('0000' + CAST(NEXT VALUE FOR Seq_Student_ID AS VARCHAR(10)), 4)),
    AccountID            INT UNIQUE NOT NULL,
    FullName             NVARCHAR(255) NOT NULL,
    Gender               NVARCHAR(50),
    DateOfBirth          DATE,
    PhoneNumber          VARCHAR(20),
    Email                VARCHAR(255) UNIQUE,
    Address              NVARCHAR(255),
    FamilyBackground     NVARCHAR(MAX)
        CONSTRAINT CHK_Student_FamilyBackground CHECK (FamilyBackground IN (N'Bình thường', N'Khó khăn')),
    GuardianName         NVARCHAR(255),
    GuardianPhoneNumber  VARCHAR(20),
    Status               NVARCHAR(50) NOT NULL DEFAULT N'Active',
    CONSTRAINT FK_Student_Account FOREIGN KEY (AccountID) REFERENCES Account(AccountID),
    CONSTRAINT CHK_Student_Status CHECK (Status IN (N'Active', N'Inactive'))
);

-- Class  (ClassSize column removed → see vw_ClassSize)
CREATE TABLE Class (
    ClassID            INT IDENTITY(1,1) PRIMARY KEY,
    ClassName          NVARCHAR(50) NOT NULL,
    Grade              INT NOT NULL,
    HomeroomTeacherID  INT,
    AcademicYear       NVARCHAR(50) NOT NULL DEFAULT '2025-2026',
    CONSTRAINT CHK_Class_Grade CHECK (Grade IN (10, 11, 12)),
    CONSTRAINT FK_Class_Employee FOREIGN KEY (HomeroomTeacherID) REFERENCES Employee(EmployeeID),
    CONSTRAINT UQ_Class_NameYear UNIQUE (ClassName, AcademicYear),   -- [2.3]
    CONSTRAINT UQ_Class_IDYear   UNIQUE (ClassID, AcademicYear)      -- [2.6] target for composite FK
);

-- ClassPlacement  (temporal model — supports class transfers + history)
-- A student can move from a class to another class within a year, but at any
-- given moment is in exactly ONE class. EffectiveTo IS NULL = current.
CREATE TABLE ClassPlacement (
    PlacementID   INT IDENTITY(1,1) PRIMARY KEY,
    StudentID     VARCHAR(10) NOT NULL,
    ClassID       INT NOT NULL,
    AcademicYear  NVARCHAR(50) NOT NULL,
    EffectiveFrom DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    EffectiveTo   DATE NULL,
    CONSTRAINT FK_ClassPlacement_Student   FOREIGN KEY (StudentID) REFERENCES Student(StudentID),
    CONSTRAINT FK_ClassPlacement_ClassYear FOREIGN KEY (ClassID, AcademicYear)
        REFERENCES Class(ClassID, AcademicYear),                    -- [2.6]
    CONSTRAINT CHK_ClassPlacement_Dates CHECK (EffectiveTo IS NULL OR EffectiveTo > EffectiveFrom)
);
GO
-- One current placement per student at any time
CREATE UNIQUE INDEX UQ_ClassPlacement_CurrentStudent
    ON ClassPlacement(StudentID)
    WHERE EffectiveTo IS NULL;
GO

-- View replacing the dropped Class.ClassSize column
CREATE VIEW vw_ClassSize AS
SELECT  c.ClassID,
        c.ClassName,
        c.AcademicYear,
        COUNT(cp.PlacementID) AS ClassSize
FROM    Class c
LEFT JOIN ClassPlacement cp
       ON cp.ClassID = c.ClassID
      AND cp.AcademicYear = c.AcademicYear
      AND cp.EffectiveTo IS NULL
GROUP BY c.ClassID, c.ClassName, c.AcademicYear;
GO

-- TeachingAssignment
CREATE TABLE TeachingAssignment (
    EmployeeID    INT NOT NULL,
    ClassID       INT NOT NULL,
    SubjectID     INT NOT NULL,
    Semester      NVARCHAR(50) NOT NULL DEFAULT N'Học kỳ 1',
    AcademicYear  NVARCHAR(50) NOT NULL DEFAULT '2025-2026',
    CONSTRAINT PK_TeachingAssignment PRIMARY KEY (EmployeeID, ClassID, SubjectID, Semester, AcademicYear),
    CONSTRAINT FK_TA_Employee  FOREIGN KEY (EmployeeID) REFERENCES Employee(EmployeeID),
    CONSTRAINT FK_TA_ClassYear FOREIGN KEY (ClassID, AcademicYear)
        REFERENCES Class(ClassID, AcademicYear),                    -- [2.6]
    CONSTRAINT FK_TA_Subject   FOREIGN KEY (SubjectID) REFERENCES Subject(SubjectID),
    CONSTRAINT CHK_TA_Semester CHECK (Semester IN (N'Học kỳ 1', N'Học kỳ 2'))
);

-- Score  (AverageScore as PERSISTED computed column)
-- Hệ số theo quy định Bộ GD&ĐT: KT thường xuyên = 1, Giữa kỳ = 2, Cuối kỳ = 3.
-- Hard-coded ở đây để có thể PERSISTED (computed column PERSISTED yêu cầu biểu thức tất định).
-- Các hàng coefficient trong Parameter giữ lại để hiển thị/cấu hình UI nếu cần.
CREATE TABLE Score (
    ScoreID           INT IDENTITY(1,1) PRIMARY KEY,
    StudentID         VARCHAR(10) NOT NULL,
    SubjectID         INT NOT NULL,
    Semester          NVARCHAR(50) NOT NULL DEFAULT N'Học kỳ 1',
    AcademicYear      NVARCHAR(50) NOT NULL DEFAULT '2025-2026',
    RegularTestScore  DECIMAL(5,2),
    MidTermScore      DECIMAL(5,2),
    FinalTermScore    DECIMAL(5,2),
    -- [Note] Môn PassFail (GDCD/GDTC) dùng chung 3 cột điểm số — encode Đ=10, KĐ=0.
    -- Trigger TRG_Score_EnforceGradeType ràng buộc giá trị ∈ {0, 10, NULL} cho môn GradeType='PassFail'.
    AverageScore AS (
        CASE
            WHEN RegularTestScore IS NULL
             AND MidTermScore     IS NULL
             AND FinalTermScore   IS NULL THEN NULL
            ELSE CAST(
                (ISNULL(RegularTestScore,0) * 1.0
               + ISNULL(MidTermScore,    0) * 2.0
               + ISNULL(FinalTermScore,  0) * 3.0) / 6.0
            AS DECIMAL(5,2))
        END
    ) PERSISTED,                                                    -- [2.1]
    CONSTRAINT FK_Score_Student FOREIGN KEY (StudentID) REFERENCES Student(StudentID),
    CONSTRAINT FK_Score_Subject FOREIGN KEY (SubjectID) REFERENCES Subject(SubjectID),
    CONSTRAINT UQ_Score         UNIQUE (StudentID, SubjectID, Semester, AcademicYear),  -- [2.3]
    CONSTRAINT CHK_Score_Semester CHECK (Semester IN (N'Học kỳ 1', N'Học kỳ 2'))
);

-- Status (lookup table for Application status)
CREATE TABLE Status (
    StatusID    INT IDENTITY(1,1) PRIMARY KEY,
    StatusName  VARCHAR(50) NOT NULL UNIQUE
);

-- Application
CREATE TABLE Application (
    RequestID           INT IDENTITY(1,1) PRIMARY KEY,
    StudentID           VARCHAR(10) NOT NULL,
    CreatedByTeacherID  INT,
    NewClassID          INT,
    RequestType         VARCHAR(50) NOT NULL,
    Reason              NVARCHAR(MAX),
    FeedbackNote        NVARCHAR(MAX),
    StatusID            INT NOT NULL DEFAULT 1,
    RespondedAt         DATETIME,
    CONSTRAINT CHK_Application_RequestType CHECK (RequestType IN ('ClassTransfer', 'DropOut')),
    CONSTRAINT FK_Application_Status   FOREIGN KEY (StatusID)          REFERENCES Status(StatusID),
    CONSTRAINT FK_Application_Student  FOREIGN KEY (StudentID)          REFERENCES Student(StudentID),
    CONSTRAINT FK_Application_Employee FOREIGN KEY (CreatedByTeacherID) REFERENCES Employee(EmployeeID),
    CONSTRAINT FK_Application_Class    FOREIGN KEY (NewClassID)         REFERENCES Class(ClassID)
);

-- SubjectReport  (PassRate as PERSISTED computed column)
CREATE TABLE SubjectReport (
    SubjectReportID     INT IDENTITY(1,1) PRIMARY KEY,
    ClassID             INT NOT NULL,
    SubjectID           INT NOT NULL,
    Semester            NVARCHAR(50) NOT NULL,
    AcademicYear        NVARCHAR(50) NOT NULL,
    TotalStudents       INT NOT NULL,    -- snapshot — independent after IsLocked=1
    PassedStudents      INT NOT NULL,    -- snapshot
    PassRate AS (
        CASE WHEN TotalStudents = 0 THEN 0
             ELSE CAST(PassedStudents AS DECIMAL(7,4)) * 100.0 / TotalStudents
        END
    ) PERSISTED,                                                    
    IsLocked            BIT DEFAULT 1,
    CreatedByTeacherID  INT NOT NULL,
    CreatedAt           DATETIME DEFAULT GETDATE(),
    CONSTRAINT UQ_SubjectReport UNIQUE (ClassID, SubjectID, Semester, AcademicYear),
    CONSTRAINT FK_SubjectReport_Class    FOREIGN KEY (ClassID)             REFERENCES Class(ClassID),
    CONSTRAINT FK_SubjectReport_Subject  FOREIGN KEY (SubjectID)           REFERENCES Subject(SubjectID),
    CONSTRAINT FK_SubjectReport_Employee FOREIGN KEY (CreatedByTeacherID)  REFERENCES Employee(EmployeeID),
    CONSTRAINT CHK_SubjectReport_Counts  CHECK (PassedStudents <= TotalStudents AND TotalStudents >= 0)
);

-- ClassReport
CREATE TABLE ClassReport (
    ClassReportID       INT IDENTITY(1,1) PRIMARY KEY,
    ClassID             INT NOT NULL,
    Semester            NVARCHAR(50) NOT NULL,
    AcademicYear        NVARCHAR(50) NOT NULL,
    TotalStudents       INT NOT NULL,   -- snapshot
    IsLocked            BIT DEFAULT 1,
    CreatedByTeacherID  INT NOT NULL,
    CreatedAt           DATETIME DEFAULT GETDATE(),
    CONSTRAINT UQ_ClassReport UNIQUE (ClassID, Semester, AcademicYear),
    CONSTRAINT FK_ClassReport_Class    FOREIGN KEY (ClassID)            REFERENCES Class(ClassID),
    CONSTRAINT FK_ClassReport_Employee FOREIGN KEY (CreatedByTeacherID) REFERENCES Employee(EmployeeID)
);
GO

-- StudentAverage — snapshot TB tất cả môn của HS (loại trừ PassFail)
-- [Note] Bảng được duy trì hoàn toàn tự động bởi TRG_Score_UpdateStudentAverage.
-- App layer chỉ cần SELECT từ bảng này, không cần JOIN/AVG runtime.
CREATE TABLE StudentAverage (
    StudentID       VARCHAR(10) NOT NULL,
    Semester        NVARCHAR(50) NOT NULL,
    AcademicYear    NVARCHAR(50) NOT NULL,
    OverallAverage  DECIMAL(5,2) NULL,           -- NULL nếu HS chưa có môn Score nào
    SubjectCount    INT NOT NULL DEFAULT 0,      -- số môn Score đã có điểm (sanity check)
    UpdatedAt       DATETIME DEFAULT GETDATE(),
    CONSTRAINT PK_StudentAverage           PRIMARY KEY (StudentID, Semester, AcademicYear),
    CONSTRAINT FK_StudentAverage_Student   FOREIGN KEY (StudentID) REFERENCES Student(StudentID),
    CONSTRAINT CHK_StudentAverage_Semester CHECK (Semester IN (N'Học kỳ 1', N'Học kỳ 2'))
);
GO

-- View phục vụ chức năng "Khôi phục môn" trên UI
CREATE VIEW vw_DeletedSubjects AS
SELECT  s.SubjectID,
        s.SubjectName,
        s.GradeType,
        (SELECT COUNT(*) FROM Score              WHERE SubjectID = s.SubjectID) AS ScoreCount,
        (SELECT COUNT(*) FROM TeachingAssignment WHERE SubjectID = s.SubjectID) AS TeachingCount,
        (SELECT COUNT(*) FROM SubjectReport      WHERE SubjectID = s.SubjectID) AS ReportCount
FROM    Subject s
WHERE   s.IsDeleted = 1;
GO

-- TRIGGERS
-- =============================================================

-- Verify Homeroom Teacher role = GVCN
CREATE TRIGGER TRG_Class_VerifyHomeroomTeacherRole
ON Class
AFTER INSERT, UPDATE
AS
BEGIN
    IF EXISTS (
        SELECT 1
        FROM   inserted i
        JOIN   Employee e ON i.HomeroomTeacherID = e.EmployeeID
        JOIN   Account  a ON e.AccountID = a.AccountID
        JOIN   Role     r ON a.RoleID    = r.RoleID
        WHERE  r.RoleName <> N'GVCN'
    )
    BEGIN
        RAISERROR('Assigned homeroom teacher must have the role GVCN.', 16, 1);
        ROLLBACK TRANSACTION;
    END
END;
GO

-- Teaching assignment must belong to GVBM or GVCN
CREATE TRIGGER TRG_TeachingAssignment_VerifyTeacherRole
ON TeachingAssignment
AFTER INSERT, UPDATE
AS
BEGIN
    IF EXISTS (
        SELECT 1
        FROM   inserted i
        JOIN   Employee e ON i.EmployeeID = e.EmployeeID
        JOIN   Account  a ON e.AccountID  = a.AccountID
        JOIN   Role     r ON a.RoleID     = r.RoleID
        WHERE  r.RoleName NOT IN (N'GVBM', N'GVCN')
    )
    BEGIN
        RAISERROR('Teaching assignment requires GVBM or GVCN role.', 16, 1);
        ROLLBACK TRANSACTION;
    END
END;
GO

-- Enforce max class size (counts only CURRENT placements)
CREATE TRIGGER TRG_ClassPlacement_EnforceMaxClassSize
ON ClassPlacement
AFTER INSERT, UPDATE
AS
BEGIN
    DECLARE @MaxClassSize INT;
    SELECT  @MaxClassSize = CAST(Value AS INT)
    FROM    Parameter
    WHERE   ParameterName = 'MaxClassSize';

    IF EXISTS (
        SELECT cp.ClassID
        FROM   ClassPlacement cp
        WHERE  cp.EffectiveTo IS NULL
          AND  cp.ClassID IN (SELECT ClassID FROM inserted)
        GROUP BY cp.ClassID
        HAVING COUNT(*) > @MaxClassSize
    )
    BEGIN
        RAISERROR('Class assignment exceeds the maximum allowed student limit.', 16, 1);
        ROLLBACK TRANSACTION;
    END
END;
GO

-- Score.AcademicYear must match the student's current placement year
CREATE TRIGGER TRG_Score_VerifyAcademicYear
ON Score
AFTER INSERT, UPDATE
AS
BEGIN
    IF EXISTS (
        SELECT 1
        FROM   inserted i
        WHERE  NOT EXISTS (
            SELECT 1
            FROM   ClassPlacement cp
            WHERE  cp.StudentID    = i.StudentID
              AND  cp.AcademicYear = i.AcademicYear
        )
    )
    BEGIN
        RAISERROR('Score.AcademicYear must match a ClassPlacement of this student.', 16, 1);
        ROLLBACK TRANSACTION;
    END
END;
GO

-- [Note] Encode Đ/KĐ → 10/0 trong cùng 3 cột điểm số (đã thống nhất tối giản schema)
-- - Môn GradeType='PassFail': 3 cột Regular/MidTerm/FinalTermScore chỉ chứa 0 (KĐ), 10 (Đ), hoặc NULL.
-- - Môn GradeType='Score'   : không ràng buộc thêm ở đây (có thể bổ sung CHECK 0..10 chung sau).
-- UI chịu trách nhiệm map Đ↔10 / KĐ↔0 khi nhập & hiển thị.
CREATE TRIGGER TRG_Score_EnforceGradeType
ON Score
AFTER INSERT, UPDATE
AS
BEGIN
    IF EXISTS (
        SELECT 1
        FROM   inserted i
        JOIN   Subject  s ON i.SubjectID = s.SubjectID
        WHERE  s.GradeType = 'PassFail'
          AND ((i.RegularTestScore IS NOT NULL AND i.RegularTestScore NOT IN (0, 10))
            OR (i.MidTermScore     IS NOT NULL AND i.MidTermScore     NOT IN (0, 10))
            OR (i.FinalTermScore   IS NOT NULL AND i.FinalTermScore   NOT IN (0, 10)))
    )
    BEGIN
        RAISERROR('Môn Đạt/Không đạt: giá trị 3 cột điểm phải là 0 (KĐ), 10 (Đ), hoặc NULL.', 16, 1);
        ROLLBACK TRANSACTION;
    END
END;
GO

-- [Note] Maintain snapshot StudentAverage — TB tất cả môn của HS, loại trừ PassFail.
-- Trigger gom các (StudentID, Semester, AcademicYear) tuple bị ảnh hưởng từ inserted ∪ deleted,
-- recompute AVG(AverageScore) chỉ trên môn GradeType='Score', rồi MERGE vào StudentAverage.
-- Dùng MERGE thay vì UPSERT 2 bước: handle batch (nhiều HS / nhiều môn trong cùng statement) atomic.
CREATE TRIGGER TRG_Score_UpdateStudentAverage
ON Score
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    --  Tập (Student, Semester, Year) bị ảnh hưởng
    DECLARE @Affected TABLE (
        StudentID    VARCHAR(10)  NOT NULL,
        Semester     NVARCHAR(50) NOT NULL,
        AcademicYear NVARCHAR(50) NOT NULL,
        PRIMARY KEY (StudentID, Semester, AcademicYear)
    );

    INSERT INTO @Affected (StudentID, Semester, AcademicYear)
    SELECT StudentID, Semester, AcademicYear FROM inserted
    UNION
    SELECT StudentID, Semester, AcademicYear FROM deleted;

    -- Recompute AVG cho từng tuple (chỉ tính môn GradeType='Score')
    ;WITH Agg AS (
        SELECT  a.StudentID,
                a.Semester,
                a.AcademicYear,
                AVG(CASE WHEN s.GradeType = 'Score' THEN sc.AverageScore END) AS OverallAvg,
                COUNT(CASE WHEN s.GradeType = 'Score' THEN 1 END)             AS Cnt
        FROM    @Affected a
        LEFT JOIN Score   sc ON sc.StudentID    = a.StudentID
                            AND sc.Semester     = a.Semester
                            AND sc.AcademicYear = a.AcademicYear
        LEFT JOIN Subject s  ON s.SubjectID = sc.SubjectID
        GROUP BY a.StudentID, a.Semester, a.AcademicYear
    )
    MERGE StudentAverage AS tgt
    USING Agg            AS src
       ON tgt.StudentID    = src.StudentID
      AND tgt.Semester     = src.Semester
      AND tgt.AcademicYear = src.AcademicYear
    WHEN MATCHED THEN
        UPDATE SET OverallAverage = src.OverallAvg,
                   SubjectCount   = src.Cnt,
                   UpdatedAt      = GETDATE()
    WHEN NOT MATCHED BY TARGET AND src.Cnt > 0 THEN
        INSERT (StudentID, Semester, AcademicYear, OverallAverage, SubjectCount)
        VALUES (src.StudentID, src.Semester, src.AcademicYear, src.OverallAvg, src.Cnt);
END;
GO

-- [Note] Smart delete cho Subject
-- - HARD DELETE nếu môn chưa có dữ liệu liên quan (Score / TA / SubjectReport / Employee.Specialization)
-- - SOFT DELETE (IsDeleted=1) nếu môn đã có lịch sử ⇒ giữ nguyên tham chiếu cho dữ liệu cũ
-- UI gọi `DELETE FROM Subject WHERE SubjectID=?` và để trigger tự quyết định
CREATE TRIGGER TRG_Subject_SmartDelete
ON Subject
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;

    -- Tập hợp các môn còn bị tham chiếu ⇒ soft delete
    DECLARE @HasRefs TABLE (SubjectID INT PRIMARY KEY);
    INSERT INTO @HasRefs(SubjectID)
    SELECT DISTINCT d.SubjectID
    FROM   deleted d
    WHERE  EXISTS (SELECT 1 FROM Score              x  WHERE x.SubjectID              = d.SubjectID)
        OR EXISTS (SELECT 1 FROM TeachingAssignment x  WHERE x.SubjectID              = d.SubjectID)
        OR EXISTS (SELECT 1 FROM SubjectReport      x  WHERE x.SubjectID              = d.SubjectID)
        OR EXISTS (SELECT 1 FROM Employee           x  WHERE x.Specialization = d.SubjectID);

    -- Soft delete
    UPDATE s
    SET    s.IsDeleted = 1
    FROM   Subject s
    JOIN   @HasRefs h ON s.SubjectID = h.SubjectID
    WHERE  s.IsDeleted = 0;   -- tránh update thừa nếu đã soft-delete trước đó

    -- Hard delete (không có tham chiếu)
    DELETE s
    FROM   Subject  s
    JOIN   deleted  d ON s.SubjectID = d.SubjectID
    LEFT JOIN @HasRefs h ON s.SubjectID = h.SubjectID
    WHERE  h.SubjectID IS NULL;
END;
GO

-- MOCK DATA  (đã điều chỉnh để khớp schema mới)
-- =============================================================

-- Roles
INSERT INTO Role (RoleName) VALUES
(N'Học sinh'), (N'IT Admin'), (N'Hiệu trưởng'), (N'GVBM'), (N'GVCN'), (N'Giáo vụ');

-- Status (lookup table)
INSERT INTO Status (StatusName) VALUES
('Pending'), ('Accepted'), ('Rejected'), ('Executed');

-- Parameters
INSERT INTO Parameter (ParameterName, Value) VALUES
('MinAge', 15),
('MaxAge', 20),
('MaxClassSize', 40),
('NumPassingGrade', 5),
('RegularScoreCoefficient', 1),
('MidtermScoreCoefficient', 2),
('FinalScoreCoefficient', 3);

-- Subjects  (IDs 1..9)
INSERT INTO Subject (SubjectName, GradeType, IsDeleted) VALUES
(N'Toán học',            'Score',    0),  -- 1
(N'Vật Lý',              'Score',    0),  -- 2
(N'Hóa học',             'Score',    0),  -- 3
(N'Sinh học',            'Score',    0),  -- 4
(N'Lịch sử',             'Score',    0),  -- 5
(N'Địa lý',              'Score',    0),  -- 6
(N'Ngữ Văn',             'Score',    0),  -- 7
(N'Giáo dục công dân',   'PassFail', 0),  -- 8
(N'Giáo dục thể chất',   'PassFail', 0);  -- 9

-- Accounts 
INSERT INTO Account (RoleID, Username, PasswordHash, IsRequiredChangePassword, IsActive) VALUES
(2, 'admin_system',  '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', 1, 1),
(3, 'ht_daott',      '3549f22fb8622a6d216ef2dcd592e04ed1f1e604cef032d7e5c425e8e72a878e', 1, 1),
(6, 'gv_vulv',       '10176e7b7b24d317acfcf8d2064cfd2f24e154f7b5a96603077d5ef813d6a6b6', 1, 1),
(5, 'gv_canpv',      'cde383eee8ee7a4400adf7a15f716f179a2eb97646b37e089eb8d6d04e663416', 1, 1),
(5, 'gv_nguht',      'cde383eee8ee7a4400adf7a15f716f179a2eb97646b37e089eb8d6d04e663416', 1, 1),
(5, 'gv_khoavv',     'cde383eee8ee7a4400adf7a15f716f179a2eb97646b37e089eb8d6d04e663416', 1, 1),
(5, 'gv_hoadt',      'cde383eee8ee7a4400adf7a15f716f179a2eb97646b37e089eb8d6d04e663416', 1, 1),
(5, 'gv_sinhbv',     'cde383eee8ee7a4400adf7a15f716f179a2eb97646b37e089eb8d6d04e663416', 1, 1),
(5, 'gv_sudt',       'cde383eee8ee7a4400adf7a15f716f179a2eb97646b37e089eb8d6d04e663416', 1, 1),
(5, 'gv_diahv',      'cde383eee8ee7a4400adf7a15f716f179a2eb97646b37e089eb8d6d04e663416', 1, 1),
(5, 'gv_anhnt',      'cde383eee8ee7a4400adf7a15f716f179a2eb97646b37e089eb8d6d04e663416', 1, 1),
(5, 'gv_bachdv',     'cde383eee8ee7a4400adf7a15f716f179a2eb97646b37e089eb8d6d04e663416', 1, 1),
(4, 'gv_duclt',      'cde383eee8ee7a4400adf7a15f716f179a2eb97646b37e089eb8d6d04e663416', 1, 1),
(4, 'gv_lucmv',      'cde383eee8ee7a4400adf7a15f716f179a2eb97646b37e089eb8d6d04e663416', 1, 1);
-- (40 student account rows — unchanged from original)
INSERT INTO Account (RoleID, Username, PasswordHash, IsRequiredChangePassword, IsActive) VALUES
(1, 'hs_nguyenvanan100110',     '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_tranthibinh150210',     '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_lehoangcuong200310',    '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_phamducduy250410',      '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_vothiyen300510',        '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_danghaidang050610',     '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_buingocgiau100710',     '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_dominhhieu150810',      '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_honhatkhang200910',     '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_lygialinh251010',       '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_maiphuongmai011110',    '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_ngotiennam051210',      '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_phanbaoNgoc120110',     '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_hoangminhquan180210',   '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_vuquocson220310',       '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_trinhanhtu280410',      '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_daothanhuy020510',      '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_nguyenhaiha080610',     '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_tranvanloc140710',      '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_lethithu200810',        '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_phamcongvinh100109',    '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_vongoctram150209',      '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_dangquanghuy220309',    '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_buituananh280409',      '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_dothanhthao050509',     '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_hovanbinh120609',       '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_lytuyetmai180709',      '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_maixuankien250809',     '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_ngothithuy010909',      '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_phanvietdung101009',    '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_hoangkimngan151109',    '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_vuminhkhoi201209',      '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_trinhgiahuy050109',     '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_daothuhuong120209',     '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_nguyentrongnhan180309', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_tranthimong100108',     '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_ledinhphuc150208',      '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_phamthanhdat220308',    '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_vothanhphong280408',    '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1),
(1, 'hs_dangthiminh050508',     '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 1, 1);

-- Employees (Specialization → Specialization)
-- Admin / Hiệu trưởng / Giáo vụ → NULL (không phải giáo viên)
INSERT INTO Employee (AccountID, FullName, Gender, Specialization, Email, HireDate, HometownAddress, PhoneNumber, NationalID, Status) VALUES
(1,  N'Nguyễn Văn Trị', N'Nam', NULL, 'admin@gmail.mock',         '2020-01-15', N'Hà Nội',    '0901234560', '001090123450', N'Active'),
(2,  N'Trần Thị Đạo',   N'Nữ',  NULL, 'hieutruong@gmail.mock',    '2010-05-15', N'TP.HCM',    '0901234561', '079190123451', N'Active'),
(3,  N'Lê Văn Vụ',      N'Nam', NULL, 'giaovu@gmail.mock',        '2015-08-20', N'Đà Nẵng',   '0901234562', '048090123452', N'Active'),
(4,  N'Phạm Văn Cán',   N'Nam', 1,    'gvcn10a1@gmail.mock',      '2018-09-01', N'TP.HCM',    '0901234563', '079090123453', N'Active'),
(5,  N'Hoàng Thị Ngữ',  N'Nữ',  7,    'gvcn10a2@gmail.mock',      '2019-09-01', N'Cần Thơ',   '0901234564', '092190123454', N'Active'),
(6,  N'Vũ Văn Khoa',    N'Nam', 2,    'gvcn10a3@gmail.mock',      '2017-09-01', N'Hải Phòng', '0901234565', '031090123455', N'Active'),
(7,  N'Đặng Thị Hóa',   N'Nữ',  3,    'gvcn10a4@gmail.mock',      '2020-09-01', N'Huế',       '0901234566', '046190123456', N'Active'),
(8,  N'Bùi Văn Sinh',   N'Nam', 4,    'gvcn11a1@gmail.mock',      '2021-09-01', N'Đồng Nai',  '0901234567', '075090123457', N'Active'),
(9,  N'Đỗ Thị Sử',      N'Nữ',  5,    'gvcn11a2@gmail.mock',      '2016-09-01', N'Bình Dương','0901234568', '074190123458', N'Active'),
(10, N'Hồ Văn Địa',     N'Nam', 6,    'gvcn11a3@gmail.mock',      '2015-09-01', N'Long An',   '0901234569', '080090123459', N'Active'),
(11, N'Ngô Thị Anh',    N'Nữ',  7,    'gvcn12a1@gmail.mock',      '2018-09-01', N'Tiền Giang','0901234570', '082190123460', N'Active'),
(12, N'Dương Văn Bách', N'Nam', 1,    'gvcn12a2@gmail.mock',      '2019-09-01', N'Bến Tre',   '0901234571', '083090123461', N'Active'),
(13, N'Lý Thị Đức',     N'Nữ',  8,    'gvbm_dao_duc@gmail.mock',  '2022-09-01', N'Vũng Tàu',  '0901234572', '077190123462', N'Active'),
(14, N'Mai Văn Lực',    N'Nam', 9,    'gvbm_the_duc@gmail.mock',  '2021-09-01', N'Tây Ninh',  '0901234573', '072090123463', N'Active');

-- Classes (AcademicYear default = '2025-2026')
INSERT INTO Class (ClassName, Grade, HomeroomTeacherID) VALUES
('10A1', 10, 4),  ('10A2', 10, 5),  ('10A3', 10, 6),  ('10A4', 10, 7),
('11A1', 11, 8),  ('11A2', 11, 9),  ('11A3', 11, 10),
('12A1', 12, 11), ('12A2', 12, 12);

-- Students  (giữ nguyên bản gốc)
INSERT INTO Student (AccountID, FullName, Gender, DateOfBirth, PhoneNumber, Email, Address, FamilyBackground, GuardianName, GuardianPhoneNumber, Status) VALUES
(15, N'Nguyễn Văn An',     N'Nam', '2010-01-10', '0912345101', 'nguyenvanan100110@gmail.mock',     N'Quận 1, TP.HCM',    N'Bình thường', N'Nguyễn Văn Ánh',  '0987654101', N'Active'),
(16, N'Trần Thị Bình',     N'Nữ',  '2010-02-15', '0912345102', 'tranthibinh150210@gmail.mock',     N'Quận 3, TP.HCM',    N'Bình thường', N'Trần Văn Biên',   '0987654102', N'Active'),
(17, N'Lê Hoàng Cường',    N'Nam', '2010-03-20', '0912345103', 'lehoangcuong200310@gmail.mock',    N'Quận 4, TP.HCM',    N'Khó khăn',    N'Lê Thị Cúc',      '0987654103', N'Active'),
(18, N'Phạm Đức Duy',      N'Nam', '2010-04-25', '0912345104', 'phamducduy250410@gmail.mock',      N'Quận 5, TP.HCM',    N'Bình thường', N'Phạm Văn Dũng',   '0987654104', N'Active'),
(19, N'Võ Thị Yến',        N'Nữ',  '2010-05-30', '0912345105', 'vothiyen300510@gmail.mock',        N'Quận 7, TP.HCM',    N'Bình thường', N'Võ Văn Yên',      '0987654105', N'Active'),
(20, N'Đặng Hải Đăng',     N'Nam', '2010-06-05', '0912345106', 'danghaidang050610@gmail.mock',     N'Quận 8, TP.HCM',    N'Bình thường', N'Đặng Thị Đào',    '0987654106', N'Active'),
(21, N'Bùi Ngọc Giàu',     N'Nữ',  '2010-07-10', '0912345107', 'buingocgiau100710@gmail.mock',     N'Quận 10, TP.HCM',   N'Bình thường', N'Bùi Văn Gấm',     '0987654107', N'Active'),
(22, N'Đỗ Minh Hiếu',      N'Nam', '2010-08-15', '0912345108', 'dominhhieu150810@gmail.mock',      N'Quận 11, TP.HCM',   N'Bình thường', N'Đỗ Thị Hiền',     '0987654108', N'Active'),
(23, N'Hồ Nhật Khang',     N'Nam', '2010-09-20', '0912345109', 'honhatkhang200910@gmail.mock',     N'Tân Bình, TP.HCM',  N'Bình thường', N'Hồ Văn Khánh',    '0987654109', N'Active'),
(24, N'Lý Gia Linh',       N'Nữ',  '2010-10-25', '0912345110', 'lygialinh251010@gmail.mock',       N'Gò Vấp, TP.HCM',    N'Khó khăn',    N'Lý Thị Lan',      '0987654110', N'Active'),
(25, N'Mai Phương Mai',    N'Nữ',  '2010-11-01', '0912345111', 'maiphuongmai011110@gmail.mock',    N'Tân Phú, TP.HCM',   N'Bình thường', N'Mai Văn Mẫn',     '0987654111', N'Active'),
(26, N'Ngô Tiến Nam',      N'Nam', '2010-12-05', '0912345112', 'ngotiennam051210@gmail.mock',      N'Phú Nhuận, TP.HCM', N'Bình thường', N'Ngô Thị Nga',     '0987654112', N'Active'),
(27, N'Phan Bảo Ngọc',     N'Nữ',  '2010-01-12', '0912345113', 'phanbaongoc120110@gmail.mock',     N'Bình Thạnh, TP.HCM',N'Bình thường', N'Phan Văn Nghĩa',  '0987654113', N'Active'),
(28, N'Hoàng Minh Quân',   N'Nam', '2010-02-18', '0912345114', 'hoangminhquan180210@gmail.mock',   N'Bình Tân, TP.HCM',  N'Bình thường', N'Hoàng Thị Quyên', '0987654114', N'Active'),
(29, N'Vũ Quốc Sơn',       N'Nam', '2010-03-22', '0912345115', 'vuquocson220310@gmail.mock',       N'Thủ Đức, TP.HCM',   N'Bình thường', N'Vũ Văn Sáng',     '0987654115', N'Active'),
(30, N'Trịnh Anh Tú',      N'Nam', '2010-04-28', '0912345116', 'trinhanhtu280410@gmail.mock',      N'Nhà Bè, TP.HCM',    N'Khó khăn',    N'Trịnh Thị Thủy',  '0987654116', N'Active'),
(31, N'Đào Thanh Uyển',    N'Nữ',  '2010-05-02', '0912345117', 'daothanhuyen020510@gmail.mock',    N'Hóc Môn, TP.HCM',   N'Bình thường', N'Đào Văn Uy',      '0987654117', N'Active'),
(32, N'Nguyễn Hải Hà',     N'Nữ',  '2010-06-08', '0912345118', 'nguyenhaiha080610@gmail.mock',     N'Bình Chánh, TP.HCM',N'Bình thường', N'Nguyễn Văn Hùng', '0987654118', N'Active'),
(33, N'Trần Văn Lộc',      N'Nam', '2010-07-14', '0912345119', 'tranvanloc140710@gmail.mock',      N'Quận 1, TP.HCM',    N'Bình thường', N'Trần Thị Liên',   '0987654119', N'Active'),
(34, N'Lê Thị Thu',        N'Nữ',  '2010-08-20', '0912345120', 'lethithu200810@gmail.mock',        N'Quận 3, TP.HCM',    N'Bình thường', N'Lê Văn Thắng',    '0987654120', N'Active'),
(35, N'Phạm Công Vinh',    N'Nam', '2009-01-10', '0912345121', 'phamcongvinh100109@gmail.mock',    N'Quận 4, TP.HCM',    N'Bình thường', N'Phạm Thị Vân',    '0987654121', N'Active'),
(36, N'Võ Ngọc Trâm',      N'Nữ',  '2009-02-15', '0912345122', 'vongoctram150209@gmail.mock',      N'Quận 5, TP.HCM',    N'Khó khăn',    N'Võ Văn Triết',    '0987654122', N'Active'),
(37, N'Đặng Quang Huy',    N'Nam', '2009-03-22', '0912345123', 'dangquanghuy220309@gmail.mock',    N'Quận 7, TP.HCM',    N'Bình thường', N'Đặng Thị Hoa',    '0987654123', N'Active'),
(38, N'Bùi Tuấn Anh',      N'Nam', '2009-04-28', '0912345124', 'buituananh280409@gmail.mock',      N'Quận 8, TP.HCM',    N'Bình thường', N'Bùi Văn Tú',      '0987654124', N'Active'),
(39, N'Đỗ Thanh Thảo',     N'Nữ',  '2009-05-05', '0912345125', 'dothanhthao050509@gmail.mock',     N'Quận 10, TP.HCM',   N'Bình thường', N'Đỗ Thị Tâm',      '0987654125', N'Active'),
(40, N'Hồ Văn Bình',       N'Nam', '2009-06-12', '0912345126', 'hovanbinh120609@gmail.mock',       N'Quận 11, TP.HCM',   N'Khó khăn',    N'Hồ Thị Bích',     '0987654126', N'Active'),
(41, N'Lý Tuyết Mai',      N'Nữ',  '2009-07-18', '0912345127', 'lytuyetmai180709@gmail.mock',      N'Tân Bình, TP.HCM',  N'Bình thường', N'Lý Văn Mười',     '0987654127', N'Active'),
(42, N'Mai Xuân Kiên',     N'Nam', '2009-08-25', '0912345128', 'maixuankien250809@gmail.mock',     N'Gò Vấp, TP.HCM',    N'Bình thường', N'Mai Thị Kim',     '0987654128', N'Active'),
(43, N'Ngô Thị Thủy',      N'Nữ',  '2009-09-01', '0912345129', 'ngothithuy010909@gmail.mock',      N'Tân Phú, TP.HCM',   N'Bình thường', N'Ngô Văn Thái',    '0987654129', N'Active'),
(44, N'Phan Việt Dũng',    N'Nam', '2009-10-10', '0912345130', 'phanvietdung101009@gmail.mock',    N'Phú Nhuận, TP.HCM', N'Bình thường', N'Phan Thị Diệp',   '0987654130', N'Active'),
(45, N'Hoàng Kim Ngân',    N'Nữ',  '2009-11-15', '0912345131', 'hoangkimngan151109@gmail.mock',    N'Bình Thạnh, TP.HCM',N'Bình thường', N'Hoàng Văn Ngữ',   '0987654131', N'Active'),
(46, N'Vũ Minh Khôi',      N'Nam', '2009-12-20', '0912345132', 'vuminhkhoi201209@gmail.mock',      N'Bình Tân, TP.HCM',  N'Khó khăn',    N'Vũ Thị Khuyên',   '0987654132', N'Active'),
(47, N'Trịnh Gia Huy',     N'Nam', '2009-01-05', '0912345133', 'trinhgiahuy050109@gmail.mock',     N'Thủ Đức, TP.HCM',   N'Bình thường', N'Trịnh Văn Hoàng', '0987654133', N'Active'),
(48, N'Đào Thu Hương',     N'Nữ',  '2009-02-12', '0912345134', 'daothuhuong120209@gmail.mock',     N'Nhà Bè, TP.HCM',    N'Bình thường', N'Đào Thị Hạnh',    '0987654134', N'Active'),
(49, N'Nguyễn Trọng Nhân', N'Nam', '2009-03-18', '0912345135', 'nguyentrongnhan180309@gmail.mock', N'Hóc Môn, TP.HCM',   N'Bình thường', N'Nguyễn Văn Nghĩa','0987654135', N'Active'),
(50, N'Trần Thị Mộng',     N'Nữ',  '2008-01-10', '0912345136', 'tranthimong100108@gmail.mock',     N'Bình Chánh, TP.HCM',N'Bình thường', N'Trần Văn Mẫn',    '0987654136', N'Active'),
(51, N'Lê Đình Phúc',      N'Nam', '2008-02-15', '0912345137', 'ledinhphuc150208@gmail.mock',      N'Quận 1, TP.HCM',    N'Bình thường', N'Lê Thị Phương',   '0987654137', N'Active'),
(52, N'Phạm Thành Đạt',    N'Nam', '2008-03-22', '0912345138', 'phamthanhdat220308@gmail.mock',    N'Quận 3, TP.HCM',    N'Khó khăn',    N'Phạm Văn Đồng',   '0987654138', N'Active'),
(53, N'Võ Thanh Phong',    N'Nam', '2008-04-28', '0912345139', 'vothanhphong280408@gmail.mock',    N'Quận 4, TP.HCM',    N'Bình thường', N'Võ Thị Phụng',    '0987654139', N'Active'),
(54, N'Đặng Thị Minh',     N'Nữ',  '2008-05-05', '0912345140', 'dangthiminh050508@gmail.mock',     N'Quận 5, TP.HCM',    N'Bình thường', N'Đặng Văn Mạnh',   '0987654140', N'Active');

-- ClassPlacement  (đã bổ sung AcademicYear + EffectiveFrom; EffectiveTo = NULL = hiện tại)
INSERT INTO ClassPlacement (StudentID, ClassID, AcademicYear, EffectiveFrom) VALUES
('hs250001', 1, '2025-2026', '2025-09-05'), ('hs250002', 1, '2025-2026', '2025-09-05'),
('hs250003', 1, '2025-2026', '2025-09-05'), ('hs250004', 1, '2025-2026', '2025-09-05'),
('hs250005', 1, '2025-2026', '2025-09-05'),
('hs250006', 2, '2025-2026', '2025-09-05'), ('hs250007', 2, '2025-2026', '2025-09-05'),
('hs250008', 2, '2025-2026', '2025-09-05'), ('hs250009', 2, '2025-2026', '2025-09-05'),
('hs250010', 2, '2025-2026', '2025-09-05'),
('hs250011', 3, '2025-2026', '2025-09-05'), ('hs250012', 3, '2025-2026', '2025-09-05'),
('hs250013', 3, '2025-2026', '2025-09-05'), ('hs250014', 3, '2025-2026', '2025-09-05'),
('hs250015', 3, '2025-2026', '2025-09-05'),
('hs250016', 4, '2025-2026', '2025-09-05'), ('hs250017', 4, '2025-2026', '2025-09-05'),
('hs250018', 4, '2025-2026', '2025-09-05'), ('hs250019', 4, '2025-2026', '2025-09-05'),
('hs250020', 4, '2025-2026', '2025-09-05'),
('hs250021', 5, '2025-2026', '2025-09-05'), ('hs250022', 5, '2025-2026', '2025-09-05'),
('hs250023', 5, '2025-2026', '2025-09-05'), ('hs250024', 5, '2025-2026', '2025-09-05'),
('hs250025', 5, '2025-2026', '2025-09-05'),
('hs250026', 6, '2025-2026', '2025-09-05'), ('hs250027', 6, '2025-2026', '2025-09-05'),
('hs250028', 6, '2025-2026', '2025-09-05'), ('hs250029', 6, '2025-2026', '2025-09-05'),
('hs250030', 6, '2025-2026', '2025-09-05'),
('hs250031', 7, '2025-2026', '2025-09-05'), ('hs250032', 7, '2025-2026', '2025-09-05'),
('hs250033', 7, '2025-2026', '2025-09-05'), ('hs250034', 7, '2025-2026', '2025-09-05'),
('hs250035', 7, '2025-2026', '2025-09-05'),
('hs250036', 8, '2025-2026', '2025-09-05'), ('hs250037', 8, '2025-2026', '2025-09-05'),
('hs250038', 8, '2025-2026', '2025-09-05'),
('hs250039', 9, '2025-2026', '2025-09-05'), ('hs250040', 9, '2025-2026', '2025-09-05');

-- TeachingAssignment  (Semester + AcademicYear dùng default)
INSERT INTO TeachingAssignment (EmployeeID, ClassID, SubjectID) VALUES
(4, 1, 1), (5, 2, 7), (6, 3, 2),
(13, 1, 8), (14, 5, 9),
(8, 5, 4), (11, 8, 7), (12, 9, 1);

-- Scores  (AverageScore tự tính, không cần INSERT)
-- Môn tính điểm
INSERT INTO Score (StudentID, SubjectID, RegularTestScore, MidTermScore, FinalTermScore) VALUES
('hs250006', 7, 7.0, 8.0, 8.5),
('hs250011', 2, 9.0, 9.5, 9.0),
('hs250021', 4, 8.0, 7.0, 8.0),
('hs250036', 7, 7.5, 8.0, 8.5),
('hs250039', 1, 9.5, 9.0, 9.5);
-- Môn Đạt/Không đạt (PassFail) — encode Đ=10, KĐ=0; trigger StudentAverage tự loại trừ qua GradeType filter
INSERT INTO Score (StudentID, SubjectID, RegularTestScore, MidTermScore, FinalTermScore) VALUES
('hs250022', 9, 10, 10, 10);

-- Mock điểm chi tiết cho lớp 10A1 (test báo cáo của GVCN Phạm Văn Cán)
-- Môn tính điểm (SubjectID 1..7)
INSERT INTO Score (StudentID, SubjectID, RegularTestScore, MidTermScore, FinalTermScore) VALUES
-- hs250001 (Giỏi)
('hs250001', 1, 9.0, 9.0, 9.5), ('hs250001', 2, 8.5, 9.0, 9.0), ('hs250001', 3, 9.0, 8.5, 9.0),
('hs250001', 4, 9.5, 9.5, 9.5), ('hs250001', 5, 8.0, 8.5, 8.0), ('hs250001', 6, 9.0, 9.0, 9.0),
('hs250001', 7, 8.5, 8.5, 8.5),
-- hs250002 (Khá)
('hs250002', 1, 6.0, 6.5, 6.0), ('hs250002', 2, 7.0, 7.0, 7.5), ('hs250002', 3, 6.5, 6.5, 6.0),
('hs250002', 4, 7.5, 8.0, 7.5), ('hs250002', 5, 6.0, 6.0, 6.5), ('hs250002', 6, 6.5, 7.0, 6.5),
('hs250002', 7, 7.0, 6.5, 7.0),
-- hs250003 (Trung Bình)
('hs250003', 1, 5.0, 5.5, 5.0), ('hs250003', 2, 5.5, 5.0, 5.5), ('hs250003', 3, 5.0, 5.0, 5.0),
('hs250003', 4, 6.0, 5.5, 6.0), ('hs250003', 5, 5.0, 6.0, 5.5), ('hs250003', 6, 5.5, 5.5, 5.5),
('hs250003', 7, 5.5, 5.0, 5.5),
-- hs250004 (Rớt Toán)
('hs250004', 1, 4.0, 4.5, 4.0),
('hs250004', 2, 7.0, 7.5, 7.0), ('hs250004', 3, 6.5, 7.0, 6.5),
('hs250004', 4, 8.0, 8.5, 8.0), ('hs250004', 5, 7.5, 7.0, 7.5), ('hs250004', 6, 6.0, 6.5, 6.0),
('hs250004', 7, 6.5, 6.0, 6.5),
-- hs250005 (Rớt 3 môn)
('hs250005', 1, 3.0, 3.5, 3.0), ('hs250005', 2, 4.0, 4.5, 4.0), ('hs250005', 3, 3.5, 4.0, 3.5),
('hs250005', 4, 6.0, 6.5, 6.0), ('hs250005', 5, 7.0, 7.5, 7.0), ('hs250005', 6, 5.5, 6.0, 5.5),
('hs250005', 7, 6.5, 6.5, 6.5);

-- Môn Đạt/Không đạt (SubjectID 8 = GDCD, 9 = GDTC) — encode Đ=10; trigger StudentAverage tự loại trừ qua GradeType filter
INSERT INTO Score (StudentID, SubjectID, RegularTestScore, MidTermScore, FinalTermScore) VALUES
('hs250001', 8, 10, 10, 10), ('hs250001', 9, 10, 10, 10),
('hs250002', 8, 10, 10, 10), ('hs250002', 9, 10, 10, 10),
('hs250003', 8, 10, 10, 10), ('hs250003', 9, 10, 10, 10),
('hs250004', 8, 10, 10, 10), ('hs250004', 9, 10, 10, 10),
('hs250005', 8, 10, 10, 10), ('hs250005', 9, 10, 10, 10);

-- Applications
INSERT INTO Application (StudentID, CreatedByTeacherID, NewClassID, RequestType, Reason, FeedbackNote, StatusID, RespondedAt) VALUES
('hs250001', 4,  2,    'ClassTransfer', N'Chuyển sang lớp 10A2 để học cùng anh em họ',     NULL,                              1, NULL),
('hs250015', 6,  NULL, 'DropOut',       N'Gia đình chuyển công tác ra nước ngoài',         N'Đã xác nhận với phụ huynh',      4, '2024-04-20 09:30:00'),
('hs250025', 8,  6,    'ClassTransfer', N'Không theo kịp chương trình nâng cao',           N'Chuyển sang lớp 11A2',           4, '2024-05-15 14:00:00'),
('hs250040', 12, NULL, 'DropOut',       N'Lý do sức khỏe',                                 NULL,                              1, NULL);

INSERT INTO ClassReport (ClassID, Semester, AcademicYear, TotalStudents, IsLocked, CreatedByTeacherID, CreatedAt)
VALUES
(1, N'Học kỳ 1', '2025-2026', 5, 1, 4, GETDATE()),  -- Lớp 10A1 (Sĩ số: 5, GVCN: Phạm Văn Cán)
(2, N'Học kỳ 1', '2025-2026', 5, 1, 5, GETDATE()),  -- Lớp 10A2 (Sĩ số: 5, GVCN: Hoàng Thị Ngữ)
(3, N'Học kỳ 1', '2025-2026', 5, 1, 6, GETDATE()),  -- Lớp 10A3 (Sĩ số: 5, GVCN: Vũ Văn Khoa)
(4, N'Học kỳ 1', '2025-2026', 5, 1, 7, GETDATE()),  -- Lớp 10A4 (Sĩ số: 5, GVCN: Đặng Thị Hóa)
(5, N'Học kỳ 1', '2025-2026', 5, 1, 8, GETDATE()),  -- Lớp 11A1 (Sĩ số: 5, GVCN: Bùi Văn Sinh)
(6, N'Học kỳ 1', '2025-2026', 5, 1, 9, GETDATE()),  -- Lớp 11A2 (Sĩ số: 5, GVCN: Đỗ Thị Sử)
(7, N'Học kỳ 1', '2025-2026', 5, 1, 10, GETDATE()), -- Lớp 11A3 (Sĩ số: 5, GVCN: Hồ Văn Địa)
(8, N'Học kỳ 1', '2025-2026', 3, 1, 11, GETDATE()), -- Lớp 12A1 (Sĩ số: 3, GVCN: Ngô Thị Anh)
(9, N'Học kỳ 1', '2025-2026', 2, 1, 12, GETDATE()); -- Lớp 12A2 (Sĩ số: 2, GVCN: Dương Văn Bách)