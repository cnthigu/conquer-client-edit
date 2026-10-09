using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CarniatoConquerUIEditor.Models;

namespace CarniatoConquerUIEditor.Services
{
    /// <summary>
    /// Game client service class, responsible for checking and reading client files
    /// </summary>
    public class GameClientService
    {
        /// <summary>
        /// Validates if the game client directory is valid
        /// </summary>
        /// <param name="clientPath">Client path</param>
        /// <returns>Game client information</returns>
        public GameClientInfo ValidateClientDirectory(string clientPath)
        {
            var clientInfo = new GameClientInfo
            {
                ClientPath = clientPath
            };
            
            try
            {
                // Check if directory exists
                if (!Directory.Exists(clientPath))
                {
                    clientInfo.IsValid = false;
                    return clientInfo;
                }
                
                // Check INI folder
                string iniPath = Path.Combine(clientPath, "ini");
                if (!Directory.Exists(iniPath))
                {
                    clientInfo.IsValid = false;
                    return clientInfo;
                }
                clientInfo.IniPath = iniPath;
                
                // Check ANI folder
                string aniPath = Path.Combine(clientPath, "ani");
                if (!Directory.Exists(aniPath))
                {
                    clientInfo.IsValid = false;
                    return clientInfo;
                }
                clientInfo.AniPath = aniPath;
                
                // Check required configuration files
                string guiIniPath = Path.Combine(iniPath, "GUI.ini");
                string gui800IniPath = Path.Combine(iniPath, "GUI800x600.ini");
                string controlAniPath = Path.Combine(aniPath, "Control.ani");
                string control1AniPath = Path.Combine(aniPath, "Control1.ani");
                
                if (File.Exists(guiIniPath) && File.Exists(gui800IniPath) && 
                    File.Exists(controlAniPath) && File.Exists(control1AniPath))
                {
                    clientInfo.IsValid = true;
                }
                else
                {
                    clientInfo.IsValid = false;
                }
            }
            catch (Exception)
            {
                clientInfo.IsValid = false;
            }
            
            return clientInfo;
        }
        
        /// <summary>
        /// Reads all game client configuration files
        /// </summary>
        /// <param name="clientInfo">Client information</param>
        /// <returns>Whether reading was successful</returns>
        public bool LoadClientConfigs(GameClientInfo clientInfo)
        {
            if (!clientInfo.IsValid)
                return false;
                
            try
            {
                // Read GUI.ini (1024x768 resolution configuration)
                string guiIniPath = Path.Combine(clientInfo.IniPath, "GUI.ini");
                clientInfo.GUI1024Configs = ParseIniFile(guiIniPath, clientInfo);
                
                // Read GUI800x600.ini (800x600 resolution configuration)
                string gui800IniPath = Path.Combine(clientInfo.IniPath, "GUI800x600.ini");
                clientInfo.GUI800Configs = ParseIniFile(gui800IniPath, clientInfo);
                
                // Read Control.ani (texture configuration)
                string controlAniPath = Path.Combine(clientInfo.AniPath, "Control.ani");
                clientInfo.ControlAniConfigs = ParseIniFile(controlAniPath, clientInfo);
                
                // Read Control1.ani (texture configuration)
                string control1AniPath = Path.Combine(clientInfo.AniPath, "Control1.ani");
                clientInfo.Control1AniConfigs = ParseIniFile(control1AniPath, clientInfo);
                
                // Check if configurations were read successfully
                bool hasConfigs = (clientInfo.GUI1024Configs?.Count ?? 0) > 0 ||
                                (clientInfo.GUI800Configs?.Count ?? 0) > 0 ||
                                (clientInfo.ControlAniConfigs?.Count ?? 0) > 0 ||
                                (clientInfo.Control1AniConfigs?.Count ?? 0) > 0;
                
                return hasConfigs;
            }
            catch (Exception)
            {
                return false;
            }
        }
        
