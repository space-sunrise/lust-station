using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Content.Client.UserInterface.Controls;
using Content.Client.Stylesheets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;

using SSColor = Robust.Shared.Maths.Color;
using ImageColor = SixLabors.ImageSharp.Color;

namespace Content.XamlPreview;

/// <summary>
/// Simplified XAML preview generator that creates visual representations 
/// of XAML files using the actual SS14 client control types.
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: Content.XamlPreview <xaml-file> <output-png>");
            Console.WriteLine("Example: Content.XamlPreview Content.Client/Guidebook/Controls/GuidebookWindow.xaml output.png");
            return 1;
        }

        var xamlPath = args[0];
        var outputPath = args[1];

        if (!File.Exists(xamlPath))
        {
            Console.WriteLine($"Error: XAML file '{xamlPath}' not found.");
            return 1;
        }

        try
        {
            var generator = new XamlPreviewGenerator();
            await generator.GeneratePreview(xamlPath, outputPath);
            Console.WriteLine($"Successfully generated preview: {outputPath}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating preview: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return 1;
        }
    }
}

/// <summary>
/// Generates XAML previews by parsing XAML structure and creating 
/// realistic visual representations using actual SS14 colors and styles.
/// </summary>
public class XamlPreviewGenerator
{
    /// <summary>
    /// SS14 color palette from StyleNano.cs for authentic rendering
    /// </summary>
    private static class SS14Colors
    {
        // Background colors
        public static readonly ImageColor PanelDark = ImageColor.FromRgb(30, 30, 34);              // #1E1E22
        public static readonly ImageColor BackgroundDark = ImageColor.FromRgb(27, 27, 30);         // #1B1B1E
        public static readonly ImageColor PanelBackground = ImageColor.FromRgb(37, 37, 42);        // #25252A
        
        // Button colors  
        public static readonly ImageColor ButtonDefault = ImageColor.FromRgb(70, 73, 102);         // #464966
        public static readonly ImageColor ButtonHovered = ImageColor.FromRgb(87, 91, 127);         // #575b7f
        public static readonly ImageColor ButtonPressed = ImageColor.FromRgb(62, 108, 69);         // #3e6c45
        public static readonly ImageColor ButtonDisabled = ImageColor.FromRgb(48, 49, 60);         // #30313c
        
        public static readonly ImageColor ButtonRed = ImageColor.FromRgb(212, 59, 59);             // #D43B3B
        public static readonly ImageColor ButtonRedHovered = ImageColor.FromRgb(223, 107, 107);    // #DF6B6B
        
        public static readonly ImageColor ButtonGreen = ImageColor.FromRgb(62, 108, 69);           // #3E6C45
        public static readonly ImageColor ButtonGreenHovered = ImageColor.FromRgb(49, 132, 62);    // #31843E
        
        // Accent colors
        public static readonly ImageColor NanoGold = ImageColor.FromRgb(168, 139, 94);             // #A88B5E
        public static readonly ImageColor AccentBlue = ImageColor.FromRgb(79, 148, 212);           // #4F94D4
        
        // Text colors
        public static readonly ImageColor TextDefault = ImageColor.White;
        public static readonly ImageColor TextDisabled = ImageColor.FromRgb(90, 90, 90);           // #5A5A5A
        public static readonly ImageColor TextGreen = ImageColor.FromRgb(49, 132, 62);             // #31843E
        public static readonly ImageColor TextOrange = ImageColor.FromRgb(165, 118, 47);           // #A5762F
        public static readonly ImageColor TextRed = ImageColor.FromRgb(187, 50, 50);               // #BB3232
        
        // Input field colors
        public static readonly ImageColor InputBackground = ImageColor.FromRgb(20, 21, 25);        // #141519
        public static readonly ImageColor InputBorder = ImageColor.FromRgb(100, 102, 105);         // #646669
        
        // Window colors
        public static readonly ImageColor WindowBackground = ImageColor.FromRgb(37, 38, 43);       // #25262B
        public static readonly ImageColor WindowBorder = ImageColor.FromRgb(79, 148, 212);         // #4F94D4
        public static readonly ImageColor TitleBarBackground = ImageColor.FromRgb(45, 46, 51);     // #2D2E33
        
