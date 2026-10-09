using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CarniatoConquerUIEditor.Models;
using CarniatoConquerUIEditor.Services;

namespace CarniatoConquerUIEditor
{
    public partial class MainForm : Form
    {
        private GameClientService _gameClientService;
        private GameClientInfo _currentClientInfo;
        private IniWriterService _iniWriterService;
        
        // Dictionary to map controls to UI elements for drag and drop
        private Dictionary<Control, UIElement> _controlToElementMap = new Dictionary<Control, UIElement>();
        
        // Track which element IDs have been modified (moved)
        private HashSet<string> _modifiedElementIds = new HashSet<string>();
        
        // Drag and drop variables
        private Control _draggedControl;
        private Point _dragStartPoint;
        private bool _isDragging = false;
        
        /// <summary>
        /// MainForm constructor
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            _gameClientService = new GameClientService();
            _iniWriterService = new IniWriterService();
            InitializeUI();
        }
        
        /// <summary>
        /// Initializes the user interface
        /// </summary>
        private void InitializeUI()
        {
            this.Text = "Carniato Conquer UI Editor";
            
            // Set status text
            textBox1.Text = "Select the game client directory";
            textBox1.ReadOnly = true;
            
            // Set UI list selection event
            listGameUI.SelectedIndexChanged += ListGameUI_SelectedIndexChanged;
            
            // Set cpGameUIPanel to 1024x768
            cpGameUIPanel.Size = new Size(1024, 768);
            cpGameUIPanel.BackColor = Color.LightGray;
        }
        