        /// <summary>
        /// Parses INI format configuration file (supports ANSI encoding)
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <param name="clientInfo">Client info to store original bytes for byte-perfect saving</param>
        /// <returns>Configuration item list</returns>
        private List<UIConfig> ParseIniFile(string filePath, GameClientInfo clientInfo = null)
        {
            var configs = new List<UIConfig>();
            
            if (!File.Exists(filePath))
                return configs;
                
            try
            {
                // Use ANSI encoding (GB2312) to read file
                System.Text.Encoding encoding = System.Text.Encoding.GetEncoding("GB2312");
                string[] lines = File.ReadAllLines(filePath, encoding);
                
                // Store original bytes for byte-perfect preservation when saving
                if (clientInfo != null)
                {
                    if (clientInfo.OriginalIniBytes == null)
                    {
                        clientInfo.OriginalIniBytes = new Dictionary<string, byte[]>();
                    }
                    clientInfo.OriginalIniBytes[filePath] = File.ReadAllBytes(filePath);
                }
                UIConfig currentConfig = null;
                string currentSection = "";
                
                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();
                    
                    // Skip empty lines
                    if (string.IsNullOrEmpty(trimmedLine))
                        continue;
                    
                    // Process section and comments
                    if (trimmedLine.StartsWith("[") && trimmedLine.Contains("]"))
                    {
                        // Save previous configuration item
                        if (currentConfig != null)
                        {
                            configs.Add(currentConfig);
                        }
                        
                        // Parse section name and comment
                        int closeBracketIndex = trimmedLine.IndexOf(']');
                        currentSection = trimmedLine.Substring(1, closeBracketIndex - 1);
                        
                        // Parse window name in comment
                        string displayName = "";
                        if (trimmedLine.Length > closeBracketIndex + 1)
                        {
                            string comment = trimmedLine.Substring(closeBracketIndex + 1).Trim();
                            if (comment.StartsWith("//"))
                            {
                                displayName = comment.Substring(2).Trim();
                            }
                        }
                        
                        // Start new configuration item
                        currentConfig = new UIConfig
                        {
                            Name = currentSection,
                            DisplayName = displayName
                        };
                        continue;
                    }
                    
                    // Skip pure comment lines
                    if (trimmedLine.StartsWith(";") || trimmedLine.StartsWith("#") || trimmedLine.StartsWith("//"))
                        continue;
                    
                    // Process key-value pairs
                    if (currentConfig != null && trimmedLine.Contains("="))
                    {
                        string[] parts = trimmedLine.Split(new char[] { '=' }, 2);
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim();
                            string value = parts[1].Trim();
                            
                            // Process special coordinate properties
                            switch (key.ToLower())
                            {
                                case "x":
                                    if (int.TryParse(value, out int x))
                                        currentConfig.X = x;
                                    break;
                                case "y":
                                    if (int.TryParse(value, out int y))
                                        currentConfig.Y = y;
                                    break;
                                case "texture":
                                case "image":
                                case "file":
                                    currentConfig.TextureFile = value;
                                    break;
                                default:
                                    currentConfig.Properties[key] = value;
                                    break;
                            }
                        }
                    }
                }
                
                // Add last configuration item
                if (currentConfig != null)
                {
                    configs.Add(currentConfig);
                }
            }
            catch (Exception)
            {
                // Return empty list when parsing fails
            }
            
