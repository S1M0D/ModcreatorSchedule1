using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Schedule1ModdingTool.Models;

namespace Schedule1ModdingTool.Services
{
    /// <summary>
    /// Service for managing undo/redo functionality by tracking project state snapshots
    /// </summary>
    public class UndoRedoService
    {
        private readonly Stack<string> _undoStack = new Stack<string>();
        private readonly Stack<string> _redoStack = new Stack<string>();
        private int _maxHistorySize = 5;
        private bool _isExecutingUndoRedo = false;

        /// <summary>
        /// Gets or sets the maximum number of undo steps to keep in history
        /// </summary>
        public int MaxHistorySize
        {
            get => _maxHistorySize;
            set
            {
                // Clamp value between 1 and 50
                _maxHistorySize = System.Math.Max(1, System.Math.Min(50, value));
                // Trim stack if new size is smaller than current stack
                TrimUndoStack();
            }
        }

        /// <summary>
        /// Event raised when undo/redo state changes
        /// </summary>
        public event EventHandler? StateChanged;

        /// <summary>
        /// Gets whether an undo operation is available
        /// </summary>
        public bool CanUndo => _undoStack.Count > 0;

        /// <summary>
        /// Gets whether a redo operation is available
        /// </summary>
        public bool CanRedo => _redoStack.Count > 0;

        /// <summary>
        /// Saves the current project state as a snapshot for undo
        /// </summary>
        public void SaveSnapshot(QuestProject project)
        {
            if (_isExecutingUndoRedo)
                return;

            try
            {
                // Serialize the project to JSON
                var json = JsonConvert.SerializeObject(project, Formatting.None);
                
                // Push to undo stack
                _undoStack.Push(json);
                
                // Limit stack size
                TrimUndoStack();

                // Clear redo stack when a new change is made
                _redoStack.Clear();
                
                StateChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UndoRedoService] Failed to save snapshot: {ex.Message}");
            }
        }

        /// <summary>
        /// Restores the previous project state (undo)
        /// </summary>
        public QuestProject? Undo(QuestProject currentProject)
        {
            if (!CanUndo || _isExecutingUndoRedo)
                return null;

            try
            {
                _isExecutingUndoRedo = true;

                // Save current state to redo stack
                var currentJson = JsonConvert.SerializeObject(currentProject, Formatting.None);
                _redoStack.Push(currentJson);

                // Pop previous state from undo stack
                var previousJson = _undoStack.Pop();
                var restoredProject = JsonConvert.DeserializeObject<QuestProject>(previousJson);
                
                if (restoredProject != null)
                {
                    // Preserve file path and restore handlers
                    restoredProject.FilePath = currentProject.FilePath;
                    restoredProject.AttachExistingQuestHandlers();
                    restoredProject.AttachExistingNpcHandlers();
                    restoredProject.AttachExistingFolderHandlers();
                    restoredProject.AttachExistingResourceHandlers();
                    restoredProject.EnsureRootFolder();
                }

                StateChanged?.Invoke(this, EventArgs.Empty);
                return restoredProject;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UndoRedoService] Failed to undo: {ex.Message}");
                return null;
            }
            finally
            {
                _isExecutingUndoRedo = false;
            }
        }

        /// <summary>
        /// Restores the next project state (redo)
        /// </summary>
        public QuestProject? Redo(QuestProject currentProject)
        {
            if (!CanRedo || _isExecutingUndoRedo)
                return null;

            try
            {
                _isExecutingUndoRedo = true;

                // Save current state to undo stack
                var currentJson = JsonConvert.SerializeObject(currentProject, Formatting.None);
                _undoStack.Push(currentJson);

                // Pop next state from redo stack
                var nextJson = _redoStack.Pop();
                var restoredProject = JsonConvert.DeserializeObject<QuestProject>(nextJson);
                
                if (restoredProject != null)
                {
                    // Preserve file path and restore handlers
                    restoredProject.FilePath = currentProject.FilePath;
                    restoredProject.AttachExistingQuestHandlers();
                    restoredProject.AttachExistingNpcHandlers();
                    restoredProject.AttachExistingFolderHandlers();
                    restoredProject.AttachExistingResourceHandlers();
                    restoredProject.EnsureRootFolder();
                }

                StateChanged?.Invoke(this, EventArgs.Empty);
                return restoredProject;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UndoRedoService] Failed to redo: {ex.Message}");
                return null;
            }
            finally
            {
                _isExecutingUndoRedo = false;
            }
        }

        /// <summary>
        /// Clears all undo/redo history
        /// </summary>
        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Trims the undo stack to the maximum history size
        /// </summary>
        private void TrimUndoStack()
        {
            if (_undoStack.Count > MaxHistorySize)
            {
                var temp = new Stack<string>();
                for (int i = 0; i < MaxHistorySize; i++)
                {
                    temp.Push(_undoStack.Pop());
                }
                _undoStack.Clear();
                while (temp.Count > 0)
                {
                    _undoStack.Push(temp.Pop());
                }
            }
        }
    }
}

