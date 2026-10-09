using System;
using System.Collections.Generic;

namespace CarniatoConquerUIEditor.Models
{
    /// <summary>
    /// UI configuration information model
    /// </summary>
    public class UIConfig
    {
        /// <summary>
        /// Configuration item name
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// X coordinate
        /// </summary>
        public int X { get; set; }
        
        /// <summary>
        /// Y coordinate
        /// </summary>
        public int Y { get; set; }
        
        /// <summary>
        /// Associated texture file
        /// </summary>
        public string TextureFile { get; set; }
        
        /// <summary>
        /// Window display name (parsed from comment)
        /// </summary>
        public string DisplayName { get; set; }
        
        /// <summary>
        /// Other properties
        /// </summary>
        public Dictionary<string, string> Properties { get; set; }
        
        public UIConfig()
        {
            Properties = new Dictionary<string, string>();
        }
    }
}