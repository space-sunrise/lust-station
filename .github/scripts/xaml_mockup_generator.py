#!/usr/bin/env python3
"""
Simple XAML visual mockup generator using Pillow.
Creates basic visual representations of XAML layouts.
"""

from PIL import Image, ImageDraw, ImageFont
from typing import Dict, List, Tuple, Optional
import os
import sys

# Add the script directory to path to import xaml-preview
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
try:
    from xaml_preview import ControlInfo, parse_xaml_structure
except ImportError:
    # If we can't import, we'll redefine the needed parts
    import xml.etree.ElementTree as ET
    from dataclasses import dataclass
    
    @dataclass
    class ControlInfo:
        name: str
        element_type: str
        attributes: Dict[str, str]
        children: List['ControlInfo']
        text_content: Optional[str] = None
        namespace: Optional[str] = None
    
    def parse_xaml_structure(file_path: str) -> Dict:
        # Simplified version for standalone use
        try:
            tree = ET.parse(file_path)
            root = tree.getroot()
            
            def parse_control_recursive(element: ET.Element) -> ControlInfo:
                name = element.tag.split('}')[-1] if '}' in element.tag else element.tag
                namespace = element.tag.split('}')[0][1:] if '}' in element.tag else None
                
                control = ControlInfo(
                    name=name,
                    element_type=name,
                    attributes=dict(element.attrib),
                    children=[],
                    text_content=element.text.strip() if element.text and element.text.strip() else None,
                    namespace=namespace
                )
                
                for child in element:
                    control.children.append(parse_control_recursive(child))
                
                return control
            
            return {'structure': parse_control_recursive(root)}
        except Exception as e:
            return {'error': str(e)}


