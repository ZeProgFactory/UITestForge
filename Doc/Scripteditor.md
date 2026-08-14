# Script Editor 

## Syntax
- Script syntax is simple line-based: `command [args]`  
  - `tap <automationId>`  
  - `fill <automationId> <text>`  
  - `clear <automationId>`  
  - `focus <automationId>`  
  - `screenshot [optional-path]`  
  - `# comment` / blank lines skipped  
- CLI commands follow existing patterns: `ui tap --automationId "id"`, `ui fill --automationId "id" --text "value"`, `ui clear --automationId "id"`, `ui focus --automationId "id"`, `ui screenshot --output "path" --overwrite`
- Snippet buttons append template text to the Editor
- Run executes line by line, streaming output to a result log area
- `#if !ANDROID` guard used like the rest of the file
