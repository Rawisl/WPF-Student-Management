
-- Tables initialization ----------------------------------------
CREATE TABLE Parameter (
    ParameterID INT CONSTRAINT PK_Parameter PRIMARY KEY,
    ParameterName VARCHAR(100) NOT NULL,
    Value DECIMAL(10,2) NOT NULL
);

CREATE TABLE Role (
    RoleID INT CONSTRAINT PK_Role PRIMARY KEY,
    RoleName NVARCHAR(100) NOT NULL,
    CONSTRAINT CHK_Role_RoleName CHECK (RoleName IN (N'Học sinh', N'IT Admin', N'Hiệu trưởng', N'GVBM', N'GVCN', N'Giáo vụ'))
);

-- Subject Table
CREATE TABLE Subject (
    SubjectID INT CONSTRAINT PK_Subject PRIMARY KEY,
    SubjectName NVARCHAR(100) NOT NULL,
    GradeType VARCHAR(50) NOT NULL, 
    IsDeleted BIT DEFAULT 0, -- Changed BOOLEAN to BIT and FALSE to 0
    CONSTRAINT CHK_Subject_SubjectName CHECK (SubjectName IN (N'Toán', N'Lý', N'Hóa', N'Sinh', N'Sử', N'Địa', N'Văn', N'Đạo Đức', N'Thể Dục'))
);

CREATE TABLE Account (
    AccountID INT CONSTRAINT PK_Account PRIMARY KEY,
    RoleID INT NOT NULL,
    Username VARCHAR(100) CONSTRAINT UQ_Account_Username UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    IsRequiredChangePassword BIT DEFAULT 1,
    IsActive BIT DEFAULT 1,
    CONSTRAINT FK_Account_Role FOREIGN KEY (RoleID) REFERENCES Role(RoleID)
);

CREATE TABLE Employee (
    EmployeeID INT CONSTRAINT PK_Employee PRIMARY KEY,
    AccountID INT CONSTRAINT UQ_Employee_Account UNIQUE NOT NULL,
    FullName NVARCHAR(255) NOT NULL,
    Gender NVARCHAR(50),
    Specialization NVARCHAR(255),
    Email VARCHAR(255) CONSTRAINT UQ_Employee_Email UNIQUE,
    HireDate DATE,
    HometownAddress NVARCHAR(255),
    PhoneNumber VARCHAR(20),
    NationalID VARCHAR(50) CONSTRAINT UQ_Employee_NationalID UNIQUE,
    Status NVARCHAR(50),
    CONSTRAINT FK_Employee_Account FOREIGN KEY (AccountID) REFERENCES Account(AccountID)
);

CREATE TABLE Student (
    StudentID INT CONSTRAINT PK_Student PRIMARY KEY,
    AccountID INT CONSTRAINT UQ_Student_Account UNIQUE NOT NULL,
    FullName NVARCHAR(255) NOT NULL,
    Gender NVARCHAR(50),
    DateOfBirth DATE,
    PhoneNumber VARCHAR(20),
    Email VARCHAR(255) CONSTRAINT UQ_Student_Email UNIQUE,
    Address NVARCHAR(255),
    FamilyBackground NVARCHAR(MAX),
    GuardianName NVARCHAR(255),
    GuardianPhoneNumber VARCHAR(20),
    Status NVARCHAR(50),
    CONSTRAINT FK_Student_Account FOREIGN KEY (AccountID) REFERENCES Account(AccountID)
);

CREATE TABLE Class (
    ClassID INT CONSTRAINT PK_Class PRIMARY KEY,
    ClassName NVARCHAR(50) NOT NULL,
    Grade INT NOT NULL,
    ClassSize INT DEFAULT 0,
    HomeroomTeacherID INT,
    CONSTRAINT CHK_Class_Grade CHECK (Grade IN (10, 11, 12)),
    CONSTRAINT FK_Class_Employee FOREIGN KEY (HomeroomTeacherID) REFERENCES Employee(EmployeeID)
);

CREATE TABLE ClassPlacement (
    StudentID INT,
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
    ScoreID INT CONSTRAINT PK_Score PRIMARY KEY,
    StudentID INT NOT NULL,
    SubjectID INT NOT NULL,
    RegularTestScore DECIMAL(5,2),
    MidTermScore DECIMAL(5,2),
    FinalTermScore DECIMAL(5,2),
    AverageScore DECIMAL(5,2),
    CONSTRAINT FK_Score_Student FOREIGN KEY (StudentID) REFERENCES Student(StudentID),
    CONSTRAINT FK_Score_Subject FOREIGN KEY (SubjectID) REFERENCES Subject(SubjectID)
);

CREATE TABLE Application (
    RequestID INT CONSTRAINT PK_Application PRIMARY KEY,
    StudentID INT NOT NULL,
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

GO


-- Triggers initialization ----------------------------------------

-- Trigger to verify Homeroom Teacher role is exactly GVCN
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

