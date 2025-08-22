# GitHub Workflow Scripts

This directory contains scripts used by GitHub Actions workflows.

## xaml-preview.py

A Python script that processes XAML files and generates formatted previews for pull request comments.

### Features

- **XAML Analysis**: Parses XAML files to extract root elements, controls, and metadata
- **Formatted Output**: Creates markdown-formatted previews with syntax highlighting
- **File Information**: Shows file size, line count, and used UI controls
- **Change Detection**: Handles added, modified, and removed files differently
- **Error Handling**: Gracefully handles malformed XAML or missing files

### Usage

```bash
python3 xaml-preview.py --modified "file1.xaml file2.xaml" --added "file3.xaml" --removed "file4.xaml"
```

### Integration

This script is used by the `.github/workflows/xaml-preview.yml` workflow, which automatically:

1. Triggers on pull requests that modify `.xaml` files
2. Analyzes the changed XAML files
3. Posts formatted previews as PR comments
4. Updates existing comments when new changes are pushed

### Output Format

The script generates a markdown comment that includes:

- Overview of changed files count
- For each file:
  - Change type indicator (✨ Added, 📝 Modified, 🗑️ Removed)
  - File metadata (root element, controls count, file size)
  - List of UI controls used in the file
  - Collapsible section with syntax-highlighted XAML content
  - Truncated preview (first 30 lines) for readability

This provides developers with immediate visual feedback on XAML changes without needing to check out the branch locally.