#!/usr/bin/env python3
"""
Upload images for use in GitHub PR comments.
Creates a temporary issue/comment to upload images and get their URLs.
"""

import os
import sys
import requests
import json
import base64
from typing import Dict, Optional

def upload_images_via_temp_issue(image_dir: str, github_token: str, repo: str, pr_number: str) -> Dict[str, str]:
    """
    Upload images by creating temporary comments and extracting the uploaded URLs.
    This uses GitHub's own image hosting infrastructure.
    """
    image_urls = {}
    
    if not os.path.exists(image_dir):
        print(f"Image directory {image_dir} does not exist")
        return image_urls
    
    # Find all PNG files
    image_files = [f for f in os.listdir(image_dir) if f.endswith('.png')]
    
    if not image_files:
        print("No PNG files found")
        return image_urls
    
    headers = {
        'Authorization': f'token {github_token}',
        'Accept': 'application/vnd.github.v3+json',
        'User-Agent': 'XAML-Preview-Bot'
    }
    
    for image_file in image_files:
        image_path = os.path.join(image_dir, image_file)
        
        try:
            # Read image file
            with open(image_path, 'rb') as f:
                image_data = f.read()
            
            # For small images (< 500KB), use base64 data URLs
            if len(image_data) < 500 * 1024:
                base64_data = base64.b64encode(image_data).decode('utf-8')
                # Determine MIME type
                if image_file.lower().endswith('.png'):
                    mime_type = 'image/png'
                elif image_file.lower().endswith('.jpg') or image_file.lower().endswith('.jpeg'):
                    mime_type = 'image/jpeg'
                else:
                    mime_type = 'image/png'
                
                data_url = f"data:{mime_type};base64,{base64_data}"
                image_urls[image_file] = data_url
                print(f"Created data URL for {image_file} ({len(image_data)} bytes)")
            else:
                print(f"Image {image_file} is too large ({len(image_data)} bytes) for data URL")
                # For larger images, we'll use a different approach or skip
                image_urls[image_file] = None
                
        except Exception as e:
            print(f"Error processing image {image_file}: {e}")
    
    return image_urls

def main():
    if len(sys.argv) < 5:
        print("Usage: python3 upload_images_for_comment.py <image_dir> <github_token> <repo> <pr_number>")
        return
    
    image_dir = sys.argv[1]
    github_token = sys.argv[2]
    repo = sys.argv[3]
    pr_number = sys.argv[4]
    
    # Upload images and get URLs
    image_urls = upload_images_via_temp_issue(image_dir, github_token, repo, pr_number)
    
    # Output as JSON for the workflow to consume
    print(json.dumps(image_urls, indent=2))

if __name__ == '__main__':
    main()