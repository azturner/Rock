-- =============================================
-- Author:		Kinyon, Mason
-- Create date: 1/27/2015
-- Description:	Used to generate the family datamart data.
-- =============================================
ALTER PROCEDURE [dbo].[_church_ccv_spDatamart_Family]
AS
BEGIN

	SET NOCOUNT ON;

	TRUNCATE TABLE _church_ccv_Datamart_Family;

	WITH CTE AS 
	(
		SELECT
			F.Id AS [FamilyID],
			F.Name,
			HH.Id,
			HH.FirstName,
			HH.NickName,
			HH.LastName,
			HH.LastName + ', ' + HH.NickName AS [FullName],
			HH.Gender,
			CS.Value AS [ConnectionStatus],
			MS.Value AS [MaritalStatus],
			FV.ValueAsDateTime AS [FirstVisitDate],
			FA.ValueAsDateTime AS [FirstActivity],
			dbo._church_ccv_ufnGetAge(HH.BirthDate) AS [Age],
			CASE 
				WHEN HH.IsEmailActive = 1 THEN HH.Email
				ELSE NULL
			END AS [Email],
			HP.NumberFormatted AS [HomePhone]
		FROM [Group] F
		INNER JOIN Person HH
			ON HH.Id = dbo._church_ccv_ufn_GetHeadOfHousehold(F.Id)
		LEFT OUTER JOIN DefinedValue CS
			ON CS.Id = HH.ConnectionStatusValueId
		LEFT OUTER JOIN DefinedValue MS
			ON MS.Id = HH.MaritalStatusValueId
		LEFT OUTER JOIN AttributeValue FV
			ON FV.EntityId = HH.Id AND FV.AttributeId = 717
		LEFT OUTER JOIN AttributeValue FA
			ON FA.EntityId = HH.Id AND FA.AttributeId = 1056
		LEFT OUTER JOIN PhoneNumber HP 
			ON HP.PersonId = HH.Id AND HP.NumberTypeValueId = 13
		WHERE F.GroupTypeId = 10
	),

	Giving AS
	(
		SELECT *
		FROM (
			SELECT FM.GroupId AS [familyid], 
				YEAR(FT.TransactionDateTime) AS [year],
				SUM(FTD.Amount) AS [total]
			FROM FinancialTransactionDetail FTD
			INNER JOIN FinancialTransaction FT ON FT.Id = FTD.TransactionId
			INNER JOIN FinancialAccount FA ON FA.Id = FTD.AccountId
			INNER JOIN PersonAlias PA ON PA.AliasPersonId = FT.AuthorizedPersonAliasId
			INNER JOIN GroupMember FM ON FM.PersonId = PA.PersonId
			WHERE FA.Id IN (745,498,609,690,708,727) AND YEAR(FT.TransactionDateTime) >= 2007
			GROUP BY FM.GroupId, YEAR(FT.TransactionDateTime)
		) AS s
		PIVOT
		(
			SUM([total])
			FOR [year] IN ([2015],[2014],[2013],[2012],[2011],[2010],[2009],[2008],[2007])
		) AS s
	)

	INSERT INTO _church_ccv_Datamart_Family
		([FamilyId],
		[FamilyName],
		[HHPersonId],
		[HHFirstName],
		[HHNickName],
		[HHLastName],
		[HHFullName],
		[HHGender],
		[HHMemberStatus],
		[HHMaritalStatus],
		[HHFirstVisit],
		[HHFirstActivity],
		[HHAge],
		[NeighborhoodId],
		[NeighborhoodName],
		[InNeighborhoodGroup],
		[IsEra],
		[NearestNeighborhoodGroupName],
		[NearestNeighborhoodGroupId],
		[IsServing],
		[Attendance16Week],
		[ConnectionStatus],
		[Email],
		[HomePhone],
		[AdultCount],
		[ChildCount],
		[LocationId],
		[Address],
		[City],
		[State],
		[Country],
		[PostalCode],
		[GeoPoint],
		[Latitude],
		[Longitude],
		[Campus],
		[AdultNames],
		[ChildNames],
		[2015Contrib],
		[2014Contrib],
		[2013Contrib],
		[2012Contrib],
		[2011Contrib],
		[2010Contrib],
		[2009Contrib],
		[2008Contrib],
		[2007Contrib],
		[Guid],
		[ForeignId])