            return configs;
        }
        
        /// <summary>
        /// Parses UI element configuration
        /// </summary>
        /// <param name="clientInfo">Client information</param>
        /// <returns>Whether parsing was successful</returns>
        public bool ParseUIElements(GameClientInfo clientInfo)
        {
            try
            {
                // Check if configuration files were loaded
                if (clientInfo.GUI1024Configs == null || clientInfo.GUI1024Configs.Count == 0)
                {
                    return false;
                }
                
                // Parse texture configuration
                var textureConfigs = ParseTextureConfigs(clientInfo.ControlAniConfigs, clientInfo.Control1AniConfigs);
                
                // Parse position configuration and build UI tree
                clientInfo.UIElements = ParseUITree(clientInfo.GUI1024Configs, textureConfigs);
                clientInfo.UIElements800 = ParseUITree(clientInfo.GUI800Configs, textureConfigs);
                
                return clientInfo.UIElements != null && clientInfo.UIElements.Count > 0;
            }
            catch (Exception)
            {
                // Log error
                return false;
            }
        }
        
        /// <summary>
        /// Parses texture configuration
        /// </summary>
        /// <param name="controlConfigs">Control.ani configuration</param>
        /// <param name="control1Configs">Control1.ani configuration</param>
        /// <returns>Texture configuration dictionary</returns>
        private Dictionary<string, List<string>> ParseTextureConfigs(List<UIConfig> controlConfigs, List<UIConfig> control1Configs)
        {
            var textureDict = new Dictionary<string, List<string>>();
            
            // Combine texture information from both configuration files
            var allConfigs = controlConfigs.Concat(control1Configs);
            
            foreach (var config in allConfigs)
            {
                if (!textureDict.ContainsKey(config.Name))
                {
                    textureDict[config.Name] = new List<string>();
                }
                
                // Process texture files Frame0, Frame1, Frame2, etc
                foreach (var prop in config.Properties)
                {
                    if (prop.Key.StartsWith("Frame") && !string.IsNullOrEmpty(prop.Value))
                    {
                        textureDict[config.Name].Add(prop.Value);
                    }
                }
                
                // Compatibility with old TextureFile property
                if (!string.IsNullOrEmpty(config.TextureFile))
                {
                    textureDict[config.Name].Add(config.TextureFile);
                }
            }
            
            return textureDict;
        }
        
        /// <summary>
        /// Parses UI tree structure
        /// </summary>
        /// <param name="guiConfigs">GUI configuration</param>
        /// <param name="textureConfigs">Texture configuration</param>
        /// <returns>UI element list</returns>
        private List<UIElement> ParseUITree(List<UIConfig> guiConfigs, Dictionary<string, List<string>> textureConfigs)
        {
            var elements = new List<UIElement>();
            var elementDict = new Dictionary<string, UIElement>();
            
            foreach (var config in guiConfigs)
            {
                var element = CreateUIElement(config, textureConfigs);
                elements.Add(element);
                elementDict[config.Name] = element;
            }
            
            // Build parent-child relationships
            BuildParentChildRelations(elements, elementDict);
            
            // Return all elements
            return elements;
        }
        
        /// <summary>
        /// Creates UI element
        /// </summary>
        /// <param name="config">Configuration information</param>
        /// <param name="textureConfigs">Texture configuration</param>
        /// <returns>UI element</returns>
        private UIElement CreateUIElement(UIConfig config, Dictionary<string, List<string>> textureConfigs)
        {
            var element = new UIElement
            {
                ID = config.Name,
                X = config.X,
                Y = config.Y,
                Properties = new Dictionary<string, string>(config.Properties)
            };
            
            // Parse width and height
            if (config.Properties.ContainsKey("w"))
            {
                if (int.TryParse(config.Properties["w"], out int width))
                    element.Width = width;
            }
            
            if (config.Properties.ContainsKey("h"))
            {
                if (int.TryParse(config.Properties["h"], out int height))
                    element.Height = height;
            }
            
            // Parse AniSection
            if (config.Properties.ContainsKey("AniSection"))
            {
                element.AniSection = config.Properties["AniSection"];
                
                // Get texture files based on AniSection
                if (textureConfigs.ContainsKey(element.AniSection))
                {
                    element.TextureFiles = textureConfigs[element.AniSection];
                }
            }
            
            // Set DisplayName: prioritize AniSection, then DisplayName from comment
            if (!string.IsNullOrEmpty(element.AniSection))
            {
                element.DisplayName = element.AniSection;
            }
            else if (!string.IsNullOrEmpty(config.DisplayName))
            {
                element.DisplayName = config.DisplayName;
            }
            else
            {
                element.DisplayName = "Unknown";
            }
            
            // Parse element type
            element.Type = ParseElementType(config.Name);
            
            return element;
        }
        
        /// <summary>
        /// Parses element type
        /// </summary>
        /// <param name="elementId">Element ID</param>
        /// <returns>Element type</returns>
        private UIElementType ParseElementType(string elementId)
        {
            if (elementId.Contains("Dialog")) return UIElementType.Dialog;
            if (elementId.Contains("Button")) return UIElementType.Button;
            if (elementId.Contains("Image")) return UIElementType.Image;
            if (elementId.Contains("Check")) return UIElementType.Check;
            if (elementId.Contains("Slider")) return UIElementType.Slider;
            if (elementId.Contains("Picture")) return UIElementType.Picture;
            if (elementId.Contains("Light")) return UIElementType.Light;
            
            return UIElementType.Unknown;
        }
        
        /// <summary>
        /// Builds parent-child relationships
        /// </summary>
        /// <param name="elements">UI element list</param>
        /// <param name="elementDict">Element dictionary</param>
        private void BuildParentChildRelations(List<UIElement> elements, Dictionary<string, UIElement> elementDict)
        {
            foreach (var element in elements)
            {
                // Parse parent-child relationship, format ParentID-ChildID
                if (element.ID.Contains("-"))
                {
                    string[] parts = element.ID.Split('-');
                    if (parts.Length == 2)
                    {
                        string parentId = "0-" + parts[0]; // Build complete parent element ID
                        
                        // If current element is not root (doesn't start with 0-), find parent element
                        if (!element.ID.StartsWith("0-") && elementDict.ContainsKey(parentId))
                        {
                            var parent = elementDict[parentId];
                            element.Parent = parent;
                            parent.Children.Add(element);
                        }
                    }
                }
            }
        }
    }
}