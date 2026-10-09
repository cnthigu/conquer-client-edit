using System;
using System.Collections.Generic;

namespace CarniatoConquerUIEditor.Models
{
    /// <summary>
    /// Game client information model
    /// </summary>
    public class GameClientInfo
    {
        /// <summary>
        /// Client root directory path
        /// </summary>
        public string ClientPath { get; set; }
        
        /// <summary>
        /// INI folder path
        /// </summary>
        public string IniPath { get; set; }
        
        /// <summary>
        /// ANI folder path
        /// </summary>
        public string AniPath { get; set; }
        
        /// <summary>
        /// Whether it is a valid game client directory
        /// </summary>
        public bool IsValid { get; set; }
        
        /// <summary>
        /// UI element tree for 1024x768 resolution
        /// </summary>
        public List<UIElement> UIElements { get; set; } = new List<UIElement>();
        
        /// <summary>
        /// UI element tree for 800x600 resolution
        /// </summary>
        public List<UIElement> UIElements800 { get; set; } = new List<UIElement>();
        
        /// <summary>
        /// GUI.ini configuration (1024x768)
        /// </summary>
        public List<UIConfig> GUI1024Configs { get; set; }
        
        /// <summary>
        /// GUI800x600.ini configuration (800x600)
        /// </summary>
        public List<UIConfig> GUI800Configs { get; set; }
        
        /// <summary>
        /// Control.ani configuration
        /// </summary>
        public List<UIConfig> ControlAniConfigs { get; set; }
        
        /// <summary>
        /// Control1.ani configuration
        /// </summary>
        public List<UIConfig> Control1AniConfigs { get; set; }
        
        /// <summary>
        /// Original INI file bytes (for byte-perfect preservation of special characters)
        /// </summary>
        public Dictionary<string, byte[]> OriginalIniBytes { get; set; }
        
        public GameClientInfo()
        {
            GUI1024Configs = new List<UIConfig>();
            GUI800Configs = new List<UIConfig>();
            ControlAniConfigs = new List<UIConfig>();
            Control1AniConfigs = new List<UIConfig>();
        }
    }
}