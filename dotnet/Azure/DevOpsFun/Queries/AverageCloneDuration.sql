SELECT	
	CAST(CAST(AVG(CAST(CAST(MinDuration as datetime) as float)) as datetime) as time) As AverageMinDuration,
	CAST(CAST(AVG(CAST(CAST(MaxDuration as datetime) as float)) as datetime) as time) As AverageMaxDuration
FROM dbo.BuildCloneTime WHERE DefinitionId = 166