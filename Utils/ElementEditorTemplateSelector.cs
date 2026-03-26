using System.Windows;
using System.Windows.Controls;
using Schedule1ModdingTool.ViewModels;

namespace Schedule1ModdingTool.Utils
{
    /// <summary>
    /// Chooses the appropriate editor template based on the open tab's element type.
    /// </summary>
    public class ElementEditorTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? QuestTemplate { get; set; }
        public DataTemplate? NpcTemplate { get; set; }
        public DataTemplate? ItemTemplate { get; set; }
        public DataTemplate? PhoneCallTemplate { get; set; }
        public DataTemplate? PhoneAppTemplate { get; set; }
        public DataTemplate? WorkspaceTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (item is OpenElementTab tab)
            {
                if (tab.IsWorkspace && WorkspaceTemplate != null)
                {
                    return WorkspaceTemplate;
                }

                if (tab.Quest != null && QuestTemplate != null)
                {
                    return QuestTemplate;
                }

                if (tab.Npc != null && NpcTemplate != null)
                {
                    return NpcTemplate;
                }

                if (tab.Item != null && ItemTemplate != null)
                {
                    return ItemTemplate;
                }

                if (tab.PhoneCall != null && PhoneCallTemplate != null)
                {
                    return PhoneCallTemplate;
                }

                if (tab.PhoneApp != null && PhoneAppTemplate != null)
                {
                    return PhoneAppTemplate;
                }
            }

            return base.SelectTemplate(item, container);
        }
    }
}
