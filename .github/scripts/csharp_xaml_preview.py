#!/usr/bin/env python3
"""
C# XAML Preview Generator integration script.
This script builds and runs the C# XAML preview tool, then uploads the images.
"""

import subprocess
import sys
import os
import json
import base64
import argparse
from pathlib import Path

def build_csharp_tool():
    """Build the C# XAML preview tool."""
    print("Building C# XAML preview tool...")
    
    # Build the preview tool
    result = subprocess.run([
        "dotnet", "build", 
        "Content.XamlPreview/Content.XamlPreview.csproj",
        "--configuration", "Release",
        "--no-restore"
    ], capture_output=True, text=True)
    
    if result.returncode != 0:
        print(f"Failed to build C# preview tool:")
        print(result.stderr)
        return False
    
    print("Successfully built C# XAML preview tool")
    return True

def generate_preview_with_csharp(xaml_file, output_file):
    """Generate a preview using the C# tool."""
    print(f"Generating preview for {xaml_file}...")
    
    # Run the C# preview generator
    result = subprocess.run([
        "dotnet", "run",
        "--project", "Content.XamlPreview/Content.XamlPreview.csproj",
        "--configuration", "Release",
        "--no-build",
        "--", xaml_file, output_file
    ], capture_output=True, text=True)
    
    if result.returncode != 0:
        print(f"Failed to generate preview for {xaml_file}:")
        print(result.stderr)
        return False
    
    print(f"Successfully generated preview: {output_file}")
    return True

def embed_image_as_data_url(image_path):
    """Convert image to base64 data URL for inline embedding."""
    try:
        with open(image_path, 'rb') as f:
            image_data = f.read()
        
        # Convert to base64
        base64_data = base64.b64encode(image_data).decode('utf-8')
        
        # Create data URL
        data_url = f"data:image/png;base64,{base64_data}"
        return data_url
    except Exception as e:
        print(f"Failed to embed image {image_path}: {e}")
        return None

def main():
    parser = argparse.ArgumentParser(description='Generate XAML previews using C# tool')
    parser.add_argument('--modified', default='', help='Modified XAML files (space-separated)')
    parser.add_argument('--added', default='', help='Added XAML files (space-separated)')
    parser.add_argument('--removed', default='', help='Removed XAML files (space-separated)')
    parser.add_argument('--output-dir', default='xaml-previews', help='Output directory for images')
    
    args = parser.parse_args()
    
    # Parse file lists
    modified_files = args.modified.split() if args.modified else []
    added_files = args.added.split() if args.added else []
    removed_files = args.removed.split() if args.removed else []
    
    # Filter for XAML files and check existence
    xaml_files = []
    missing_files = []
    
    for files_list in [modified_files, added_files]:
        for file in files_list:
            if file.endswith('.xaml'):
                if os.path.exists(file):
                    xaml_files.append(file)
                else:
                    missing_files.append(file)
                    print(f"Warning: XAML file {file} was detected as changed but not found in current checkout")
    
    if missing_files:
        print(f"Missing files that may need attention: {missing_files}")
        print("This usually means the workflow is running on the wrong branch/commit.")
    
    if not xaml_files:
        if missing_files:
            print(f"No XAML files to process - {len(missing_files)} files were missing from checkout")
            # Still return 0 to avoid failing the workflow, but generate a report
        else:
            print("No XAML files to process")
        return 0
    
    # Build the C# tool
    if not build_csharp_tool():
        return 1
    
    # Create output directory
    os.makedirs(args.output_dir, exist_ok=True)
    
    # Generate previews
    image_urls = {}
    successful_previews = 0
    
    for xaml_file in xaml_files:
        # Generate output filename
        base_name = os.path.splitext(os.path.basename(xaml_file))[0]
        output_file = os.path.join(args.output_dir, f"{base_name}_preview.png")
        
        # Generate preview
        if generate_preview_with_csharp(xaml_file, output_file):
            # Convert to data URL for embedding
            data_url = embed_image_as_data_url(output_file)
            if data_url:
                image_urls[xaml_file] = data_url
                successful_previews += 1
            else:
                print(f"Failed to embed image for {xaml_file}")
        else:
            print(f"Failed to generate preview for {xaml_file}")
    
    # Output the image URLs as JSON for the workflow
    with open('image_urls.json', 'w') as f:
        json.dump(image_urls, f, indent=2)
    
    print(f"Generated {successful_previews} previews out of {len(xaml_files)} XAML files")
    
    # Generate preview content for GitHub comment
    content = generate_preview_content(modified_files, added_files, removed_files, image_urls, missing_files)
    
    # Output for GitHub Actions
    print(f"PREVIEW_CONTENT<<EOF")
    print(content)
    print("EOF")
    
    return 0

def generate_preview_content(modified_files, added_files, removed_files, image_urls, missing_files=None):
    """Generate the GitHub comment content with embedded images."""
    
    # Filter for XAML files
    modified_xaml = [f for f in modified_files if f.endswith('.xaml')]
    added_xaml = [f for f in added_files if f.endswith('.xaml')]
    removed_xaml = [f for f in removed_files if f.endswith('.xaml')]
    
    total_files = len(modified_xaml) + len(added_xaml) + len(removed_xaml)
    
    content = []
    content.append("🎨 XAML Preview Bot")
    content.append("")
    content.append(f"Found {total_files} XAML file(s) changed: {len(modified_xaml)} modified, {len(added_xaml)} added, {len(removed_xaml)} removed")
    content.append("")
    
    # Process each type of change
    for file_list, change_type, emoji in [
        (modified_xaml, "Modified", "📝"),
        (added_xaml, "Added", "✨"),
        (removed_xaml, "Removed", "🗑️")
    ]:
        if not file_list:
            continue
            
        for xaml_file in file_list:
            content.append(f"{emoji} **{change_type}**: {xaml_file}")
            
            if change_type != "Removed" and xaml_file in image_urls:
                # File exists and has a preview
                content.append("")
            elif change_type != "Removed" and missing_files and xaml_file in missing_files:
                # File was detected as changed but not found
                content.append("⚠️ File not found in current checkout")
            elif change_type == "Removed":
                # File was removed
                content.append("")
            else:
                # File exists but preview failed
                content.append("❌ Preview generation failed")
            content.append("")
    
    # Add troubleshooting info if there were missing files
    if missing_files:
        content.append("## ⚠️ Issues Detected")
        content.append("")
        content.append("Some files were detected as changed but not found in the current checkout.")
        content.append("This typically happens when the workflow runs on the wrong branch/commit.")
        content.append("The preview bot has been updated to checkout the correct PR branch.")
        content.append("")
    
    content.append("🤖 **About This Preview**")
    content.append("This enhanced preview shows the UI structure and layout analysis of your XAML changes. The structure diagram uses icons to represent different control types and shows the hierarchy to help you understand the layout without building locally.")
    content.append("")
    content.append("Preview automatically generated by XAML Preview Bot")
    
    return "\n".join(content)

if __name__ == "__main__":
    sys.exit(main())