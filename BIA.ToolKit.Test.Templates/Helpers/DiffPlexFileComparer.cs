using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BIA.ToolKit.Test.Templates.Assertions;
using DiffPlex;
using DiffPlex.DiffBuilder.Model;
using DiffPlex.DiffBuilder;
using DiffPlex.Model;
using LibGit2Sharp;

namespace BIA.ToolKit.Test.Templates.Helpers
{
    public static class DiffPlexFileComparer
    {
        public sealed record FileDiffResult(
            int Modified,
            int Moved,
            int Added,
            int Deleted,
            bool AreEqual);

        /// <summary>
        /// Compare two files using DiffPlex's side-by-side alignment.
        /// Rules:
        /// - Unchanged: both sides present, marked as Unchanged with identical text.
        /// - Modified: both sides present, different text (aligned substitution).
        /// - Added: line only on the new side.
        /// - Deleted: line only on the old side.
        /// - Moved: matched pairs of (Deleted + Added) with identical text.
        ///
        /// Priority: Unchanged -> Modified -> Added/Deleted -> Moved (transform Add+Del into Move).
        /// </summary>
        public static FileDiffResult CompareFiles(string referencePath, string generatedPath)
        {
            var oldText = File.ReadAllText(referencePath);
            var newText = File.ReadAllText(generatedPath);

            // Fast path: exact same content.
            if (string.Equals(oldText, newText, StringComparison.Ordinal))
            {
                return new FileDiffResult(0, 0, 0, 0, true);
            }

            var differ = new Differ();
            var builder = new SideBySideDiffBuilder(differ);
            var model = builder.BuildDiffModel(oldText, newText);

            int modified = 0;

            // We'll collect raw added/deleted texts, then post-process into "moved".
            var addedTexts = new Dictionary<string, int>(StringComparer.Ordinal);
            var deletedTexts = new Dictionary<string, int>(StringComparer.Ordinal);

            var oldLines = model.OldText.Lines;
            var newLines = model.NewText.Lines;
            int lineCount = Math.Max(oldLines.Count, newLines.Count);

            for (int i = 0; i < lineCount; i++)
            {
                var oldLine = i < oldLines.Count ? oldLines[i] : new DiffPiece(string.Empty, ChangeType.Imaginary, 0);
                var newLine = i < newLines.Count ? newLines[i] : new DiffPiece(string.Empty, ChangeType.Imaginary, 0);

                bool hasOld = oldLine.Type != ChangeType.Imaginary;
                bool hasNew = newLine.Type != ChangeType.Imaginary;

                var oldTextLine = hasOld ? oldLine.Text ?? string.Empty : null;
                var newTextLine = hasNew ? newLine.Text ?? string.Empty : null;

                if (hasOld && hasNew)
                {
                    // Both sides have a line at this aligned position.
                    if (oldLine.Type == ChangeType.Unchanged &&
                        newLine.Type == ChangeType.Unchanged &&
                        string.Equals(oldTextLine, newTextLine, StringComparison.Ordinal))
                    {
                        // Unchanged: ignore.
                        continue;
                    }

                    // Treat as a modification when aligned lines differ.
                    // (This includes ChangeType.Modified or Inserted/Deleted aligned as substitution.)
                    if (!string.Equals(oldTextLine, newTextLine, StringComparison.Ordinal))
                    {
                        modified++;
                    }
                    // If texts are equal here but flagged differently by DiffPlex, we ignore;
                    // it's usually an alignment artifact and not semantically different.
                }
                else if (hasOld && !hasNew)
                {
                    // Pure deletion at this position.
                    IncrementCount(deletedTexts, oldTextLine);
                }
                else if (!hasOld && hasNew)
                {
                    // Pure addition at this position.
                    IncrementCount(addedTexts, newTextLine);
                }
            }

            // Convert matching (Deleted + Added) pairs with same text into "Moved".
            int moved = 0;

            foreach (var kvp in deletedTexts)
            {
                var text = kvp.Key;
                var deletedCount = kvp.Value;

                if (addedTexts.TryGetValue(text, out var addedCount) && addedCount > 0)
                {
                    int match = Math.Min(deletedCount, addedCount);
                    if (match > 0)
                    {
                        moved += match;
                        deletedTexts[text] -= match;
                        addedTexts[text] -= match;
                    }
                }
            }

            int deleted = deletedTexts.Values.Sum();
            int added = addedTexts.Values.Sum();

            bool areEqual = modified == 0 && moved == 0 && added == 0 && deleted == 0;

            return new FileDiffResult(modified, moved, added, deleted, areEqual);
        }

        private static void IncrementCount(Dictionary<string, int> map, string key)
        {
            key ??= string.Empty;

            if (map.TryGetValue(key, out var count))
            {
                map[key] = count + 1;
            }
            else
            {
                map[key] = 1;
            }
        }
    }
}
