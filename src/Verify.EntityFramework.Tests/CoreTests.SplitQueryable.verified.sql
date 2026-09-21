select   c.Id,
         c.Name
from     Companies as c
where    c.Name = N'company name'
order by c.Id

-- This LINQ query is being executed in split-query mode, and the SQL shown is for the first query to be executed. Additional queries may also be executed depending on the results of the first query.