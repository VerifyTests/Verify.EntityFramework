SELECT [companies].[Id], [companies].[Name], [employees].[Id], [employees].[Age], [employees].[CompanyId], [employees].[Name]
FROM [Companies] AS [companies]
LEFT JOIN [Employees] AS [employees] ON [companies].[Id] = [employees].[CompanyId]
ORDER BY [companies].[Name], [companies].[Id]