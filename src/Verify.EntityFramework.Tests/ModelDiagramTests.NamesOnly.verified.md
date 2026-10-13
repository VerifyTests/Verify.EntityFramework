```mermaid
erDiagram
  Companies["**Companies**"]
  Employees["**Employees**"]
  Companies ||--o{ Employees : "FK_Employees_Companies_CompanyId"
```