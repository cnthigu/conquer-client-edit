using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CarniatoConquerUIEditor.Models;

namespace CarniatoConquerUIEditor.Services
{
    /// <summary>
    /// Service for writing INI configuration files
    /// Works with bytes to preserve special characters exactly
    /// </summary>
    public class IniWriterService
    {
        /// <summary>
        /// Saves UI configurations to INI file
        /// Only updates x= and y= values for modified sections, preserves everything else EXACTLY as-is (byte-perfect)
        /// </summary>
        /// <param name="filePath">Path to INI file</param>
        /// <param name="configs">List of UI configurations</param>
        /// <param name="originalBytes">Original file bytes to preserve special characters exactly</param>
        /// <param name="modifiedIds">Set of element IDs that were actually modified (only these will be updated)</param>
        /// <returns>Whether saving was successful</returns>
        public bool SaveIniFile(string filePath, List<UIConfig> configs, byte[] originalBytes = null, HashSet<string> modifiedIds = null)
        {
            if (string.IsNullOrEmpty(filePath) || configs == null)
                return false;

            try
            {
                // If we don't have original bytes, read from file (fallback)
                if (originalBytes == null || originalBytes.Length == 0)
                {
                    originalBytes = File.ReadAllBytes(filePath);
                }

                Encoding encoding = Encoding.GetEncoding("GB2312");
                byte[] updatedBytes = UpdateIniFromOriginalBytes(originalBytes, encoding, configs, modifiedIds);
                File.WriteAllBytes(filePath, updatedBytes);
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IniWriterService] Error saving INI: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Updates INI file by working with bytes directly to preserve special characters
        /// Only updates x= and y= lines for sections that have been modified
        /// </summary>
        private byte[] UpdateIniFromOriginalBytes(byte[] originalBytes, Encoding encoding, List<UIConfig> configs, HashSet<string> modifiedIds = null)
        {
            // If no modified IDs, return original bytes unchanged
            if (modifiedIds == null || modifiedIds.Count == 0)
            {
                return originalBytes;
            }

            // Create dictionary mapping ID -> UIConfig
            var configDict = new Dictionary<string, UIConfig>();
            foreach (var config in configs)
            {
                if (!string.IsNullOrEmpty(config.Name))
                {
                    configDict[config.Name] = config;
                }
            }
            
            // Find line boundaries directly in bytes to preserve exact positions
            var lineBoundaries = FindLineBoundaries(originalBytes);
            
            // Convert to string ONLY for parsing structure (section IDs, x=, y=)
            string content = encoding.GetString(originalBytes);
            string[] lines = content.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            
            // Build result by copying original bytes line by line, only replacing x= and y= values
            var resultParts = new List<byte[]>();
            string currentSectionId = null;
            bool sectionHasUpdates = false;
            
            for (int lineIndex = 0; lineIndex < lines.Length && lineIndex < lineBoundaries.Count; lineIndex++)
            {
                string line = lines[lineIndex];
                string trimmed = line.Trim();
                
                // Get exact byte boundaries for this line from original bytes
                var boundary = lineBoundaries[lineIndex];
                int lineStartByte = boundary.Start;
                int lineByteLength = boundary.Length;
                bool isLastLine = (lineIndex == lines.Length - 1);
                
                // Check if this is a section header [ID]
                if (trimmed.StartsWith("[") && trimmed.Contains("]"))
                {
                    // Extract ID from [ID] or [ID] //comment
                    int closeBracket = trimmed.IndexOf(']');
                    if (closeBracket > 0)
                    {
                        currentSectionId = trimmed.Substring(1, closeBracket - 1);
                        sectionHasUpdates = modifiedIds.Contains(currentSectionId) && configDict.ContainsKey(currentSectionId);
                    }
                    else
                    {
                        currentSectionId = null;
                        sectionHasUpdates = false;
                    }
                    
                    // Copy original bytes for this line exactly (including line ending if present)
                    byte[] lineBytes = new byte[lineByteLength];
                    Array.Copy(originalBytes, lineStartByte, lineBytes, 0, lineByteLength);
                    resultParts.Add(lineBytes);
                    continue;
                }

                // If we're in a section with updates, check for x= or y= lines
                if (sectionHasUpdates && currentSectionId != null && configDict.ContainsKey(currentSectionId))
                {
                    var config = configDict[currentSectionId];
                    byte[] updatedLineBytes = null;
                    
                    if (trimmed.StartsWith("x=", StringComparison.OrdinalIgnoreCase))
                    {
                        updatedLineBytes = CreateUpdatedLineBytes(line, "x", config.X, originalBytes, lineStartByte, lineByteLength, encoding);
                    }
                    else if (trimmed.StartsWith("y=", StringComparison.OrdinalIgnoreCase))
                    {
                        updatedLineBytes = CreateUpdatedLineBytes(line, "y", config.Y, originalBytes, lineStartByte, lineByteLength, encoding);
                    }
                    
                    if (updatedLineBytes != null)
                    {
                        resultParts.Add(updatedLineBytes);
                        continue;
                    }
                }

                // For all other lines, copy original bytes EXACTLY (including line ending)
                byte[] originalLineBytes = new byte[lineByteLength];
                Array.Copy(originalBytes, lineStartByte, originalLineBytes, 0, lineByteLength);
                resultParts.Add(originalLineBytes);
            }

            // Combine all parts
            int totalLength = resultParts.Sum(arr => arr.Length);
            byte[] result = new byte[totalLength];
            int offset = 0;
            foreach (byte[] part in resultParts)
            {
                Array.Copy(part, 0, result, offset, part.Length);
                offset += part.Length;
            }
            
            return result;
        }

        /// <summary>
        /// Creates updated line bytes for x= or y= lines, preserving prefix and line ending
        /// </summary>
        private byte[] CreateUpdatedLineBytes(string originalLine, string key, int newValue, byte[] originalBytes, 
            int lineStartByte, int lineByteLength, Encoding encoding)
        {
            // Find position of key in the line string (case-insensitive)
            int keyPos = -1;
            char keyChar = key[0];
            for (int i = 0; i < originalLine.Length; i++)
            {
                if ((char.ToLowerInvariant(originalLine[i]) == keyChar) && 
                    i + 1 < originalLine.Length && originalLine[i + 1] == '=')
                {
                    keyPos = i;
                    break;
                }
            }
            
            // Extract line ending from original bytes
            int lineContentByteLength = encoding.GetByteCount(originalLine);
            int lineEndingByteLength = lineByteLength - lineContentByteLength;
            byte[] originalLineEnding = null;
            if (lineEndingByteLength > 0)
            {
                originalLineEnding = new byte[lineEndingByteLength];
                Array.Copy(originalBytes, lineStartByte + lineContentByteLength, originalLineEnding, 0, lineEndingByteLength);
            }
            
            // Create new value bytes
            byte[] newValueBytes = encoding.GetBytes($"{key}={newValue}");
            byte[] newLineBytes;
            
            if (keyPos >= 0)
            {
                // Preserve prefix from original line
                string prefix = originalLine.Substring(0, keyPos);
                byte[] prefixBytes = encoding.GetBytes(prefix);
                
                // Combine: prefix + new value + line ending
                int newLineLength = prefixBytes.Length + newValueBytes.Length + (originalLineEnding?.Length ?? 0);
                newLineBytes = new byte[newLineLength];
                int offset = 0;
                Array.Copy(prefixBytes, 0, newLineBytes, offset, prefixBytes.Length);
                offset += prefixBytes.Length;
                Array.Copy(newValueBytes, 0, newLineBytes, offset, newValueBytes.Length);
                offset += newValueBytes.Length;
                if (originalLineEnding != null)
                {
                    Array.Copy(originalLineEnding, 0, newLineBytes, offset, originalLineEnding.Length);
                }
            }
            else
            {
                // No prefix, just value + line ending
                int newLineLength = newValueBytes.Length + (originalLineEnding?.Length ?? 0);
                newLineBytes = new byte[newLineLength];
                int offset = 0;
                Array.Copy(newValueBytes, 0, newLineBytes, offset, newValueBytes.Length);
                offset += newValueBytes.Length;
                if (originalLineEnding != null)
                {
                    Array.Copy(originalLineEnding, 0, newLineBytes, offset, originalLineEnding.Length);
                }
            }
            
            return newLineBytes;
        }

        /// <summary>
        /// Finds line boundaries directly in bytes
        /// </summary>
        private List<(int Start, int Length)> FindLineBoundaries(byte[] bytes)
        {
            var boundaries = new List<(int Start, int Length)>();
            int start = 0;
            
            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] == 0x0D && i + 1 < bytes.Length && bytes[i + 1] == 0x0A) // \r\n
                {
                    boundaries.Add((start, i - start + 2)); // Include \r\n
                    start = i + 2;
                    i++; // Skip \n
                }
                else if (bytes[i] == 0x0A) // \n
                {
                    boundaries.Add((start, i - start + 1)); // Include \n
                    start = i + 1;
                }
                else if (bytes[i] == 0x0D) // \r (standalone)
                {
                    boundaries.Add((start, i - start + 1)); // Include \r
                    start = i + 1;
                }
            }
            
            // Add last line if file doesn't end with line ending
            if (start < bytes.Length)
            {
                boundaries.Add((start, bytes.Length - start));
            }
            
            return boundaries;
        }

    }
}
