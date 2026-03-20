using System.IO;
using System.Text;
using System.Windows;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Utils;
using Schedule1ModdingTool.ViewModels;

namespace Schedule1ModdingTool.Services
{
    /// <summary>
    /// Manages CRUD operations for quests, NPCs, items, phone apps, and folders.
    /// </summary>
    public class ElementManagementService
    {
        private readonly WorkspaceViewModel _workspaceViewModel;

        public ElementManagementService(WorkspaceViewModel workspaceViewModel)
        {
            _workspaceViewModel = workspaceViewModel;
        }

        /// <summary>
        /// Adds a new quest to the project based on a template.
        /// </summary>
        /// <param name="project">The current project.</param>
        /// <param name="template">The quest template to use.</param>
        /// <returns>The created quest.</returns>
        public QuestBlueprint? AddQuest(QuestProject project, QuestBlueprint? template)
        {
            if (template == null) return null;

            var settings = ModSettings.Load();
            // Use project namespace if set, otherwise fall back to settings
            var projectNamespace = !string.IsNullOrWhiteSpace(project.ProjectNamespace)
                ? project.ProjectNamespace
                : settings.DefaultModNamespace;
            var quest = new QuestBlueprint(template.BlueprintType)
            {
                ClassName = $"Quest{project.Quests.Count + 1}",
                QuestTitle = $"New Quest {project.Quests.Count + 1}",
                QuestDescription = "A new quest for Schedule 1",
                BlueprintType = template.BlueprintType,
                Namespace = $"{projectNamespace}.Quests",
                ModName = project.ProjectName,
                ModAuthor = settings.DefaultModAuthor,
                ModVersion = settings.DefaultModVersion,
                FolderId = _workspaceViewModel.SelectedFolder?.Id ?? QuestProject.RootFolderId
            };

            project.AddQuest(quest);
            _workspaceViewModel.UpdateQuestCount(project.Quests.Count);
            return quest;
        }

