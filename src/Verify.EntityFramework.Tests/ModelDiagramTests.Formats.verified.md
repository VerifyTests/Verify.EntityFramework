```mermaid
erDiagram
  Companies["**Companies**"] {
    int Id pk
    nvarchar(max) Name
  }
  Employees["**Employees**"] {
    int Id pk
    int Age
    int CompanyId
    nvarchar(max) Name
  }
  Companies ||--o{ Employees : "FK_Employees_Companies_CompanyId"
```