        // Chat colors  
        public static readonly ImageColor ChatBackground = ImageColor.FromRgba(37, 37, 42, 221);   // #25252ADD
        
        // Tree colors
        public static readonly ImageColor TreeEvenRow = ImageColor.FromRgb(37, 37, 42);            // #25252A
        public static readonly ImageColor TreeOddRow = ImageColor.FromRgb(30, 30, 34);             // TreeEvenRow * 0.8
        public static readonly ImageColor TreeSelectedRow = ImageColor.FromRgb(55, 55, 68);        // #373744
    }
    public async Task GeneratePreview(string xamlPath, string outputPath)
    {
        Console.WriteLine($"Parsing XAML file: {xamlPath}");
        
        // Parse XAML to understand structure  
        var structure = await ParseXamlStructure(xamlPath);
        if (structure == null)
        {
            throw new InvalidOperationException($"Failed to parse XAML structure from {xamlPath}");
        }

        Console.WriteLine($"Rendering preview for {structure.RootType}...");
        
        // Create visual representation
        await CreateVisualPreview(structure, outputPath);
        
        Console.WriteLine($"Preview generation complete.");
    }

    private async Task<XamlStructure?> ParseXamlStructure(string xamlPath)
    {
        try
        {
            var xamlContent = await File.ReadAllTextAsync(xamlPath);
            
            // Extract basic structure from XAML
            var rootTypeName = ExtractRootTypeName(xamlContent);
            if (string.IsNullOrEmpty(rootTypeName))
            {
                return null;
            }

            // Analyze the XAML content for key elements
            var structure = new XamlStructure
            {
                RootType = rootTypeName,
                FilePath = xamlPath,
                HasTitle = xamlContent.Contains("Title="),
                HasButtons = xamlContent.Contains("<Button") || xamlContent.Contains("Button "),
                HasLabels = xamlContent.Contains("<Label") || xamlContent.Contains("Label "),
                HasInputs = xamlContent.Contains("<LineEdit") || xamlContent.Contains("LineEdit ") || 
                           xamlContent.Contains("<TextEdit") || xamlContent.Contains("TextEdit "),
                HasContainers = xamlContent.Contains("Container") || xamlContent.Contains("Split"),
                HasTabs = xamlContent.Contains("Tab"),
                HasScrolls = xamlContent.Contains("ScrollContainer") || xamlContent.Contains("Scroll"),
                HasTrees = xamlContent.Contains("Tree") || xamlContent.Contains("ItemList"),
                ComplexityScore = CalculateComplexity(xamlContent),
                ActualText = ExtractTextContent(xamlContent),
                ButtonTexts = ExtractButtonTexts(xamlContent),
                LabelTexts = ExtractLabelTexts(xamlContent)
            };

            // Try to extract title if it's a window
            if (structure.HasTitle)
            {
                var titleMatch = Regex.Match(xamlContent, @"Title=""([^""]+)""");
                if (titleMatch.Success)
                {
                    structure.Title = titleMatch.Groups[1].Value;
                }
            }

            // Extract window size if specified
            ExtractWindowSize(xamlContent, structure);

            return structure;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing XAML: {ex.Message}");
            return null;
        }
    }