        /// <summary>
        /// Removes a quest from the project after confirmation.
        /// </summary>
        /// <param name="project">The current project.</param>
        /// <param name="quest">The quest to remove.</param>
        /// <returns>True if the quest was removed, false otherwise.</returns>
        public bool RemoveQuest(QuestProject project, QuestBlueprint quest)
        {
            if (quest == null) return false;

            var result = MessageBox.Show($"Are you sure you want to remove '{quest.DisplayName}'?",
                "Remove Quest", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Delete the generated C# file before removing from project
                DeleteGeneratedQuestFile(project, quest);
                
                project.RemoveQuest(quest);
                _workspaceViewModel.UpdateQuestCount(project.Quests.Count);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Duplicates an existing quest.
        /// </summary>
        /// <param name="project">The current project.</param>
        /// <param name="quest">The quest to duplicate.</param>
        /// <returns>The duplicated quest.</returns>
        public QuestBlueprint? DuplicateQuest(QuestProject project, QuestBlueprint? quest)
        {
            if (quest == null) return null;

            var duplicate = quest.DeepCopy();
            duplicate.ClassName = $"{duplicate.ClassName}Copy";
            duplicate.QuestTitle = $"{duplicate.QuestTitle} (Copy)";
            duplicate.QuestId = $"{duplicate.QuestId}_copy";
            duplicate.FolderId = quest.FolderId; // Keep in same folder

            project.AddQuest(duplicate);
            _workspaceViewModel.UpdateQuestCount(project.Quests.Count);
            return duplicate;
        }

        /// <summary>
        /// Adds a new NPC to the project based on a template.
        /// </summary>
        /// <param name="project">The current project.</param>
        /// <param name="template">The NPC template to use.</param>
        /// <returns>The created NPC.</returns>
        public NpcBlueprint AddNpc(QuestProject project, NpcBlueprint? template)
        {
            var settings = ModSettings.Load();
            // Use project namespace if set, otherwise fall back to settings
            var projectNamespace = !string.IsNullOrWhiteSpace(project.ProjectNamespace)
                ? project.ProjectNamespace
                : settings.DefaultModNamespace;
            var npc = template?.DeepCopy() ?? new NpcBlueprint();
            npc.ClassName = $"Npc{project.Npcs.Count + 1}";
            npc.NpcId = $"npc_{project.Npcs.Count + 1}";
            npc.FirstName = string.IsNullOrWhiteSpace(npc.FirstName) ? "New" : npc.FirstName;
            npc.LastName = string.IsNullOrWhiteSpace(npc.LastName) ? "NPC" : npc.LastName;
            npc.Namespace = $"{projectNamespace}.NPCs";
            npc.ModName = string.IsNullOrWhiteSpace(project.ProjectName) ? npc.ModName : project.ProjectName;
            npc.ModAuthor = settings.DefaultModAuthor;
            npc.ModVersion = settings.DefaultModVersion;
            npc.FolderId = _workspaceViewModel.SelectedFolder?.Id ?? QuestProject.RootFolderId;

            project.AddNpc(npc);
            _workspaceViewModel.UpdateNpcCount(project.Npcs.Count);
            return npc;
        }

        /// <summary>
        /// Removes an NPC from the project after confirmation.
        /// </summary>
        /// <param name="project">The current project.</param>
        /// <param name="npc">The NPC to remove.</param>
        /// <returns>True if the NPC was removed, false otherwise.</returns>
        public bool RemoveNpc(QuestProject project, NpcBlueprint npc)
        {
            if (npc == null) return false;

            var result = MessageBox.Show($"Are you sure you want to remove '{npc.DisplayName}'?",
                "Remove NPC", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Delete the generated C# file before removing from project
                DeleteGeneratedNpcFile(project, npc);
                
                project.RemoveNpc(npc);
                _workspaceViewModel.UpdateNpcCount(project.Npcs.Count);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Duplicates an existing NPC.
        /// </summary>
        /// <param name="project">The current project.</param>
        /// <param name="npc">The NPC to duplicate.</param>
        /// <returns>The duplicated NPC.</returns>
        public NpcBlueprint? DuplicateNpc(QuestProject project, NpcBlueprint? npc)
        {
            if (npc == null) return null;

            var duplicate = npc.DeepCopy();
            duplicate.ClassName = $"{duplicate.ClassName}Copy";
            duplicate.FirstName = duplicate.FirstName;
            duplicate.LastName = $"{duplicate.LastName} (Copy)";
            duplicate.NpcId = $"{duplicate.NpcId}_copy";
            duplicate.FolderId = npc.FolderId; // Keep in same folder

            project.AddNpc(duplicate);
            _workspaceViewModel.UpdateNpcCount(project.Npcs.Count);
            return duplicate;
        }

        /// <summary>
        /// Adds a new item to the project based on a template.
        /// </summary>
        public ItemBlueprint AddItem(QuestProject project, ItemBlueprint? template)
        {
            var settings = ModSettings.Load();
            var projectNamespace = !string.IsNullOrWhiteSpace(project.ProjectNamespace)
                ? project.ProjectNamespace
                : settings.DefaultModNamespace;
            var item = template?.DeepCopy() ?? new ItemBlueprint();
            item.ClassName = $"Item{project.Items.Count + 1}";
            item.ItemId = $"item_{project.Items.Count + 1}";
            item.ItemName = $"New Item {project.Items.Count + 1}";
            item.Namespace = $"{projectNamespace}.Items";
            item.ModName = string.IsNullOrWhiteSpace(project.ProjectName) ? item.ModName : project.ProjectName;
            item.ModAuthor = settings.DefaultModAuthor;
            item.ModVersion = settings.DefaultModVersion;
            item.FolderId = _workspaceViewModel.SelectedFolder?.Id ?? QuestProject.RootFolderId;

            project.AddItem(item);
            _workspaceViewModel.UpdateItemCount(project.Items.Count);
            return item;
        }

        /// <summary>
        /// Removes an item from the project after confirmation.
        /// </summary>
        public bool RemoveItem(QuestProject project, ItemBlueprint item)
        {
            if (item == null) return false;

            var result = MessageBox.Show($"Are you sure you want to remove '{item.DisplayName}'?",
                "Remove Item", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                DeleteGeneratedItemFile(project, item);
                project.RemoveItem(item);
                _workspaceViewModel.UpdateItemCount(project.Items.Count);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Duplicates an existing item.
        /// </summary>
        public ItemBlueprint? DuplicateItem(QuestProject project, ItemBlueprint? item)
        {
            if (item == null) return null;

            var duplicate = item.DeepCopy();
            duplicate.ClassName = $"{duplicate.ClassName}Copy";
            duplicate.ItemId = $"{duplicate.ItemId}_copy";
            duplicate.ItemName = $"{duplicate.ItemName} (Copy)";
            duplicate.FolderId = item.FolderId;

            project.AddItem(duplicate);
            _workspaceViewModel.UpdateItemCount(project.Items.Count);
            return duplicate;
        }

        /// <summary>
        /// Adds a new phone app to the project based on a template.
        /// </summary>
        public PhoneAppBlueprint AddPhoneApp(QuestProject project, PhoneAppBlueprint? template)
        {
            var settings = ModSettings.Load();
            var projectNamespace = !string.IsNullOrWhiteSpace(project.ProjectNamespace)
                ? project.ProjectNamespace
                : settings.DefaultModNamespace;
            var phoneApp = template?.DeepCopy() ?? new PhoneAppBlueprint();
            phoneApp.ClassName = $"PhoneApp{project.PhoneApps.Count + 1}";
            phoneApp.AppName = $"phone_app_{project.PhoneApps.Count + 1}";
            phoneApp.AppTitle = $"New Phone App {project.PhoneApps.Count + 1}";
            phoneApp.Namespace = $"{projectNamespace}.PhoneApps";
            phoneApp.ModName = string.IsNullOrWhiteSpace(project.ProjectName) ? phoneApp.ModName : project.ProjectName;
            phoneApp.ModAuthor = settings.DefaultModAuthor;
            phoneApp.ModVersion = settings.DefaultModVersion;
            phoneApp.FolderId = _workspaceViewModel.SelectedFolder?.Id ?? QuestProject.RootFolderId;

            project.AddPhoneApp(phoneApp);
            _workspaceViewModel.UpdatePhoneAppCount(project.PhoneApps.Count);
            return phoneApp;
        }

        /// <summary>
        /// Removes a phone app from the project after confirmation.
        /// </summary>
        public bool RemovePhoneApp(QuestProject project, PhoneAppBlueprint phoneApp)
        {
            if (phoneApp == null) return false;

            var result = MessageBox.Show($"Are you sure you want to remove '{phoneApp.DisplayName}'?",
                "Remove Phone App", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                DeleteGeneratedPhoneAppFile(project, phoneApp);
                project.RemovePhoneApp(phoneApp);
                _workspaceViewModel.UpdatePhoneAppCount(project.PhoneApps.Count);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Duplicates an existing phone app.
        /// </summary>
        public PhoneAppBlueprint? DuplicatePhoneApp(QuestProject project, PhoneAppBlueprint? phoneApp)
        {
            if (phoneApp == null) return null;

            var duplicate = phoneApp.DeepCopy();
            duplicate.ClassName = $"{duplicate.ClassName}Copy";
            duplicate.AppName = $"{duplicate.AppName}_copy";
            duplicate.AppTitle = $"{duplicate.AppTitle} (Copy)";
            duplicate.FolderId = phoneApp.FolderId;

            project.AddPhoneApp(duplicate);
            _workspaceViewModel.UpdatePhoneAppCount(project.PhoneApps.Count);
            return duplicate;
        }

        /// <summary>
        /// Creates a new folder in the project.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if folder creation fails.</exception>
        public void CreateFolder()
        {
            _workspaceViewModel.CreateFolder();
        }

        /// <summary>
        /// Duplicates a folder.
        /// </summary>
        /// <param name="project">The current project.</param>
        /// <param name="folder">The folder to duplicate.</param>
        public void DuplicateFolder(QuestProject project, ModFolder? folder)
        {
            if (folder == null || project == null) return;

            try
            {
                var duplicate = new ModFolder
                {
                    Name = $"{folder.Name} (Copy)",
                    ParentId = folder.ParentId
                };

                project.Folders.Add(duplicate);
                _workspaceViewModel.NavigateToFolder(duplicate);
            }
            catch (Exception ex)
            {
                AppUtils.ShowError($"Unable to duplicate folder: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a folder and optionally moves its contents to the parent folder.
        /// </summary>
        /// <param name="project">The current project.</param>
        /// <param name="folder">The folder to delete.</param>
        public void DeleteFolder(QuestProject project, ModFolder? folder)
        {
            if (folder == null || project == null) return;

            // Prevent deleting root folder
            if (string.IsNullOrWhiteSpace(folder.ParentId))
            {
                AppUtils.ShowWarning("Cannot delete the root folder.");
                return;
            }

            // Check if folder has children
            var hasChildren = project.Folders.Any(f => f.ParentId == folder.Id) ||
                             project.Quests.Any(q => q.FolderId == folder.Id) ||
                             project.Npcs.Any(n => n.FolderId == folder.Id) ||
                             project.Items.Any(i => i.FolderId == folder.Id) ||
                             project.PhoneApps.Any(app => app.FolderId == folder.Id);

            if (hasChildren)
            {
                var result = MessageBox.Show(
                    $"Folder '{folder.Name}' contains items. Do you want to delete it and move all items to its parent folder?",
                    "Delete Folder",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // Move all children to parent folder
                var parentId = folder.ParentId ?? QuestProject.RootFolderId;
                foreach (var childFolder in project.Folders.Where(f => f.ParentId == folder.Id).ToList())
                {
                    childFolder.ParentId = parentId;
                }
                foreach (var quest in project.Quests.Where(q => q.FolderId == folder.Id))
                {
                    quest.FolderId = parentId;
                }
                foreach (var npc in project.Npcs.Where(n => n.FolderId == folder.Id))
                {
                    npc.FolderId = parentId;
                }
                foreach (var item in project.Items.Where(i => i.FolderId == folder.Id))
                {
                    item.FolderId = parentId;
                }
                foreach (var phoneApp in project.PhoneApps.Where(app => app.FolderId == folder.Id))
                {
                    phoneApp.FolderId = parentId;
                }
            }
            else
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete folder '{folder.Name}'?",
                    "Delete Folder",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            // Navigate to parent folder before deleting
            var parentFolder = project.GetFolderById(folder.ParentId ?? QuestProject.RootFolderId);
            if (parentFolder != null)
            {
                _workspaceViewModel.NavigateToFolder(parentFolder);
            }

            project.Folders.Remove(folder);
        }

        /// <summary>
        /// Deletes the generated C# file for a quest.
        /// </summary>
        private void DeleteGeneratedQuestFile(QuestProject project, QuestBlueprint quest)
        {
            if (project == null || quest == null) return;
            if (string.IsNullOrWhiteSpace(project.FilePath)) return;

            try
            {
                var projectDir = Path.GetDirectoryName(project.FilePath);
                if (string.IsNullOrWhiteSpace(projectDir) || !Directory.Exists(projectDir))
                    return;

                var className = MakeSafeIdentifier(quest.ClassName, "GeneratedQuest");
                var questFilePath = Path.Combine(projectDir, "Quests", $"{className}.cs");

                if (File.Exists(questFilePath))
                {
                    File.Delete(questFilePath);
                }
            }
            catch (Exception ex)
            {
                // Log but don't show error - file deletion failure shouldn't prevent removal
                System.Diagnostics.Debug.WriteLine($"[ElementManagementService] Failed to delete quest file: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes the generated C# file for an NPC.
        /// </summary>
        private void DeleteGeneratedNpcFile(QuestProject project, NpcBlueprint npc)
        {
            if (project == null || npc == null) return;
            if (string.IsNullOrWhiteSpace(project.FilePath)) return;

            try
            {
                var projectDir = Path.GetDirectoryName(project.FilePath);
                if (string.IsNullOrWhiteSpace(projectDir) || !Directory.Exists(projectDir))
                    return;

                var className = MakeSafeIdentifier(npc.ClassName, "GeneratedNpc");
                var npcFilePath = Path.Combine(projectDir, "NPCs", $"{className}.cs");

                if (File.Exists(npcFilePath))
                {
                    File.Delete(npcFilePath);
                }
            }
            catch (Exception ex)
            {
                // Log but don't show error - file deletion failure shouldn't prevent removal
                System.Diagnostics.Debug.WriteLine($"[ElementManagementService] Failed to delete NPC file: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes the generated C# file for an item.
        /// </summary>
        private void DeleteGeneratedItemFile(QuestProject project, ItemBlueprint item)
        {
            if (project == null || item == null) return;
            if (string.IsNullOrWhiteSpace(project.FilePath)) return;

            try
            {
                var projectDir = Path.GetDirectoryName(project.FilePath);
                if (string.IsNullOrWhiteSpace(projectDir) || !Directory.Exists(projectDir))
                    return;

                var className = MakeSafeIdentifier(item.ClassName, "GeneratedItem");
                var itemFilePath = Path.Combine(projectDir, "Items", $"{className}.cs");

                if (File.Exists(itemFilePath))
                {
                    File.Delete(itemFilePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ElementManagementService] Failed to delete item file: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes the generated C# file for a phone app.
        /// </summary>
        private void DeleteGeneratedPhoneAppFile(QuestProject project, PhoneAppBlueprint phoneApp)
        {
            if (project == null || phoneApp == null) return;
            if (string.IsNullOrWhiteSpace(project.FilePath)) return;

            try
            {
                var projectDir = Path.GetDirectoryName(project.FilePath);
                if (string.IsNullOrWhiteSpace(projectDir) || !Directory.Exists(projectDir))
                    return;

                var className = MakeSafeIdentifier(phoneApp.ClassName, "GeneratedPhoneApp");
                var phoneAppFilePath = Path.Combine(projectDir, "PhoneApps", $"{className}.cs");

                if (File.Exists(phoneAppFilePath))
                {
                    File.Delete(phoneAppFilePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ElementManagementService] Failed to delete phone app file: {ex.Message}");
            }
        }

        /// <summary>
        /// Makes a safe C# identifier from a candidate string, matching the logic used in ModProjectGeneratorService.
        /// </summary>
        private static string MakeSafeIdentifier(string? candidate, string fallback)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return fallback;
            }

            var builder = new StringBuilder();
            foreach (var ch in candidate)
            {
                if (builder.Length == 0)
                {
                    if (char.IsLetter(ch) || ch == '_')
                    {
                        builder.Append(ch);
                    }
                    else if (char.IsDigit(ch))
                    {
                        builder.Append('_').Append(ch);
                    }
                }
                else
                {
                    if (char.IsLetterOrDigit(ch) || ch == '_')
                    {
                        builder.Append(ch);
                    }
                    else
                    {
                        builder.Append('_');
                    }
                }
            }

            return builder.Length > 0 ? builder.ToString() : fallback;
        }
    }
}
