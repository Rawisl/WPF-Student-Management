-- Creates an index on FullName to make text searching  fast
CREATE NONCLUSTERED INDEX IX_Student_FullName ON Student(FullName);

-- Creates an index on ClassID to speed up the JOIN operations during class searches
CREATE NONCLUSTERED INDEX IX_ClassPlacement_ClassID ON ClassPlacement(ClassID);

-- Stored Procedure to get student average grade by IDs
GO
CREATE PROCEDURE usp_GetStudentGPA
    @StudentID VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    -- Already calculated by the system trigger.
    SELECT 
        CAST(AVG(AverageScore) AS DECIMAL(5, 2)) AS TotalGPA
    FROM Score
    WHERE StudentID = @StudentID;
END
GO