SELECT
	F.Id AS [FamilyID],
	F.Name,
	HH.Id,
	HH.FirstName,
	HH.NickName,
	HH.LastName,
	HH.FullName,
	HH.Gender,
	HH.ConnectionStatus,
	HH.MaritalStatus,
	HH.FirstVisitDate,
	HH.FirstActivity,
	HH.Age,
	(SELECT TOP 1 Id
		FROM dbo.ufnGroup_GeofencingGroups(L.Id, 48)),
	(SELECT TOP 1 Name
		FROM dbo.ufnGroup_GeofencingGroups(L.Id, 48)),
		CASE
			WHEN EXISTS (SELECT NG.Id
				FROM GroupMember NGM
				LEFT OUTER JOIN [Group] NG
				ON NG.Id = NGM.GroupId 
				AND NG.GroupTypeId = 49
				WHERE NGM.PersonId IN(SELECT TOP 1 P.Id
					FROM Person P
					INNER JOIN GroupMember GM ON GM.PersonId = P.Id
					WHERE GM.GroupId = F.Id
					AND GM.GroupRoleId = 3
					ORDER BY P.Gender, P.BirthYear DESC)) THEN 1
		    ELSE 0
	END,
	(SELECT RegularAttendee
		FROM _church_ccv_Datamart_ERA ERA
		WHERE ERA.WeekendDate = dbo._church_ccv_ufnGetSaturdayDate(GETDATE())
			AND F.Id = ERA.FamilyId), --isera
	(SELECT TOP 1 G.Name
		FROM dbo._church_ccv_Datamart_NearestGroup NG
		INNER JOIN GroupLocation GL 
		ON GL.LocationId = NG.GroupLocationId
		INNER JOIN [Group] G 
		ON G.Id = GL.GroupId AND G.GroupTypeId = 49
		WHERE NG.FamilyLocationId = L.Id
		ORDER BY NG.Distance),--nearest groupid
	(SELECT TOP 1 G.Id
		FROM dbo._church_ccv_Datamart_NearestGroup NG
		INNER JOIN GroupLocation GL 
		ON GL.LocationId = NG.GroupLocationId
		INNER JOIN [Group] G 
		ON G.Id = GL.GroupId AND G.GroupTypeId = 49
		WHERE NG.FamilyLocationId = L.Id
		ORDER BY NG.Distance),--nearestgroupname
	CASE
		WHEN EXISTS (SELECT *
			FROM [Group] G
			INNER JOIN GroupMember FM
				ON FM.GroupId = G.Id AND G.GroupTypeId = 10
			INNER JOIN [GroupMember] STM
				ON STM.PersonId = FM.PersonId
			INNER JOIN [Group] ST
				ON ST.Id = STM.GroupId AND ST.GroupTypeId = 23
			WHERE G.Id = F.Id) THEN 1
		ELSE 0
	END, --isserving
	(SELECT TimesAttendedLast16Weeks
		FROM _church_ccv_Datamart_ERA
		WHERE FamilyId = F.Id
		AND WeekendDate = dbo._church_ccv_ufnGetSaturdayDate(GETDATE())), --attendance16wk
	HH.ConnectionStatus, --connectionstatus
	HH.Email,
	HH.HomePhone, --home phone
	(SELECT COUNT(*)
		FROM GroupMember FM
		WHERE FM.GroupId = F.Id
			AND FM.GroupRoleId = 3), --adultcount
	(SELECT COUNT(*)
		FROM GroupMember FM
		WHERE FM.GroupId = F.Id
			AND FM.GroupRoleId = 4), --childcount
	L.Id,
	L.Street1 + ' ' + L.Street2,
	L.City,
	L.[State],
	L.Country,
	L.PostalCode,
	L.GeoPoint,
	L.GeoPoint.Lat,
	L.GeoPoint.Long,
	C.Name,
	STUFF((SELECT ', ' + P.NickName
			FROM GroupMember FM
			INNER JOIN Person P
				ON P.Id = FM.PersonId
			WHERE FM.GroupId = F.Id
				AND FM.GroupRoleId = 3
			ORDER BY P.Gender
			FOR XML PATH('')), 1, 1 , ''), --adultnames
	STUFF((SELECT ', ' + P.NickName
			FROM GroupMember FM
			INNER JOIN Person P
				ON P.Id = FM.PersonId
			WHERE FM.GroupId = F.Id
				AND FM.GroupRoleId = 4
			ORDER BY P.BirthDate
			FOR XML PATH('')), 1, 1 , ''), --childnames
	G.[2015],
	G.[2014],
	G.[2013],
	G.[2012],
	G.[2011],
	G.[2010],
	G.[2009],
	G.[2008],
	G.[2007],
	NEWID(),
	NULL
FROM CTE HH
INNER JOIN [Group] F ON F.Id = HH.FamilyId
INNER JOIN Campus C ON C.Id = F.CampusId
LEFT OUTER JOIN [GroupLocation] GL ON GL.GroupId = F.Id AND GL.GroupLocationTypeValueId = 19
LEFT OUTER JOIN [Location] L ON L.Id = GL.LocationId
LEFT OUTER JOIN Giving G ON G.familyid = F.Id
WHERE F.GroupTypeId = 10

END

