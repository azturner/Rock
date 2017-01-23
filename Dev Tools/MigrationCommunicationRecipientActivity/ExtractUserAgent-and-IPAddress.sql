declare @ipaddressPatternSendGridMandrill nvarchar(max) = '%([0-9]%.%[0-9]%.%[0-9]%.%[0-9]%)%'
declare @ipaddressPatternMailgun nvarchar(max) = '%Opened from [0-9]%.%[0-9]%.%[0-9]%.%[0-9]% using %'
declare @ipaddressPatternMailgun_start nvarchar(max) = '%[0-9]%.%[0-9]%.%[0-9]%.%[0-9]%'
declare @ipaddressPatternMailgun_end nvarchar(max) = '%u%'

-- get the IP Address (SendGrid or Mandrill)
SELECT top 1000 replace(replace(substring([ActivityDetail], PATINDEX(@ipaddressPatternSendGridMandrill, [ActivityDetail]), 8000), '(', ''), ')', '')
  FROM [CommunicationRecipientActivity]
  where ActivityType = 'Opened'  and [ActivityDetail] like @ipaddressPatternSendGridMandrill order by id desc

-- get the IP Address (Mailgun)
select top 1000 substring(x.Parsed, 0, PATINDEX(@ipaddressPatternMailgun_end, x.Parsed)) from (
SELECT id, substring([ActivityDetail], PATINDEX(@ipaddressPatternMailgun_start, [ActivityDetail]), 8000) [Parsed]
  FROM [CommunicationRecipientActivity]
  where ActivityType = 'Opened' and [ActivityDetail] like @ipaddressPatternMailgun
  ) x  order by id desc


-- get just the UserAgent, etc stuff  (SendGrid or Mandrill)
SELECT replace(substring([ActivityDetail], 0, PATINDEX(@ipaddressPatternSendGridMandrill, [ActivityDetail])), 'Opened from', ''), count(*)
  FROM [CommunicationRecipientActivity]
  where ActivityType = 'Opened'
  group by replace(substring([ActivityDetail], 0, PATINDEX(@ipaddressPatternSendGridMandrill, [ActivityDetail])), 'Opened from', '')
  order by count(*) desc
  


  