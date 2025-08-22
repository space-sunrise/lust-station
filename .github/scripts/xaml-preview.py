#!/usr/bin/env python3
"""
XAML Preview Generator
Processes XAML files and generates formatted previews for PR comments.
"""

import os
import sys
import xml.etree.ElementTree as ET
import argparse
from pathlib import Path
from typing import List, Dict, Optional


def parse_xaml_file(file_path: str) -> Dict:
    """Parse XAML file and extract useful information."""
    try:
        tree = ET.parse(file_path)
        root = tree.getroot()
        
        # Extract basic information
        info = {
            'root_element': root.tag,
            'attributes': dict(root.attrib),
            'children_count': len(list(root)),
            'has_content': bool(root.text and root.text.strip()),
            'namespaces': {},
            'controls': [],
            'file_size': os.path.getsize(file_path),
            'line_count': 0
        }
        
        # Count lines
        with open(file_path, 'r', encoding='utf-8') as f:
            info['line_count'] = len(f.readlines())
        
        # Extract namespaces
        for key, value in root.attrib.items():
            if key.startswith('xmlns'):
                namespace_name = key.split(':', 1)[1] if ':' in key else 'default'
                info['namespaces'][namespace_name] = value
        
        # Extract child controls (simplified)
        for child in root.iter():
            if child.tag != root.tag:
                control_name = child.tag.split('}')[-1] if '}' in child.tag else child.tag
                if control_name not in info['controls']:
                    info['controls'].append(control_name)
        
        return info
        
    except ET.ParseError as e:
        return {
            'error': f'XML Parse Error: {str(e)}',
            'file_size': os.path.getsize(file_path) if os.path.exists(file_path) else 0,
            'line_count': 0
        }
    except Exception as e:
        return {
            'error': f'Error: {str(e)}',
            'file_size': os.path.getsize(file_path) if os.path.exists(file_path) else 0,
            'line_count': 0
        }


def format_file_info(file_path: str, info: Dict, change_type: str) -> str:
    """Format file information for display."""
    icon_map = {
        'added': '✨',
        'modified': '📝',
        'removed': '🗑️'
    }
    
    icon = icon_map.get(change_type, '📄')
    relative_path = file_path
    
    if 'error' in info:
        return f"## {icon} {change_type.title()}: `{relative_path}`\n\n⚠️ **Error processing file:** {info['error']}\n\n"
    
    # File size formatting
    size = info['file_size']
    if size > 1024 * 1024:
        size_str = f"{size / (1024 * 1024):.1f} MB"
    elif size > 1024:
        size_str = f"{size / 1024:.1f} KB"
    else:
        size_str = f"{size} bytes"
    
    # Build summary
    summary_parts = []
    
    # Clean up root element name for better readability
    root_element = info.get('root_element', 'Unknown')
    if '}' in root_element:
        root_element = root_element.split('}')[-1]
    summary_parts.append(f"**Root Element:** `{root_element}`")
    
    summary_parts.append(f"**Controls:** {len(info.get('controls', []))} types")
    summary_parts.append(f"**Size:** {size_str} ({info['line_count']} lines)")
    
    if info.get('controls'):
        controls_list = ', '.join(f"`{ctrl}`" for ctrl in info['controls'][:10])
        if len(info['controls']) > 10:
            controls_list += f" and {len(info['controls']) - 10} more"
        summary_parts.append(f"**Used Controls:** {controls_list}")
    
    # Show namespaces if interesting
    namespaces = info.get('namespaces', {})
    if namespaces and len(namespaces) > 1:  # More than just default namespace
        ns_list = []
        for ns, uri in namespaces.items():
            if ns != 'default' and 'spacestation14.io' not in uri:
                ns_list.append(f"`{ns}`")
        if ns_list:
            summary_parts.append(f"**Custom Namespaces:** {', '.join(ns_list[:5])}")
    
    summary = '\n'.join(f"- {part}" for part in summary_parts)
    
    # Read file content for preview
    content_preview = ""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            lines = f.readlines()
            preview_lines = lines[:30]  # Show first 30 lines
            content_preview = ''.join(preview_lines)
            if len(lines) > 30:
                content_preview += f"\n... (showing first 30 of {len(lines)} lines)"
    except Exception as e:
        content_preview = f"Error reading file: {str(e)}"
    
    return f"""## {icon} {change_type.title()}: `{relative_path}`

{summary}

<details><summary>📋 Click to view XAML content</summary>

```xml
{content_preview}
```

</details>

"""


def process_xaml_files(modified_files: List[str], added_files: List[str], removed_files: List[str]) -> str:
    """Process all XAML files and generate preview content."""
    
    # Filter to only include XAML files
    modified_xaml = [f for f in modified_files if f.endswith('.xaml')]
    added_xaml = [f for f in added_files if f.endswith('.xaml')]
    removed_xaml = [f for f in removed_files if f.endswith('.xaml')]
    
    preview_content = "# 🎨 XAML Preview Bot\n\n"
    
    total_files = len(modified_xaml) + len(added_xaml) + len(removed_xaml)
    
    if total_files == 0:
        return preview_content + "No XAML files were changed in this PR.\n"
    
    preview_content += f"Found **{total_files}** XAML file(s) changed in this PR:\n\n"
    
    # Process added files
    for file_path in added_xaml:
        if os.path.exists(file_path):
            info = parse_xaml_file(file_path)
            preview_content += format_file_info(file_path, info, 'added')
        else:
            preview_content += f"## ✨ Added: `{file_path}`\n\n⚠️ **File not found in current checkout**\n\n"
    
    # Process modified files
    for file_path in modified_xaml:
        if os.path.exists(file_path):
            info = parse_xaml_file(file_path)
            preview_content += format_file_info(file_path, info, 'modified')
        else:
            preview_content += f"## 📝 Modified: `{file_path}`\n\n⚠️ **File not found in current checkout**\n\n"
    
    # Process removed files
    for file_path in removed_xaml:
        preview_content += f"## 🗑️ Removed: `{file_path}`\n\n"
    
    # Add footer
    preview_content += "\n---\n"
    preview_content += "*This preview was automatically generated by the XAML Preview Bot*"
    
    return preview_content


def main():
    parser = argparse.ArgumentParser(description='Generate XAML file previews')
    parser.add_argument('--modified', default='', help='Space-separated list of modified files')
    parser.add_argument('--added', default='', help='Space-separated list of added files')
    parser.add_argument('--removed', default='', help='Space-separated list of removed files')
    
    args = parser.parse_args()
    
    # Parse file lists
    modified_files = [f.strip() for f in args.modified.split() if f.strip()]
    added_files = [f.strip() for f in args.added.split() if f.strip()]
    removed_files = [f.strip() for f in args.removed.split() if f.strip()]
    
    # Generate preview content
    preview_content = process_xaml_files(modified_files, added_files, removed_files)
    
    # Output for GitHub Actions
    print("PREVIEW_CONTENT<<EOF")
    print(preview_content)
    print("EOF")


if __name__ == '__main__':
    main()