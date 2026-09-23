select   e.Id,
         e.Age,
         e.CompanyId,
         e.Name,
         c.Id,
         c.Name,
         e0.Id,
         e0.Age,
         e0.CompanyId,
         e0.Name
from     Employees as e
         inner join
         Companies as c
         on e.CompanyId = c.Id
         left outer join
         Employees as e0
         on c.Id = e0.CompanyId
where    e.Name = N'Employee1'
order by e.Id,
         c.Id