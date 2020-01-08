CREATE TABLE [dbo].[Companies](
	[Id] [int] NOT NULL,
	[Content] [nvarchar](max) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

CREATE TABLE [dbo].[Employees](
	[Id] [int] NOT NULL,
	[CompanyId] [int] NOT NULL,
	[Content] [nvarchar](max) NULL,
	[Age] [int] NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