    private static string ExtractRootTypeName(string xamlContent)
    {
        using var reader = new StringReader(xamlContent);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (line.StartsWith('<') && !line.StartsWith("<?") && !line.StartsWith("<!--"))
            {
                var startIndex = 1;
                var endIndex = line.IndexOfAny([' ', '>', '\t', '\n'], startIndex);
                if (endIndex > startIndex)
                {
                    var elementName = line.Substring(startIndex, endIndex - startIndex);
                    if (elementName.Contains(':'))
                    {
                        elementName = elementName.Split(':').Last();
                    }
                    return elementName;
                }
            }
        }
        return string.Empty;
    }

    private static int CalculateComplexity(string xamlContent)
    {
        var score = 0;
        
        // Count control types
        score += CountOccurrences(xamlContent, "<Button");
        score += CountOccurrences(xamlContent, "<Label");
        score += CountOccurrences(xamlContent, "<LineEdit");
        score += CountOccurrences(xamlContent, "<TextEdit");
        score += CountOccurrences(xamlContent, "Container");
        score += CountOccurrences(xamlContent, "<Split");
        score += CountOccurrences(xamlContent, "<Tab");
        score += CountOccurrences(xamlContent, "<Tree");
        score += CountOccurrences(xamlContent, "<Grid");
        score += CountOccurrences(xamlContent, "<Scroll");
        score += CountOccurrences(xamlContent, "<ItemList");
        
        return score;
    }

    private static string ExtractTextContent(string xamlContent)
    {
        var textMatches = Regex.Matches(xamlContent, @"Text=""([^""]+)""");
        var texts = textMatches.Cast<Match>().Select(m => m.Groups[1].Value).ToList();
        return string.Join(", ", texts.Take(3)); // First 3 text elements
    }

    private static List<string> ExtractButtonTexts(string xamlContent)
    {
        var buttonTexts = new List<string>();
        var buttonMatches = Regex.Matches(xamlContent, @"<Button[^>]*Text=""([^""]+)""");
        foreach (Match match in buttonMatches)
        {
            buttonTexts.Add(match.Groups[1].Value);
        }
        return buttonTexts.Take(5).ToList(); // Max 5 buttons
    }

    private static List<string> ExtractLabelTexts(string xamlContent)
    {
        var labelTexts = new List<string>();
        var labelMatches = Regex.Matches(xamlContent, @"<Label[^>]*Text=""([^""]+)""");
        foreach (Match match in labelMatches)
        {
            labelTexts.Add(match.Groups[1].Value);
        }
        return labelTexts.Take(5).ToList(); // Max 5 labels
    }

    private static void ExtractWindowSize(string xamlContent, XamlStructure structure)
    {
        // Try to extract MinSize or SetSize
        var sizeMatch = Regex.Match(xamlContent, @"(?:MinSize|SetSize)=""(\d+),?\s*(\d+)""");
        if (sizeMatch.Success)
        {
            if (int.TryParse(sizeMatch.Groups[1].Value, out var width) && 
                int.TryParse(sizeMatch.Groups[2].Value, out var height))
            {
                structure.PreferredWidth = width;
                structure.PreferredHeight = height;
            }
        }
    }

    private static int CountOccurrences(string text, string pattern)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(pattern, index)) != -1)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }

    private async Task CreateVisualPreview(XamlStructure structure, string outputPath)
    {
        // Determine appropriate size based on control type and XAML content
        var (width, height) = GetPreviewSize(structure);
        
        Console.WriteLine($"Creating {width}x{height} preview for {structure.RootType}...");

        using var image = new Image<Rgba32>(width, height);
        
        // Fill with authentic SS14 background color
        image.Mutate(x => x.BackgroundColor(SS14Colors.BackgroundDark));
        
        // Render the UI based on structure using SS14 styling
        image.Mutate(ctx => RenderXamlStructure(ctx, structure, width, height));

        // Save to PNG
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await image.SaveAsPngAsync(outputPath);
        Console.WriteLine($"Saved {width}x{height} preview to {outputPath}");
    }

    private (int width, int height) GetPreviewSize(XamlStructure structure)
    {
        // Use preferred size from XAML if available
        if (structure.PreferredWidth > 0 && structure.PreferredHeight > 0)
        {
            return (structure.PreferredWidth, structure.PreferredHeight);
        }

        // Default sizes based on control type and complexity
        return structure.RootType.ToLowerInvariant() switch
        {
            "fancywindow" => structure.ComplexityScore > 10 ? (1000, 800) : (900, 700),
            "window" => structure.ComplexityScore > 8 ? (900, 700) : (800, 600),
            "dialog" => structure.ComplexityScore > 5 ? (500, 400) : (400, 300),
            "panel" => structure.ComplexityScore > 6 ? (700, 500) : (600, 400),
            "tabcontainer" => (800, 600),
            "splitcontainer" => (900, 600),
            _ => structure.ComplexityScore > 10 ? (900, 700) : (800, 600)
        };
    }

    private void RenderXamlStructure(IImageProcessingContext ctx, XamlStructure structure, int width, int height)
    {
        // Render based on the root control type
        switch (structure.RootType.ToLowerInvariant())
        {
            case "fancywindow":
            case "window":
                RenderWindow(ctx, structure, width, height);
                break;
            case "panel":
            case "panelcontainer":
                RenderPanel(ctx, structure, width, height);
                break;
            default:
                RenderGenericControl(ctx, structure, width, height);
                break;
        }
    }

    private void RenderWindow(IImageProcessingContext ctx, XamlStructure structure, int width, int height)
    {
        // Window background using authentic SS14 colors
        ctx.Fill(SS14Colors.WindowBackground, new RectangleF(0, 0, width, height));
        
        // Window border
        ctx.Draw(SS14Colors.WindowBorder, 2, new RectangleF(0, 0, width, height));
        
        // Title bar
        ctx.Fill(SS14Colors.TitleBarBackground, new RectangleF(0, 0, width, 35));
        
        // Title text rendering
        if (!string.IsNullOrEmpty(structure.Title))
        {
            RenderText(ctx, structure.Title, 10, 10, SS14Colors.TextDefault, 14);
        }
        else
        {
            // Default title placeholder
            RenderText(ctx, "Window", 10, 10, SS14Colors.TextDefault, 14);
        }
        
        // Window controls (close, minimize, etc.) using SS14 colors
        ctx.Fill(SS14Colors.ButtonRed, new RectangleF(width - 30, 8, 18, 18)); // Close button
        ctx.Fill(SS14Colors.NanoGold, new RectangleF(width - 55, 8, 18, 18)); // Minimize button
        
        // Content area
        var contentY = 40;
        var contentHeight = height - contentY - 5;
        
        RenderContent(ctx, structure, 5, contentY, width - 10, contentHeight);
    }

    private void RenderPanel(IImageProcessingContext ctx, XamlStructure structure, int width, int height)
    {
        // Panel background using authentic SS14 colors
        ctx.Fill(SS14Colors.PanelBackground, new RectangleF(0, 0, width, height));
        
        // Panel border
        ctx.Draw(SS14Colors.AccentBlue, 1, new RectangleF(0, 0, width, height));
        
        RenderContent(ctx, structure, 5, 5, width - 10, height - 10);
    }

    private void RenderGenericControl(IImageProcessingContext ctx, XamlStructure structure, int width, int height)
    {
        // Generic control background
        ctx.Fill(SS14Colors.PanelDark, new RectangleF(0, 0, width, height));
        
        // Border
        ctx.Draw(SS14Colors.AccentBlue, 1, new RectangleF(0, 0, width, height));
        
        // Type indicator
        RenderText(ctx, structure.RootType, 5, 5, SS14Colors.NanoGold, 12);
        
        RenderContent(ctx, structure, 5, 30, width - 10, height - 35);
    }

    private void RenderContent(IImageProcessingContext ctx, XamlStructure structure, int x, int y, int width, int height)
    {
        var currentY = y;
        var itemHeight = 35;
        var spacing = 10;
        
        // Render containers first
        if (structure.HasContainers)
        {
            // Split container representation using SS14 colors
            ctx.Draw(SS14Colors.AccentBlue, 1, new RectangleF(x, currentY, width, height / 2));
            ctx.Fill(ImageColor.FromRgba(79, 148, 212, 32), new RectangleF(x + 1, currentY + 1, width - 2, height / 2 - 2));
            currentY += height / 2 + spacing;
        }
        
        // Render scroll containers
        if (structure.HasScrolls)
        {
            RenderScrollContainer(ctx, x, currentY, width, Math.Min(150, height - (currentY - y)));
            currentY += 160;
        }
        
        // Render tree controls
        if (structure.HasTrees)
        {
            RenderTreeControl(ctx, x, currentY, width, Math.Min(200, height - (currentY - y)));
            currentY += 210;
        }
        
        // Render buttons with actual text
        if (structure.HasButtons)
        {
            var buttonTexts = structure.ButtonTexts.Any() ? structure.ButtonTexts : new List<string> { "OK", "Cancel", "Apply" };
            for (int i = 0; i < Math.Min(buttonTexts.Count, 5); i++)
            {
                if (currentY + itemHeight > y + height) break;
                
                var buttonText = buttonTexts[i].Length > 15 ? buttonTexts[i][..12] + "..." : buttonTexts[i];
                RenderButton(ctx, x + 10, currentY, Math.Min(150, width - 20), 30, buttonText);
                currentY += itemHeight;
            }
        }
        
        // Render input fields
        if (structure.HasInputs)
        {
            for (int i = 0; i < Math.Min(3, structure.ComplexityScore / 4); i++)
            {
                if (currentY + itemHeight > y + height) break;
                
                RenderInput(ctx, x + 10, currentY, Math.Min(200, width - 20), 25, i == 0 ? "Input text..." : "");
                currentY += itemHeight;
            }
        }
        
        // Render labels with actual text
        if (structure.HasLabels)
        {
            var labelTexts = structure.LabelTexts.Any() ? structure.LabelTexts : new List<string> { "Label", "Status", "Info" };
            for (int i = 0; i < Math.Min(labelTexts.Count, 6); i++)
            {
                if (currentY + 20 > y + height) break;
                
                var labelText = labelTexts[i].Length > 20 ? labelTexts[i][..17] + "..." : labelTexts[i];
                RenderLabel(ctx, x + 10, currentY, Math.Min(200, width - 20), 18, labelText);
                currentY += 25;
            }
        }
        
        // Render tabs if present
        if (structure.HasTabs)
        {
            RenderTabs(ctx, x, currentY, width, Math.Min(40, y + height - currentY));
        }
    }

    private void RenderButton(IImageProcessingContext ctx, int x, int y, int width, int height, string text)
    {
        // Button background using authentic SS14 colors
        ctx.Fill(SS14Colors.ButtonDefault, new RectangleF(x, y, width, height));
        
        // Button border with SS14 accent color
        ctx.Draw(SS14Colors.AccentBlue, 1, new RectangleF(x, y, width, height));
        
        // Button text - render actual text instead of placeholder
        if (!string.IsNullOrEmpty(text))
        {
            RenderText(ctx, text, x + 8, y + (height - 12) / 2, SS14Colors.TextDefault, 10);
        }
    }

    private void RenderInput(IImageProcessingContext ctx, int x, int y, int width, int height, string placeholder = "")
    {
        // Input background using authentic SS14 colors
        ctx.Fill(SS14Colors.InputBackground, new RectangleF(x, y, width, height));
        
        // Input border
        ctx.Draw(SS14Colors.InputBorder, 1, new RectangleF(x, y, width, height));
        
        // Placeholder text
        if (!string.IsNullOrEmpty(placeholder))
        {
            RenderText(ctx, placeholder, x + 5, y + (height - 10) / 2, SS14Colors.TextDisabled, 9);
        }
        else
        {
            // Cursor indicator
            ctx.Fill(SS14Colors.TextDefault, new RectangleF(x + 5, y + 5, 1, height - 10));
        }
    }

    private void RenderLabel(IImageProcessingContext ctx, int x, int y, int width, int height, string text = "Label")
    {
        // Render actual label text
        RenderText(ctx, text, x, y, SS14Colors.TextDefault, 10);
    }

    private void RenderTabs(IImageProcessingContext ctx, int x, int y, int width, int height)
    {
        // Tab background using SS14 colors
        ctx.Fill(SS14Colors.TitleBarBackground, new RectangleF(x, y, width, height));
        
        // Individual tabs
        var tabWidth = width / 3;
        var tabNames = new[] { "General", "Options", "Advanced" };
        
        for (int i = 0; i < 3; i++)
        {
            var tabX = x + i * tabWidth;
            
            // Tab background (active vs inactive)
            var tabColor = i == 0 ? SS14Colors.ButtonHovered : SS14Colors.ButtonDefault;
            ctx.Fill(tabColor, new RectangleF(tabX, y, tabWidth, height));
            ctx.Draw(SS14Colors.AccentBlue, 1, new RectangleF(tabX, y, tabWidth, height));
            
            // Tab text
            RenderText(ctx, tabNames[i], tabX + 8, y + (height - 12) / 2, SS14Colors.TextDefault, 10);
        }
    }

    private void RenderScrollContainer(IImageProcessingContext ctx, int x, int y, int width, int height)
    {
        // Scroll container background
        ctx.Fill(SS14Colors.PanelDark, new RectangleF(x, y, width, height));
        ctx.Draw(SS14Colors.InputBorder, 1, new RectangleF(x, y, width, height));
        
        // Scrollbar
        var scrollbarWidth = 12;
        ctx.Fill(SS14Colors.ButtonDefault, new RectangleF(x + width - scrollbarWidth, y, scrollbarWidth, height));
        ctx.Fill(SS14Colors.ButtonHovered, new RectangleF(x + width - scrollbarWidth + 2, y + 10, scrollbarWidth - 4, height / 3));
        
        // Content items
        for (int i = 0; i < 3; i++)
        {
            var itemY = y + 5 + i * 25;
            ctx.Fill(SS14Colors.TreeEvenRow, new RectangleF(x + 5, itemY, width - scrollbarWidth - 10, 20));
            RenderText(ctx, $"Item {i + 1}", x + 8, itemY + 4, SS14Colors.TextDefault, 9);
        }
    }

    private void RenderTreeControl(IImageProcessingContext ctx, int x, int y, int width, int height)
    {
        // Tree background
        ctx.Fill(SS14Colors.PanelDark, new RectangleF(x, y, width, height));
        ctx.Draw(SS14Colors.InputBorder, 1, new RectangleF(x, y, width, height));
        
        // Tree items with alternating colors (SS14 style)
        var itemHeight = 20;
        var items = new[] { "Root Item", "  Child 1", "  Child 2", "Another Root", "  Nested Item" };
        
        for (int i = 0; i < Math.Min(items.Length, height / itemHeight); i++)
        {
            var itemY = y + i * itemHeight;
            var bgColor = i % 2 == 0 ? SS14Colors.TreeEvenRow : SS14Colors.TreeOddRow;
            
            ctx.Fill(bgColor, new RectangleF(x, itemY, width, itemHeight));
            
            // Tree expand/collapse indicator for parent items
            if (!items[i].StartsWith("  "))
            {
                ctx.Fill(SS14Colors.AccentBlue, new RectangleF(x + 5, itemY + 7, 6, 6));
            }
            
            RenderText(ctx, items[i].TrimStart(), x + 15, itemY + 3, SS14Colors.TextDefault, 9);
        }
    }

    /// <summary>
    /// Renders text using simple character-based approximation
    /// </summary>
    private void RenderText(IImageProcessingContext ctx, string text, int x, int y, ImageColor color, int fontSize)
    {
        if (string.IsNullOrEmpty(text)) return;

        // Simple text rendering as filled rectangles (character approximation)
        var charWidth = Math.Max(1, fontSize * 0.6f);
        var charHeight = fontSize;
        
        for (int i = 0; i < Math.Min(text.Length, 50); i++) // Limit text length
        {
            if (text[i] == ' ') continue; // Skip spaces
            
            var charX = x + i * charWidth;
            ctx.Fill(color, new RectangleF(charX, y, charWidth * 0.8f, charHeight));
        }
    }
}

/// <summary>
/// Represents the structure and complexity of a XAML file
/// </summary>
public class XamlStructure
{
    public string RootType { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string? Title { get; set; }
    public bool HasTitle { get; set; }
    public bool HasButtons { get; set; }
    public bool HasLabels { get; set; }
    public bool HasInputs { get; set; }
    public bool HasContainers { get; set; }
    public bool HasTabs { get; set; }
    public bool HasScrolls { get; set; }
    public bool HasTrees { get; set; }
    public int ComplexityScore { get; set; }
    public string ActualText { get; set; } = "";
    public List<string> ButtonTexts { get; set; } = new();
    public List<string> LabelTexts { get; set; } = new();
    public int PreferredWidth { get; set; }
    public int PreferredHeight { get; set; }
}

/// <summary>
/// Simple window wrapper for controls that aren't windows themselves
/// </summary>
public class SimpleWindow : FancyWindow
{
    public Control Contents { get; }

    public SimpleWindow()
    {
        Title = "Preview Window";
        SetSize = new Vector2(800, 600);
        
        Contents = new PanelContainer();
        AddChild(Contents);
    }
}