class XamlMockupGenerator:
    def __init__(self, width: int = 800, height: int = 600):
        self.width = width
        self.height = height
        # Updated colors to match Space Station 14's UI theme
        self.colors = {
            'background': '#1B1B1E',  # Dark background like in game
            'panel': '#25262B',       # Panel background
            'panel_inset': '#1E1E22', # Inset panels
            'button': '#404040',      # Button background
            'button_hover': '#4A4A50', # Button hover
            'text': '#BFC0C4',        # Light text
            'text_bright': '#FFFFFF', # Bright text  
            'accent': '#4F94D4',      # Blue accent
            'border': '#404048',      # Border color
            'separator': '#353540',   # Separator color
            'tab_active': '#505060',  # Active tab
            'tab_inactive': '#35353F', # Inactive tab
            'input': '#2A2A30',       # Input background
            'warning': '#D49C3D',     # Warning/orange
            'error': '#D43F3F',       # Error/red
            'success': '#3FD43F'      # Success/green
        }
        
    def generate_mockup(self, xaml_file: str, output_file: str) -> bool:
        """Generate a visual mockup of the XAML file."""
        try:
            # Parse the XAML structure
            info = parse_xaml_structure(xaml_file)
            if 'error' in info:
                print(f"Error parsing XAML: {info['error']}")
                return False
            
            structure = info.get('structure')
            if not structure:
                print("No structure found in XAML")
                return False
            
            # Create image
            image = Image.new('RGB', (self.width, self.height), self.colors['background'])
            draw = ImageDraw.Draw(image)
            
            # Try to load fonts
            try:
                # Try to find good fonts for the mockup
                font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 11)
                title_font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 14)
                small_font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 9)
            except:
                try:
                    font = ImageFont.truetype("arial.ttf", 11)
                    title_font = ImageFont.truetype("arialbd.ttf", 14)
                    small_font = ImageFont.truetype("arial.ttf", 9)
                except:
                    font = ImageFont.load_default()
                    title_font = ImageFont.load_default()
                    small_font = ImageFont.load_default()
            
            # Draw title bar
            title = f"XAML Preview: {os.path.basename(xaml_file)}"
            title_height = 30
            draw.rectangle([0, 0, self.width, title_height], fill=self.colors['panel'], outline=self.colors['border'])
            draw.text((10, 8), title, fill=self.colors['text_bright'], font=title_font)
            
            # Draw the UI structure
            self._draw_control(draw, structure, 5, title_height + 5, self.width - 10, self.height - title_height - 10, font, small_font)
            
            # Add a subtle grid pattern for better visual reference
            self._draw_background_grid(draw)
            
            # Save the image
            image.save(output_file, 'PNG')
            return True
            
        except Exception as e:
            print(f"Error generating mockup: {e}")
            return False
    
    def _draw_background_grid(self, draw: ImageDraw.ImageDraw):
        """Draw a subtle background grid for visual reference."""
        grid_color = '#202025'
        grid_size = 20
        
        # Draw vertical lines
        for x in range(0, self.width, grid_size):
            draw.line([(x, 0), (x, self.height)], fill=grid_color, width=1)
        
        # Draw horizontal lines
        for y in range(0, self.height, grid_size):
            draw.line([(0, y), (self.width, y)], fill=grid_color, width=1)
    
    def _draw_control(self, draw: ImageDraw.ImageDraw, control: ControlInfo, 
                     x: int, y: int, width: int, height: int, 
                     font: ImageFont.ImageFont, small_font: ImageFont.ImageFont,
                     level: int = 0) -> int:
        """Draw a control and its children, returns the Y position after drawing."""
        if level > 10 or width < 10 or height < 10:  # Prevent infinite recursion and too small areas
            return y
        
        # Determine control type and style
        control_style = self._get_control_style(control.element_type)
        
        # Calculate dimensions based on content and level
        control_height = min(control_style['height'] + (level * 2), height // 3)
        padding = max(3, 8 - level)
        
        # Ensure we don't exceed available space
        if y + control_height > y + height:
            control_height = max(15, height - 5)
        
        # Draw the control background if needed
        if control_style['draw_background']:
            draw.rectangle([x, y, x + width, y + control_height], 
                         fill=control_style['background'], 
                         outline=control_style['border'], width=1)
        
        # Draw control icon and label
        label_text = self._get_control_label(control)
        icon = self._get_control_icon_text(control.element_type)
        
        if label_text:
            display_text = f"{icon} {label_text}" if icon else label_text
            text_color = control_style.get('text_color', self.colors['text'])
            
            # Choose font based on control type
            current_font = font
            if control.element_type in ['Window', 'FancyWindow', 'DefaultWindow']:
                current_font = font  # Use regular font
                text_color = self.colors['text_bright']
            elif level > 2:
                current_font = small_font
            
            # Limit text length to fit
            max_chars = max(5, (width - padding * 2) // 7)
            if len(display_text) > max_chars:
                display_text = display_text[:max_chars - 3] + "..."
            
            # Position text properly
            text_y = y + max(2, (control_height - 12) // 2)
            draw.text((x + padding, text_y), display_text, fill=text_color, font=current_font)
        
        # Handle special control types
        if control.element_type == 'Button':
            # Draw button highlight
            highlight_y = y + control_height - 2
            draw.line([(x + 2, highlight_y), (x + width - 2, highlight_y)], 
                     fill=self.colors['accent'], width=1)
        
        elif control.element_type in ['Separator', 'HSeparator']:
            # Draw horizontal separator
            sep_y = y + control_height // 2
            draw.line([(x + 5, sep_y), (x + width - 5, sep_y)], 
                     fill=self.colors['separator'], width=2)
        
        elif control.element_type == 'VSeparator':
            # Draw vertical separator
            sep_x = x + width // 2
            draw.line([(sep_x, y + 2), (sep_x, y + control_height - 2)], 
                     fill=self.colors['separator'], width=2)
        
        # Draw children for container controls
        current_y = y + control_height + padding
        available_height = height - (current_y - y)
        
        if control.children and available_height > 20:
            # Different layout strategies based on container type
            if control.element_type == 'BoxContainer':
                orientation = control.attributes.get('Orientation', 'Vertical')
                if orientation == 'Horizontal':
                    self._draw_horizontal_children(draw, control.children, x + padding, current_y, 
                                                 width - padding * 2, available_height, 
                                                 font, small_font, level + 1)
                else:
                    self._draw_vertical_children(draw, control.children, x + padding, current_y,
                                               width - padding * 2, available_height,
                                               font, small_font, level + 1)
            elif control.element_type == 'SplitContainer':
                self._draw_split_children(draw, control, x + padding, current_y,
                                        width - padding * 2, available_height,
                                        font, small_font, level + 1)
            elif control.element_type == 'TabContainer':
                self._draw_tab_children(draw, control.children, x + padding, current_y,
                                      width - padding * 2, available_height,
                                      font, small_font, level + 1)
            else:
                # Default: vertical layout
                self._draw_vertical_children(draw, control.children, x + padding * 2, current_y,
                                           width - padding * 4, available_height,
                                           font, small_font, level + 1)
        
        return max(current_y, y + control_height)
    
    def _draw_vertical_children(self, draw, children, x, y, width, height, font, small_font, level):
        """Draw children in vertical layout."""
        child_height = max(15, height // max(1, len(children)))
        current_y = y
        
        for child in children:
            if current_y >= y + height - 10:
                break
            remaining_height = y + height - current_y
            actual_height = min(child_height, remaining_height)
            
            if actual_height > 10:
                current_y = self._draw_control(draw, child, x, current_y, width, actual_height,
                                             font, small_font, level)
                current_y += 2
    
    def _draw_horizontal_children(self, draw, children, x, y, width, height, font, small_font, level):
        """Draw children in horizontal layout."""
        if not children:
            return
        
        child_width = max(30, width // len(children))
        current_x = x
        
        for child in children:
            if current_x >= x + width - 10:
                break
            remaining_width = x + width - current_x
            actual_width = min(child_width, remaining_width)
            
            if actual_width > 20:
                self._draw_control(draw, child, current_x, y, actual_width, height,
                                 font, small_font, level)
                current_x += actual_width + 3
    
    def _draw_split_children(self, draw, control, x, y, width, height, font, small_font, level):
        """Draw children in split container layout."""
        orientation = control.attributes.get('Orientation', 'Horizontal')
        
        if len(control.children) >= 2:
            if orientation == 'Horizontal':
                # Split horizontally
                split_x = x + width // 2
                self._draw_control(draw, control.children[0], x, y, width // 2 - 2, height,
                                 font, small_font, level)
                # Draw splitter
                draw.line([(split_x, y), (split_x, y + height)], fill=self.colors['separator'], width=2)
                if len(control.children) > 1:
                    self._draw_control(draw, control.children[1], split_x + 2, y, width // 2 - 2, height,
                                     font, small_font, level)
            else:
                # Split vertically
                split_y = y + height // 2
                self._draw_control(draw, control.children[0], x, y, width, height // 2 - 2,
                                 font, small_font, level)
                # Draw splitter
                draw.line([(x, split_y), (x + width, split_y)], fill=self.colors['separator'], width=2)
                if len(control.children) > 1:
                    self._draw_control(draw, control.children[1], x, split_y + 2, width, height // 2 - 2,
                                     font, small_font, level)
    
    def _draw_tab_children(self, draw, children, x, y, width, height, font, small_font, level):
        """Draw children in tab container layout."""
        if not children:
            return
        
        # Draw tab headers
        tab_height = 25
        tab_width = min(100, width // max(1, len(children)))
        
        for i, child in enumerate(children[:min(5, len(children))]):  # Max 5 tabs
            tab_x = x + i * tab_width
            if i == 0:  # First tab is active
                draw.rectangle([tab_x, y, tab_x + tab_width, y + tab_height],
                             fill=self.colors['tab_active'], outline=self.colors['border'])
            else:
                draw.rectangle([tab_x, y, tab_x + tab_width, y + tab_height],
                             fill=self.colors['tab_inactive'], outline=self.colors['border'])
            
            # Tab label
            tab_label = f"Tab {i+1}"
            if 'Name' in child.attributes:
                tab_label = child.attributes['Name'][:8]
            draw.text((tab_x + 5, y + 6), tab_label, fill=self.colors['text'], font=small_font)
        
        # Draw content area
        content_y = y + tab_height
        content_height = height - tab_height
        
        if content_height > 20 and children:
            # Draw first child as active tab content
            draw.rectangle([x, content_y, x + width, y + height],
                         fill=self.colors['panel_inset'], outline=self.colors['border'])
            self._draw_control(draw, children[0], x + 3, content_y + 3, width - 6, content_height - 6,
                             font, small_font, level)
    
    def _get_control_style(self, control_type: str) -> Dict:
        """Get visual style for a control type matching SS14's theme."""
        styles = {
            'Window': {
                'height': 30,
                'background': self.colors['panel'],
                'border': self.colors['border'],
                'draw_background': True,
                'text_color': self.colors['text_bright']
            },
            'FancyWindow': {
                'height': 30,
                'background': self.colors['panel'],
                'border': self.colors['accent'],
                'draw_background': True,
                'text_color': self.colors['text_bright']
            },
            'DefaultWindow': {
                'height': 30,
                'background': self.colors['panel'],
                'border': self.colors['border'],
                'draw_background': True,
                'text_color': self.colors['text_bright']
            },
            'Button': {
                'height': 22,
                'background': self.colors['button'],
                'border': self.colors['border'],
                'draw_background': True,
                'text_color': self.colors['text_bright']
            },
            'Label': {
                'height': 16,
                'background': None,
                'border': None,
                'draw_background': False,
                'text_color': self.colors['text']
            },
            'RichTextLabel': {
                'height': 16,
                'background': None,
                'border': None,
                'draw_background': False,
                'text_color': self.colors['text']
            },
            'TextEdit': {
                'height': 20,
                'background': self.colors['input'],
                'border': self.colors['border'],
                'draw_background': True,
                'text_color': self.colors['text']
            },
            'LineEdit': {
                'height': 20,
                'background': self.colors['input'],
                'border': self.colors['border'],
                'draw_background': True,
                'text_color': self.colors['text']
            },
            'BoxContainer': {
                'height': 18,
                'background': None,
                'border': self.colors['separator'],
                'draw_background': False,
                'text_color': self.colors['text']
            },
            'PanelContainer': {
                'height': 20,
                'background': self.colors['panel_inset'],
                'border': self.colors['border'],
                'draw_background': True,
                'text_color': self.colors['text']
            },
            'ScrollContainer': {
                'height': 18,
                'background': self.colors['panel_inset'],
                'border': self.colors['border'],
                'draw_background': True,
                'text_color': self.colors['text']
            },
            'SplitContainer': {
                'height': 20,
                'background': None,
                'border': self.colors['separator'],
                'draw_background': False,
                'text_color': self.colors['accent']
            },
            'TabContainer': {
                'height': 25,
                'background': self.colors['panel'],
                'border': self.colors['border'],
                'draw_background': True,
                'text_color': self.colors['text']
            },
            'CheckBox': {
                'height': 18,
                'background': None,
                'border': None,
                'draw_background': False,
                'text_color': self.colors['text']
            },
            'OptionButton': {
                'height': 20,
                'background': self.colors['button'],
                'border': self.colors['border'],
                'draw_background': True,
                'text_color': self.colors['text']
            },
            'Separator': {
                'height': 8,
                'background': None,
                'border': None,
                'draw_background': False,
                'text_color': self.colors['separator']
            },
            'HSeparator': {
                'height': 8,
                'background': None,
                'border': None,
                'draw_background': False,
                'text_color': self.colors['separator']
            },
            'VSeparator': {
                'height': 20,
                'background': None,
                'border': None,
                'draw_background': False,
                'text_color': self.colors['separator']
            }
        }
        
        # Default style for unknown controls
        default_style = {
            'height': 18,
            'background': self.colors['panel'],
            'border': self.colors['border'],
            'draw_background': True,
            'text_color': self.colors['text']
        }
        
        return styles.get(control_type, default_style)
    
    def _get_control_icon_text(self, control_type: str) -> str:
        """Get a text icon for the control type for better accessibility."""
        icons = {
            'Window': '[WIN]',
            'FancyWindow': '[WIN]',
            'DefaultWindow': '[WIN]',
            'BoxContainer': '[BOX]',
            'VBoxContainer': '[VBOX]',
            'HBoxContainer': '[HBOX]',
            'SplitContainer': '[SPLIT]',
            'ScrollContainer': '[SCROLL]',
            'TabContainer': '[TAB]',
            'PanelContainer': '[PANEL]',
            'Button': '[BTN]',
            'Label': '',
            'RichTextLabel': '',
            'TextEdit': '[TEXT]',
            'LineEdit': '[INPUT]',
            'CheckBox': '[☐]',
            'OptionButton': '[○]',
            'Separator': '—',
            'VSeparator': '|',
            'HSeparator': '—',
        }
        return icons.get(control_type, '[CTRL]')
    
    def _get_control_label(self, control: ControlInfo) -> str:
        """Get display label for a control."""
        # Priority: Name > Text > Type
        if 'Name' in control.attributes:
            return f"{control.element_type}: {control.attributes['Name']}"
        elif 'Text' in control.attributes:
            text = control.attributes['Text']
            # Clean up localization keys
            if text.startswith('{Loc'):
                text = text.replace('{Loc ', '').replace("'", '').replace('}', '')
            return f"{control.element_type}: {text}"
        else:
            return control.element_type


def main():
    if len(sys.argv) < 2:
        print("Usage: python3 xaml_mockup_generator.py <xaml-file> [output-file]")
        return
    
    xaml_file = sys.argv[1]
    output_file = sys.argv[2] if len(sys.argv) > 2 else xaml_file.replace('.xaml', '_mockup.png')
    
    if not os.path.exists(xaml_file):
        print(f"XAML file not found: {xaml_file}")
        return
    
    generator = XamlMockupGenerator()
    if generator.generate_mockup(xaml_file, output_file):
        print(f"Mockup generated: {output_file}")
    else:
        print("Failed to generate mockup")


if __name__ == '__main__':
    main()