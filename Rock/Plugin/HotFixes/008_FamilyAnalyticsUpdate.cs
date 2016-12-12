﻿// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
namespace Rock.Plugin.HotFixes
{
    /// <summary>
    /// 
    /// </summary>
    [MigrationNumber( 8, "1.6.0" )]
    public class FamilyAnalyticsUpdate : Migration
    {
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            Sql( @"
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[spCrm_FamilyAnalyticsAttendance]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[spCrm_FamilyAnalyticsAttendance]
" );

            Sql( @"
/*
<doc>
	<summary>
 		This stored procedure updates several attributes related to a person's
		attendance.
	</summary>
	
	<remarks>	
		For eRA we only consider adults for the critieria.
	</remarks>
	<code>
		EXEC [dbo].[spCrm_FamilyAnalyticsAttendance] 
	</code>
</doc>
*/

CREATE PROCEDURE [dbo].[spCrm_FamilyAnalyticsAttendance]

AS
BEGIN
	
	-- configuration of the duration in weeks
	DECLARE @EntryAttendanceDurationWeeks int = 16
		
	DECLARE @cACTIVE_RECORD_STATUS_VALUE_GUID uniqueidentifier = '618F906C-C33D-4FA3-8AEF-E58CB7B63F1E'
	DECLARE @cPERSON_RECORD_TYPE_VALUE_GUID uniqueidentifier = '36CF10D6-C695-413D-8E7C-4546EFEF385E'
	DECLARE @cATTRIBUTE_IS_ERA_GUID uniqueidentifier = 'CE5739C5-2156-E2AB-48E5-1337C38B935E'
	DECLARE @cFAMILY_GROUPTYPE_GUID uniqueidentifier = '790E3215-3B10-442B-AF69-616C0DCB998E'
	DECLARE @cADULT_ROLE_GUID uniqueidentifier = '2639F9A5-2AAE-4E48-A8C3-4FFE86681E42'
	DECLARE @cCHILD_ROLE_GUID uniqueidentifier = 'C8B1814F-6AA7-4055-B2D7-48FE20429CB9'

	DECLARE @cATTRIBUTE_FIRST_ATTENDED uniqueidentifier  = 'AB12B3B0-55B8-D6A5-4C1F-DB9CCB2C4342'
	DECLARE @cATTRIBUTE_LAST_ATTENDED uniqueidentifier  = '5F4C6462-018E-D19C-4AB0-9843CB21C57E'
	DECLARE @cATTRIBUTE_TIMES_ATTENDED_IN_DURATION uniqueidentifier  = '45A1E978-DC5B-CFA1-4AF4-EA098A24C914'

	-- --------- END CONFIGURATION --------------

	DECLARE @ActiveRecordStatusValueId int = (SELECT TOP 1 [Id] FROM [DefinedValue] WHERE [Guid] = @cACTIVE_RECORD_STATUS_VALUE_GUID)
	DECLARE @PersonRecordTypeValueId int = (SELECT TOP 1 [Id] FROM [DefinedValue] WHERE [Guid] = @cPERSON_RECORD_TYPE_VALUE_GUID)
	DECLARE @IsEraAttributeId int = (SELECT TOP 1 [Id] FROM [Attribute] WHERE [Guid] = @cATTRIBUTE_IS_ERA_GUID)
	DECLARE @FamilyGroupTypeId int = (SELECT TOP 1 [Id] FROM [GroupType] WHERE [Guid] = @cFAMILY_GROUPTYPE_GUID)
	DECLARE @AdultRoleId int = (SELECT TOP 1 [Id] FROM [GroupTypeRole] WHERE [Guid] = @cADULT_ROLE_GUID)
	DECLARE @ChildRoleId int = (SELECT TOP 1 [Id] FROM [GroupTypeRole] WHERE [Guid] = @cCHILD_ROLE_GUID)

	-- calculate dates for query
	DECLARE @SundayDateStart datetime = [dbo].[ufnUtility_GetPreviousSundayDate]()
	DECLARE @SundayEntryAttendanceDuration datetime = DATEADD(DAY,  (7 * @EntryAttendanceDurationWeeks * -1), @SundayDateStart)
	


	-- first checkin
	DECLARE @FirstAttendedAttributeId int = (SELECT TOP 1 [Id] FROM [Attribute] WHERE [Guid] = @cATTRIBUTE_FIRST_ATTENDED)
	DELETE FROM [AttributeValue] WHERE [AttributeId] = @FirstAttendedAttributeId;

	WITH
	  cteIndividual ([PersonId], [GivingGroupId], [FamilyRole])
	  AS
	  (
		SELECT p.[Id] AS [PersonId], p.[GivingGroupId], CASE WHEN fr.[FamilyRole] = @AdultRoleId THEN 'Adult' ELSE 'Child' END
		FROM [Person] p
		CROSS APPLY
			(
			SELECT TOP 1 gm.[GroupRoleId] AS [FamilyRole]
			FROM    
				[GroupMember] gm 
				INNER JOIN [Group] g ON g.[Id] = gm.[GroupId] AND g.[GroupTypeId] = @FamilyGroupTypeId
				INNER JOIN [GroupTypeRole] gtr ON gtr.[Id] = gm.[GroupRoleId]
			WHERE 
				gm.[PersonId] = p.[Id] 
			) fr
		WHERE
			[RecordStatusValueId] = @ActiveRecordStatusValueId -- record is active
			AND [RecordTypeValueId] = @PersonRecordTypeValueId  -- person record type (not business)
	  )
	INSERT INTO AttributeValue ([EntityId], [AttributeId], [Value], [IsSystem], [Guid], [CreatedDateTime])
	SELECT * FROM 
		(SELECT 
			i.[PersonId]
			, @FirstAttendedAttributeId AS [AttributeId]
			, CASE WHEN [FamilyRole] = 'Adult' THEN 
					(SELECT 
						MIN(a.StartDateTime )
					FROM
						[Attendance] a
						INNER JOIN [PersonAlias] pa ON pa.[Id] = a.[PersonAliasId]
					WHERE 
						[GroupId] IN (SELECT [Id] FROM [dbo].[ufnCheckin_WeeklyServiceGroups]())
                        AND a.[DidAttend] = 1
						AND pa.[PersonId] IN (SELECT [Id] FROM [dbo].[ufnCrm_FamilyMembersOfPersonId](i.[PersonId])))
				ELSE
					(SELECT 
						MIN(a.StartDateTime )
					FROM
						[Attendance] a
						INNER JOIN [PersonAlias] pa ON pa.[Id] = a.[PersonAliasId]
					WHERE 
						[GroupId] IN (SELECT [Id] FROM [dbo].[ufnCheckin_WeeklyServiceGroups]())
                        AND a.[DidAttend] = 1
						AND pa.[PersonId] = i.[PersonId])
			  END AS [FirstAttendedDate]
			, 0 AS [IsSystem]
			, newid() AS [Guid]
			, getdate() AS [CreateDate]
		FROM cteIndividual i ) AS a
	WHERE a.[FirstAttendedDate] IS NOT NULL

	-- last checkin
	DECLARE @LastAttendedAttributeId int = (SELECT TOP 1 [Id] FROM [Attribute] WHERE [Guid] = @cATTRIBUTE_LAST_ATTENDED)
	DELETE FROM [AttributeValue] WHERE [AttributeId] = @LastAttendedAttributeId;

	WITH
	  cteIndividual ([PersonId], [GivingGroupId], [FamilyRole])
	  AS
	  (
		SELECT p.[Id] AS [PersonId], p.[GivingGroupId], CASE WHEN fr.[FamilyRole] = @AdultRoleId THEN 'Adult' ELSE 'Child' END
		FROM [Person] p
		CROSS APPLY
			(
			SELECT TOP 1 gm.[GroupRoleId] AS [FamilyRole]
			FROM    
				[GroupMember] gm 
				INNER JOIN [Group] g ON g.[Id] = gm.[GroupId] AND g.[GroupTypeId] = @FamilyGroupTypeId
				INNER JOIN [GroupTypeRole] gtr ON gtr.[Id] = gm.[GroupRoleId]
			WHERE 
				gm.[PersonId] = p.[Id] 
			) fr
		WHERE
			[RecordStatusValueId] = @ActiveRecordStatusValueId -- record is active
			AND [RecordTypeValueId] = @PersonRecordTypeValueId  -- person record type (not business)
	  )
	INSERT INTO AttributeValue ([EntityId], [AttributeId], [Value], [IsSystem], [Guid], [CreatedDateTime])
	SELECT * FROM 
		(SELECT 
			i.[PersonId]
			, @LastAttendedAttributeId AS [AttributeId]
			, CASE WHEN [FamilyRole] = 'Adult' THEN 
					(SELECT 
						MAX(a.StartDateTime )
					FROM
						[Attendance] a
						INNER JOIN [PersonAlias] pa ON pa.[Id] = a.[PersonAliasId]
					WHERE 
						[GroupId] IN (SELECT [Id] FROM [dbo].[ufnCheckin_WeeklyServiceGroups]())
                        AND a.[DidAttend] = 1
						AND pa.[PersonId] IN (SELECT [Id] FROM [dbo].[ufnCrm_FamilyMembersOfPersonId](i.[PersonId])))
				ELSE
					(SELECT 
						MAX(a.StartDateTime )
					FROM
						[Attendance] a
						INNER JOIN [PersonAlias] pa ON pa.[Id] = a.[PersonAliasId]
					WHERE 
						[GroupId] IN (SELECT [Id] FROM [dbo].[ufnCheckin_WeeklyServiceGroups]())
                        AND a.[DidAttend] = 1
						AND pa.[PersonId] = i.[PersonId])
			  END AS [LastAttendedDate]
			, 0 AS [IsSystem]
			, newid() AS [Guid]
			, getdate() AS [CreateDate]
		FROM cteIndividual i ) AS a
	WHERE a.[LastAttendedDate] IS NOT NULL

	-- times checkedin
	DECLARE @TimesAttendedAttributeId int = (SELECT TOP 1 [Id] FROM [Attribute] WHERE [Guid] = @cATTRIBUTE_TIMES_ATTENDED_IN_DURATION)
	DELETE FROM [AttributeValue] WHERE [AttributeId] = @TimesAttendedAttributeId;

	WITH
	  cteIndividual ([PersonId], [GivingGroupId], [FamilyRole])
	  AS
	  (
		SELECT p.[Id] AS [PersonId], p.[GivingGroupId], CASE WHEN fr.[FamilyRole] = @AdultRoleId THEN 'Adult' ELSE 'Child' END
		FROM [Person] p
		CROSS APPLY
			(
			SELECT TOP 1 gm.[GroupRoleId] AS [FamilyRole]
			FROM    
				[GroupMember] gm 
				INNER JOIN [Group] g ON g.[Id] = gm.[GroupId] AND g.[GroupTypeId] = @FamilyGroupTypeId
				INNER JOIN [GroupTypeRole] gtr ON gtr.[Id] = gm.[GroupRoleId]
			WHERE 
				gm.[PersonId] = p.[Id] 
			) fr
		WHERE
			[RecordStatusValueId] = @ActiveRecordStatusValueId -- record is active
			AND [RecordTypeValueId] = @PersonRecordTypeValueId  -- person record type (not business)
	  )
	INSERT INTO AttributeValue ([EntityId], [AttributeId], [Value], [IsSystem], [Guid], [CreatedDateTime])
	SELECT * FROM 
		(SELECT 
			i.[PersonId]
			, @TimesAttendedAttributeId AS [AttributeId]
			, CASE WHEN [FamilyRole] = 'Adult' THEN 
					(SELECT 
						COUNT(*)
					FROM
						[Attendance] a
						INNER JOIN [PersonAlias] pa ON pa.[Id] = a.[PersonAliasId]
					WHERE 
						[GroupId] IN (SELECT [Id] FROM [dbo].[ufnCheckin_WeeklyServiceGroups]())
						AND CAST( a.[StartDateTime] AS DATE ) <= @SundayDateStart AND a.[StartDateTime] >= @SundayEntryAttendanceDuration
                        AND a.[DidAttend] = 1
						AND pa.[PersonId] IN (SELECT [Id] FROM [dbo].[ufnCrm_FamilyMembersOfPersonId](i.[PersonId])))
				ELSE
					(SELECT 
						COUNT(*)
					FROM
						[Attendance] a
						INNER JOIN [PersonAlias] pa ON pa.[Id] = a.[PersonAliasId]
					WHERE 
						[GroupId] IN (SELECT [Id] FROM [dbo].[ufnCheckin_WeeklyServiceGroups]())
						AND CAST( a.[StartDateTime] AS DATE ) <= @SundayDateStart AND a.[StartDateTime] >= @SundayEntryAttendanceDuration
                        AND a.[DidAttend] = 1
						AND pa.[PersonId] = i.[PersonId])
			  END AS [CheckinCount]
			, 0 AS [IsSystem]
			, newid() AS [Guid]
			, getdate() AS [CreateDate]
		FROM cteIndividual i ) AS a
	WHERE a.[CheckinCount] IS NOT NULL

	
END
" );

            Sql( @"
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[spCrm_FamilyAnalyticsEraDataset]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[spCrm_FamilyAnalyticsEraDataset]
" );

            Sql( @"
/*
<doc>
	<summary>
 		This stored procedure returns a data set used by the Rock eRA job to add/remove
		people from being an eRA. It should not be modified as it will be updated in the
		future to meet additional requirements.

		The goal of the query is to return both those that meet the eRA requirements as well
		as those that are marked as already being an eRA and the criteria to ensure that
		they still should be an era.
	</summary>
	
	<remarks>	
		For eRA we only consider adults for the critieria.
	</remarks>
	<code>
		EXEC [dbo].[spCrm_FamilyAnalyticsEraDataset] 
	</code>
</doc>
*/

CREATE PROCEDURE [dbo].[spCrm_FamilyAnalyticsEraDataset]
	
AS
BEGIN
	
	-- configuration of the duration in weeks
	DECLARE @EntryGivingDurationLongWeeks int = 52
	DECLARE @EntryGivingDurationShortWeeks int = 6
	DECLARE @EntryAttendanceDurationWeeks int = 16
	DECLARE @ExitGivingDurationWeeks int = 8
	DECLARE @ExitAttendanceDurationShortWeeks int = 4
	DECLARE @ExitAttendanceDurationLongWeeks int = 16

	-- configuration of the item counts in the durations
	DECLARE @EntryGiftCountDurationLong int = 4
	DECLARE @EntryGiftCountDurationShort int = 1
	DECLARE @EntryAttendanceCountDuration int = 8
	
	DECLARE @cACTIVE_RECORD_STATUS_VALUE_GUID uniqueidentifier = '618F906C-C33D-4FA3-8AEF-E58CB7B63F1E'
	DECLARE @cPERSON_RECORD_TYPE_VALUE_GUID uniqueidentifier = '36CF10D6-C695-413D-8E7C-4546EFEF385E'
	DECLARE @cATTRIBUTE_IS_ERA_GUID uniqueidentifier = 'CE5739C5-2156-E2AB-48E5-1337C38B935E'
	DECLARE @cFAMILY_GROUPTYPE_GUID uniqueidentifier = '790E3215-3B10-442B-AF69-616C0DCB998E'
	DECLARE @cADULT_ROLE_GUID uniqueidentifier = '2639F9A5-2AAE-4E48-A8C3-4FFE86681E42'
	DECLARE @cTRANSACTION_TYPE_CONTRIBUTION uniqueidentifier = '2D607262-52D6-4724-910D-5C6E8FB89ACC';

	-- --------- END CONFIGURATION --------------

	DECLARE @ActiveRecordStatusValueId int = (SELECT TOP 1 [Id] FROM [DefinedValue] WHERE [Guid] = @cACTIVE_RECORD_STATUS_VALUE_GUID)
	DECLARE @PersonRecordTypeValueId int = (SELECT TOP 1 [Id] FROM [DefinedValue] WHERE [Guid] = @cPERSON_RECORD_TYPE_VALUE_GUID)
	DECLARE @IsEraAttributeId int = (SELECT TOP 1 [Id] FROM [Attribute] WHERE [Guid] = @cATTRIBUTE_IS_ERA_GUID)
	DECLARE @FamilyGroupTypeId int = (SELECT TOP 1 [Id] FROM [GroupType] WHERE [Guid] = @cFAMILY_GROUPTYPE_GUID)
	DECLARE @AdultRoleId int = (SELECT TOP 1 [Id] FROM [GroupTypeRole] WHERE [Guid] = @cADULT_ROLE_GUID)
	DECLARE @ContributionType int = (SELECT TOP 1 [Id] FROM [DefinedValue] WHERE [Guid] = @cTRANSACTION_TYPE_CONTRIBUTION)

	-- calculate dates for query
	DECLARE @SundayDateStart datetime = [dbo].[ufnUtility_GetPreviousSundayDate]()
	DECLARE @SundayEntryGivingDurationLong datetime = DATEADD(DAY,  (7 * @EntryGivingDurationLongWeeks * -1), @SundayDateStart)
	DECLARE @SundayEntryGivingDurationShort datetime = DATEADD(DAY,  (7 * @EntryGivingDurationShortWeeks * -1), @SundayDateStart)
	DECLARE @SundayEntryAttendanceDuration datetime = DATEADD(DAY,  (7 * @EntryAttendanceDurationWeeks * -1), @SundayDateStart)

	DECLARE @SundayExitGivingDuration datetime = DATEADD(DAY, (7 * @ExitGivingDurationWeeks * -1), @SundayDateStart)
	DECLARE @SundayExitAttendanceDurationShort datetime = DATEADD(DAY,  (7 * @ExitAttendanceDurationShortWeeks * -1), @SundayDateStart)
	DECLARE @SundayExitAttendanceDurationLong datetime = DATEADD(DAY,  (7 * @ExitAttendanceDurationLongWeeks * -1), @SundayDateStart)
	

	SELECT
		[FamilyId]
		, MAX([EntryGiftCountDurationShort]) AS [EntryGiftCountDurationShort]
		, MAX([EntryGiftCountDurationLong]) AS [EntryGiftCountDurationLong]
		, MAX([ExitGiftCountDuration]) AS [ExitGiftCountDuration]
		, MAX([EntryAttendanceCountDuration]) AS [EntryAttendanceCountDuration]
		, MAX([ExitAttendanceCountDurationShort]) AS [ExitAttendanceCountDurationShort]
		, MAX([ExitAttendanceCountDurationLong]) AS [ExitAttendanceCountDurationLong]
		, CAST(MAX([IsEra]) AS BIT) AS [IsEra]
	FROM (
		SELECT 
			p.[Id]
			, CASE WHEN (era.[Value] = 'true') THEN 1  ELSE 0 END AS [IsEra]
			, g.[Id] AS [FamilyId]
			, (SELECT COUNT(DISTINCT(ft.[Id])) 
					FROM [FinancialTransaction] ft
						INNER JOIN [PersonAlias] pa ON pa.[Id] = ft.[AuthorizedPersonAliasId]
						INNER JOIN [Person] g1 ON g1.[Id] = pa.[PersonId]
						INNER JOIN [FinancialTransactionDetail] ftd ON ftd.[TransactionId] = ft.[Id]
						INNER JOIN [FinancialAccount] fa ON fa.[Id] = ftd.AccountId
					WHERE 
						ft.TransactionTypeValueId = @ContributionType
						AND ft.TransactionDateTime >= @SundayEntryGivingDurationShort
						AND ft.TransactionDateTime <= @SundayDateStart
						AND ( g1.[Id] = p.[Id] OR ( g1.[GivingGroupId] IS NOT NULL AND g1.[GivingGroupID] = p.[GivingGroupId] ) )
						AND fa.[IsTaxDeductible] = 1) AS [EntryGiftCountDurationShort]
			, (SELECT COUNT(DISTINCT(ft.[Id])) 
					FROM [FinancialTransaction] ft
						INNER JOIN [PersonAlias] pa ON pa.[Id] = ft.[AuthorizedPersonAliasId]
						INNER JOIN [Person] g1 ON g1.[Id] = pa.[PersonId]
						INNER JOIN [FinancialTransactionDetail] ftd ON ftd.[TransactionId] = ft.[Id]
						INNER JOIN [FinancialAccount] fa ON fa.[Id] = ftd.AccountId
					WHERE 
						ft.TransactionTypeValueId = @ContributionType
						AND ft.TransactionDateTime >= @SundayExitGivingDuration
						AND ft.TransactionDateTime <= @SundayDateStart
						AND ( g1.[Id] = p.[Id] OR ( g1.[GivingGroupId] IS NOT NULL AND g1.[GivingGroupID] = p.[GivingGroupId] ) )
						AND fa.[IsTaxDeductible] = 1) AS [ExitGiftCountDuration]	
			, (SELECT COUNT(DISTINCT(ft.[Id])) 
					FROM [FinancialTransaction] ft
						INNER JOIN [PersonAlias] pa ON pa.[Id] = ft.[AuthorizedPersonAliasId]
						INNER JOIN [Person] g1 ON g1.[Id] = pa.[PersonId]
						INNER JOIN [FinancialTransactionDetail] ftd ON ftd.[TransactionId] = ft.[Id]
						INNER JOIN [FinancialAccount] fa ON fa.[Id] = ftd.AccountId
					WHERE 
						ft.TransactionTypeValueId = @ContributionType
						AND ft.TransactionDateTime >= @SundayEntryGivingDurationLong
						AND ft.TransactionDateTime <= @SundayDateStart
						AND ( g1.[Id] = p.[Id] OR ( g1.[GivingGroupId] IS NOT NULL AND g1.[GivingGroupID] = p.[GivingGroupId] ) )
						AND fa.[IsTaxDeductible] = 1) AS [EntryGiftCountDurationLong]	
			, (SELECT 
					COUNT(DISTINCT a.SundayDate )
				FROM
					[Attendance] a
					INNER JOIN [Group] ag ON ag.[Id] = a.[GroupId]
					INNER JOIN [GroupType] agt ON agt.[Id] = ag.[GroupTypeId] AND agt.[AttendanceCountsAsWeekendService] = 1
					INNER JOIN [PersonAlias] pa ON pa.[Id] = a.[PersonAliasId]
				WHERE 
					pa.[PersonId] IN (SELECT [PersonId] FROM [GroupMember] WHERE [GroupId] = g.[Id] ) 
                    AND a.[DidAttend] = 1
					AND a.[StartDateTime] <= @SundayDateStart AND a.[StartDateTime] >= @SundayExitAttendanceDurationShort) AS [ExitAttendanceCountDurationShort]
			, (SELECT 
					COUNT(DISTINCT a.SundayDate )
				FROM
					[Attendance] a
					INNER JOIN [Group] ag ON ag.[Id] = a.[GroupId]
					INNER JOIN [GroupType] agt ON agt.[Id] = ag.[GroupTypeId] AND agt.[AttendanceCountsAsWeekendService] = 1
					INNER JOIN [PersonAlias] pa ON pa.[Id] = a.[PersonAliasId]
				WHERE 
					pa.[PersonId] IN (SELECT [PersonId] FROM [GroupMember] WHERE [GroupId] = g.[Id] ) 
                    AND a.[DidAttend] = 1
					AND a.[StartDateTime] <= @SundayDateStart AND a.[StartDateTime] >= @SundayEntryAttendanceDuration) AS [EntryAttendanceCountDuration]
			, (SELECT 
					COUNT(DISTINCT a.SundayDate )
				FROM
					[Attendance] a
					INNER JOIN [Group] ag ON ag.[Id] = a.[GroupId]
					INNER JOIN [GroupType] agt ON agt.[Id] = ag.[GroupTypeId] AND agt.[AttendanceCountsAsWeekendService] = 1
					INNER JOIN [PersonAlias] pa ON pa.[Id] = a.[PersonAliasId]
				WHERE 
					pa.[PersonId] IN (SELECT [PersonId] FROM [GroupMember] WHERE [GroupId] = g.[Id] ) 
                    AND a.[DidAttend] = 1
					AND a.[StartDateTime] <= @SundayDateStart AND a.[StartDateTime] >= @SundayExitAttendanceDurationLong) AS [ExitAttendanceCountDurationLong]	
		FROM
			[Person] p
			INNER JOIN [GroupMember] gm ON gm.[PersonId] = p.[Id] AND gm.[GroupRoleId] = @AdultRoleId
			INNER JOIN [Group] g ON g.[Id] = gm.[GroupId] AND g.[GroupTypeId] = @FamilyGroupTypeId
			LEFT OUTER JOIN [AttributeValue] era ON era.[EntityId] = p.[Id] AND era.[AttributeId] = @IsEraAttributeId
		WHERE
			[RecordStatusValueId] = @ActiveRecordStatusValueId -- record is active
			AND [RecordTypeValueId] = @PersonRecordTypeValueId  -- person record type (not business)
		) AS t
		WHERE (
			([IsEra] = 1)
			OR (
				( [EntryGiftCountDurationLong] >= @EntryGiftCountDurationLong AND [EntryGiftCountDurationShort] >= @EntryGiftCountDurationShort )
				OR
				( [EntryAttendanceCountDuration] >= @EntryAttendanceCountDuration )
			)
		)
		GROUP BY [FamilyId]
	
END
" );
            // Fix for issue #1877 to allow Benevolence workers to upload Benevolence Request Documents.
            RockMigrationHelper.AddSecurityAuthForBinaryFileType( Rock.SystemGuid.BinaryFiletype.BENEVOLENCE_REQUEST_DOCUMENTS, 0, "Edit", true, Rock.SystemGuid.Group.GROUP_BENEVOLENCE, Model.SpecialRole.None, "620C29F0-983C-45D5-B5D9-782E7792EEE3" );
        }

        /// <summary>
        /// The commands to undo a migration from a specific version
        /// </summary>
        public override void Down()
        {
            RockMigrationHelper.DeleteSecurityAuth( "620C29F0-983C-45D5-B5D9-782E7792EEE3" );
        }
    }
}