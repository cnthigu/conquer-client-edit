using System.Collections.Generic;

namespace CarniatoConquerUIEditor.Models
{
    /// <summary>
    /// UI element type
    /// </summary>
    public enum UIElementType
    {
        Dialog,
        Button,
        Image,
        Check,
        Slider,
        Picture,
        Light,
        Unknown
    }

    /// <summary>
    /// UI element information
    /// </summary>
    public class UIElement
    {
        public string ID { get; set; }
        public UIElementType Type { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string AniSection { get; set; }
        public List<string> TextureFiles { get; set; } = new List<string>();
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
        public List<UIElement> Children { get; set; } = new List<UIElement>();
        public UIElement Parent { get; set; }
        
        /// <summary>
        /// Whether it is a root UI form
        /// </summary>
        public bool IsRootDialog => ID.StartsWith("0-") && Type == UIElementType.Dialog;
    }
}