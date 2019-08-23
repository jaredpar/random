/* SELECT * FROM dbo.BuildCloneTime WHERE DefinitionId = 15 and BuildStartTime > '8/14	/2019' and maxduration > '00:10:00' */

/* SELECT * FROM dbo.JobCloneTime WHERE BuildId = 311446 */

/*
SELECT COUNT(*), CAST(CAST(AVG(CAST(CAST(Duration as datetime) as float)) as datetime) as time) FROM dbo.JobCloneTime 
where buildstarttime > '2019/08/19' AND Build
*/
/*
SELECT TOP 10 DefinitionId, Count(*) as JobCount
FROM dbo.JobCloneTime 
WHERE buildstarttime > '2019/08/19'
GROUP BY DefinitionId
OrdER BY JobCount DESC

*/

SELECT DefinitionId, AVG(FetchSize) AS FetchSize
From JobCloneTime 
WHERE BuildStartTime > '2019/08/20'
GROUP BY DefinitionId
ORDER BY FetchSize DESC

