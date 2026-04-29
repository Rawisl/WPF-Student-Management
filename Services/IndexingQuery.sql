-- Creates an index on FullName to make text searching  fast
CREATE NONCLUSTERED INDEX IX_Student_FullName ON Student(FullName);

-- Creates an index on ClassID to speed up the JOIN operations during class searches
CREATE NONCLUSTERED INDEX IX_ClassPlacement_ClassID ON ClassPlacement(ClassID);