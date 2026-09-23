select   c.Id,
         c.Name,
         e.Id,
         e.Age,
         e.CompanyId,
         e.Name
from     Companies as c
         left outer join
         Employees as e
         on c.Id = e.CompanyId
where    c.Name = N'Company1'
order by c.Id