        /// <summary>
        /// Button 1 click event - Select client directory
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                LoadGameClient(folderBrowserDialog1.SelectedPath);
            }
        }
        
        private void MainForm_Load(object sender, EventArgs e)
        {
            // Event handler required by designer
        }
        
        /// <summary>
        /// UI list selection change event
        /// </summary>
        private void ListGameUI_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listGameUI.SelectedItem != null)
            {
                DisplayUIElement(listGameUI.SelectedItem.ToString());
            }
        }
        
        /// <summary>
        /// Loads the game client
        /// </summary>
        /// <param name="clientPath">Client path</param>
        private void LoadGameClient(string clientPath)
        {
            try
            {
                textBox1.Text = "Validating game client directory...";
                textBox1.ForeColor = Color.Blue;
                Application.DoEvents();
                
                // Validate client directory
                _currentClientInfo = _gameClientService.ValidateClientDirectory(clientPath);
                
                if (!_currentClientInfo.IsValid)
                {
                    textBox1.Text = "Invalid client directory: missing required folders or configuration files";
                    textBox1.ForeColor = Color.Red;
                    MessageBox.Show("The selected directory is not a valid game client!\n\nMake sure the directory contains:\n- ini folder (with GUI.ini and GUI800x600.ini)\n- ani folder (with Control.ani and Control1.ani)", 
                                  "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                textBox1.Text = "Reading configuration files...";
                Application.DoEvents();
                
                // Read configuration files
                if (_gameClientService.LoadClientConfigs(_currentClientInfo))
                {
                    textBox1.Text = "Parsing UI elements...";
                    Application.DoEvents();
                    
                    // Parse UI elements
                    if (_gameClientService.ParseUIElements(_currentClientInfo))
                    {
                        int uiCount = _currentClientInfo.UIElements?.Count ?? 0;
                        textBox1.Text = $"Game client loaded successfully: {clientPath}\nFound {uiCount} UI elements";
                        textBox1.ForeColor = Color.Green;
                        
                        // Enable save button
                        buttonSave.Enabled = true;
                        
                        // Update UI list
                        UpdateUIList();
                        
                        if (uiCount == 0)
                        {
                            MessageBox.Show("Warning: No UI elements found!\n\nPossible causes:\n1. Configuration file format is incorrect\n2. Configuration files are empty\n3. Parsing logic error", 
                                          "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    else
                    {
                        textBox1.Text = "Failed to parse UI elements";
                        textBox1.ForeColor = Color.Red;
                        MessageBox.Show("Error parsing UI elements!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    textBox1.Text = "Failed to read configuration files";
                    textBox1.ForeColor = Color.Red;
                    MessageBox.Show("Error reading configuration files!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                textBox1.Text = "Load failed";
                textBox1.ForeColor = Color.Red;
                MessageBox.Show($"Error loading game client:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// Displays UI element (string overload)
        /// </summary>
        /// <param name="displayText">Display text</param>
        private void DisplayUIElement(string displayText)
        {
            var element = FindUIElementByDisplayText(displayText);
            if (element != null)
            {
                DisplayUIElement(element);
            }
        }

        /// <summary>
        /// Displays UI element
        /// </summary>
        /// <param name="element">UI element</param>
        private void DisplayUIElement(UIElement element)
        {
            // Clear panel and map (keep _modifiedElementIds to track moved elements)
            cpGameUIPanel.Controls.Clear();
            _controlToElementMap.Clear();
            
            // Create visual representation of UI element
            var elementControl = CreateUIElementControl(element);
            cpGameUIPanel.Controls.Add(elementControl);
            _controlToElementMap[elementControl] = element;
            
            // Enable drag and drop for root element
            EnableDragAndDrop(elementControl);
            
            // Add child elements recursively
            foreach (var child in element.Children)
            {
                var childControl = CreateUIElementControl(child);
                elementControl.Controls.Add(childControl);
                _controlToElementMap[childControl] = child;
                
                // Enable drag and drop for child elements
                EnableDragAndDrop(childControl);
            }
        }
        
        /// <summary>
        /// Enables drag and drop functionality for a control
        /// </summary>
        private void EnableDragAndDrop(Control control)
        {
            control.MouseDown += Control_MouseDown;
            control.MouseMove += Control_MouseMove;
            control.MouseUp += Control_MouseUp;
            control.Cursor = Cursors.Hand;
        }
        
        /// <summary>
        /// Mouse down event for dragging controls
        /// </summary>
        private void Control_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _draggedControl = sender as Control;
                _dragStartPoint = e.Location;
                _isDragging = false;
            }
        }
        
        /// <summary>
        /// Mouse move event for dragging controls
        /// </summary>
        private void Control_MouseMove(object sender, MouseEventArgs e)
        {
            if (_draggedControl != null && e.Button == MouseButtons.Left)
            {
                if (!_isDragging)
                {
                    // Check if mouse moved enough to start dragging
                    int deltaX = Math.Abs(e.X - _dragStartPoint.X);
                    int deltaY = Math.Abs(e.Y - _dragStartPoint.Y);
                    if (deltaX > 3 || deltaY > 3)
                    {
                        _isDragging = true;
                    }
                }
                
                if (_isDragging)
                {
                    // Get the parent container (panel or parent control)
                    Control parent = _draggedControl.Parent;
                    if (parent == null)
                        parent = cpGameUIPanel;
                    
                    // Calculate new position relative to parent
                    Point mousePos = parent.PointToClient(Control.MousePosition);
                    
                    // Adjust for mouse offset (where the mouse was when dragging started)
                    Point newLocation = new Point(
                        mousePos.X - _dragStartPoint.X,
                        mousePos.Y - _dragStartPoint.Y
                    );
                    
                    // Clamp to panel bounds (1024x768) - but respect parent container
                    int maxX = parent.Width - _draggedControl.Width;
                    int maxY = parent.Height - _draggedControl.Height;
                    newLocation.X = Math.Max(0, Math.Min(newLocation.X, maxX));
                    newLocation.Y = Math.Max(0, Math.Min(newLocation.Y, maxY));
                    
                    // If parent is not the main panel, we need to calculate absolute position
                    Point absoluteLocation = newLocation;
                    if (parent != cpGameUIPanel)
                    {
                        // Get absolute position relative to main panel
                        absoluteLocation = cpGameUIPanel.PointToClient(parent.PointToScreen(newLocation));
                    }
                    
                    // Update control position
                    _draggedControl.Location = newLocation;
                    
                    // Update UI element coordinates (use absolute position for INI)
                    if (_controlToElementMap.ContainsKey(_draggedControl))
                    {
                        var element = _controlToElementMap[_draggedControl];
                        element.X = absoluteLocation.X;
                        element.Y = absoluteLocation.Y;
                        
                        // Mark this element as modified
                        _modifiedElementIds.Add(element.ID);
                        
                        // Update status
                        textBox1.Text = $"Element {element.ID} moved to ({element.X}, {element.Y})";
                    }
                }
            }
        }
        
        /// <summary>
        /// Mouse up event for dragging controls
        /// </summary>
        private void Control_MouseUp(object sender, MouseEventArgs e)
        {
            _draggedControl = null;
            _isDragging = false;
        }

        /// <summary>
        /// Creates UI element control
        /// </summary>
        /// <param name="element">UI element</param>
        /// <returns>Control</returns>
        private Control CreateUIElementControl(UIElement element)
        {
            Control control;
            
            switch (element.Type)
            {
                case UIElementType.Dialog:
                    control = new Panel()
                    {
                        BackColor = Color.LightGray,
                        BorderStyle = BorderStyle.FixedSingle
                    };
                    break;
                    
                case UIElementType.Button:
                    control = new Button()
                    {
                        Text = element.ID,
                        FlatStyle = FlatStyle.Flat
                    };
                    break;
                    
                default:
                    control = new Label()
                    {
                        Text = element.Type.ToString(),
                        BackColor = Color.White,
                        BorderStyle = BorderStyle.FixedSingle
                    };
                    break;
            }
            
            // Set position and size
            control.Location = new Point(element.X, element.Y);
            control.Size = new Size(element.Width, element.Height);
            control.Name = element.ID;
            
            // Add tooltip
            var tooltip = new ToolTip();
            tooltip.SetToolTip(control, $"ID: {element.ID}\nType: {element.Type}\nPosition: ({element.X}, {element.Y})\nSize: {element.Width}x{element.Height}\nTexture: {string.Join(", ", element.TextureFiles)}\n\nDrag to move");
            
            return control;
        }
        
        /// <summary>
        /// Saves changes to INI file
        /// </summary>
        private void SaveChanges()
        {
            if (_currentClientInfo == null || !_currentClientInfo.IsValid)
            {
                MessageBox.Show("No client loaded. Please load a client first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                textBox1.Text = "Saving changes to GUI.ini...";
                textBox1.ForeColor = Color.Blue;
                Application.DoEvents();

                // Update UIConfig from UIElement ONLY for modified elements
                UpdateConfigsFromModifiedElements();

                // Get original file bytes to preserve special characters exactly
                string guiIniPath = Path.Combine(_currentClientInfo.IniPath, "GUI.ini");
                
                byte[] originalBytes = null;
                if (_currentClientInfo.OriginalIniBytes != null && _currentClientInfo.OriginalIniBytes.ContainsKey(guiIniPath))
                {
                    originalBytes = _currentClientInfo.OriginalIniBytes[guiIniPath];
                }
                else if (File.Exists(guiIniPath))
                {
                    originalBytes = File.ReadAllBytes(guiIniPath);
                }

                if (originalBytes == null || originalBytes.Length == 0)
                {
                    textBox1.Text = "Error: Could not read original file";
                    textBox1.ForeColor = Color.Red;
                    MessageBox.Show($"Could not read original file:\n{guiIniPath}\n\nPlease reload the client.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                // Save GUI.ini (1024x768) - pass original bytes and only modified IDs
                if (_iniWriterService.SaveIniFile(guiIniPath, _currentClientInfo.GUI1024Configs, originalBytes, _modifiedElementIds))
                {
                    textBox1.Text = "Changes saved successfully to GUI.ini";
                    textBox1.ForeColor = Color.Green;
                    MessageBox.Show("Changes saved successfully to GUI.ini", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    textBox1.Text = "Failed to save changes";
                    textBox1.ForeColor = Color.Red;
                    MessageBox.Show("Failed to save changes to GUI.ini", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                textBox1.Text = "Error saving changes";
                textBox1.ForeColor = Color.Red;
                MessageBox.Show($"Error saving changes:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Updates UIConfig coordinates ONLY for modified elements
        /// </summary>
        private void UpdateConfigsFromModifiedElements()
        {
            if (_currentClientInfo?.GUI1024Configs == null || _currentClientInfo.UIElements == null)
                return;

            if (_modifiedElementIds.Count == 0)
            {
                return;
            }

            // Create dictionary for quick lookup
            var configDict = new Dictionary<string, UIConfig>();
            foreach (var config in _currentClientInfo.GUI1024Configs)
            {
                if (!string.IsNullOrEmpty(config.Name))
                {
                    configDict[config.Name] = config;
                }
            }

            // Update ONLY modified elements
            UpdateModifiedElementCoordinates(_currentClientInfo.UIElements, configDict);
        }

        /// <summary>
        /// Recursively updates coordinates ONLY for modified elements
        /// </summary>
        private void UpdateModifiedElementCoordinates(List<UIElement> elements, Dictionary<string, UIConfig> configDict)
        {
            foreach (var element in elements)
            {
                // Only update if this element was modified
                if (!string.IsNullOrEmpty(element.ID) && _modifiedElementIds.Contains(element.ID) && configDict.ContainsKey(element.ID))
                {
                    var config = configDict[element.ID];
                    config.X = element.X;
                    config.Y = element.Y;
                }

                // Check children recursively
                if (element.Children != null && element.Children.Count > 0)
                {
                    UpdateModifiedElementCoordinates(element.Children, configDict);
                }
            }
        }

        /// <summary>
        /// Updates the UI list
        /// </summary>
        private void UpdateUIList()
        {
            if (_currentClientInfo?.UIElements == null) 
            {
                return;
            }
            
            listGameUI.Items.Clear();
            
            // Display all root elements
            foreach (var element in _currentClientInfo.UIElements)
            {
                AddUIElementToList(element, 0);
            }
        }

        /// <summary>
        /// Recursively adds UI element to list
        /// </summary>
        /// <param name="element">UI element</param>
        /// <param name="level">Level</param>
        private void AddUIElementToList(UIElement element, int level)
        {
            string indent = new string(' ', level * 2);
            
            // Build display text: prioritize AniSection, format "AniSection - ID" or "AniSection (comment) - ID"
            string displayText;
            
            if (!string.IsNullOrEmpty(element.AniSection))
            {
                // If has AniSection, prioritize AniSection
                if (!string.IsNullOrEmpty(element.DisplayName) && element.DisplayName != element.AniSection)
                {
                    // Has comment and comment different from AniSection: AniSection (comment) - ID
                    displayText = $"{element.AniSection} ({element.DisplayName}) - {element.ID}";
                }
                else
                {
                    // No comment or comment equals AniSection: AniSection - ID
                    displayText = $"{element.AniSection} - {element.ID}";
                }
            }
            else if (!string.IsNullOrEmpty(element.DisplayName))
            {
                // No AniSection but has DisplayName: DisplayName - ID
                displayText = $"{element.DisplayName} - {element.ID}";
            }
            else
            {
                // Has neither: Type - ID
                displayText = $"{element.Type} - {element.ID}";
            }
                
            listGameUI.Items.Add($"{indent}{displayText}");
            
            foreach (var child in element.Children)
            {
                AddUIElementToList(child, level + 1);
            }
        }
        
        /// <summary>
        /// Finds UI element by display text
        /// </summary>
        /// <param name="displayText">Display text</param>
        /// <returns>UI element</returns>
        private UIElement FindUIElementByDisplayText(string displayText)
        {
            if (_currentClientInfo?.UIElements == null) return null;
            
            // Remove indentation spaces
            string cleanText = displayText.TrimStart();
            
            // Parse new format: AniSection - ID or AniSection (comment) - ID
            if (cleanText.Contains(" - "))
            {
                string[] parts = cleanText.Split(new string[] { " - " }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    // parts[1] is the ID
                    string elementId = parts[1].Trim();
                    
                    return FindUIElementById(_currentClientInfo.UIElements, elementId);
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Recursively finds UI element by ID
        /// </summary>
        /// <param name="elements">UI element list</param>
        /// <param name="elementId">Element ID</param>
        /// <returns>UI element</returns>
        private UIElement FindUIElementById(List<UIElement> elements, string elementId)
        {
            foreach (var element in elements)
            {
                if (element.ID == elementId)
                    return element;
                    
                var found = FindUIElementById(element.Children, elementId);
                if (found != null)
                    return found;
            }
            
            return null;
        }

        /// <summary>
        /// Save button click event
        /// </summary>
        private void buttonSave_Click(object sender, EventArgs e)
        {
            SaveChanges();
        }
    }
}
