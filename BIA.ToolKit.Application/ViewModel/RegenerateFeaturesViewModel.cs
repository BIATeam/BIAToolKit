namespace BIA.ToolKit.Application.ViewModel
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Domain.ModifyProject.RegenerateFeatures;

    public class RegenerateFeaturesViewModel : ObservableObject
    {
        private ObservableCollection<RegenerableEntity> entities = [];
        private bool regenerateCrud = true;
        private bool regenerateOption = true;
        private bool regenerateDto = true;
        private bool isRegenerateEnabled;

        public ObservableCollection<RegenerableEntity> Entities
        {
            get => entities;
            set
            {
                entities = value;
                RaisePropertyChanged(nameof(Entities));
                RaisePropertyChanged(nameof(HasReadyEntities));
                RaisePropertyChanged(nameof(HasIssueEntities));
                RaisePropertyChanged(nameof(IssueCount));
                RaisePropertyChanged(nameof(ReadyCount));
                RaisePropertyChanged(nameof(TotalCount));
            }
        }

        public bool RegenerateCrud
        {
            get => regenerateCrud;
            set
            {
                regenerateCrud = value;
                RaisePropertyChanged(nameof(RegenerateCrud));
                RefreshRegenerateEnabled();
            }
        }

        public bool RegenerateOption
        {
            get => regenerateOption;
            set
            {
                regenerateOption = value;
                RaisePropertyChanged(nameof(RegenerateOption));
                RefreshRegenerateEnabled();
            }
        }

        public bool RegenerateDto
        {
            get => regenerateDto;
            set
            {
                regenerateDto = value;
                RaisePropertyChanged(nameof(RegenerateDto));
                RefreshRegenerateEnabled();
            }
        }

        public bool IsRegenerateEnabled
        {
            get => isRegenerateEnabled;
            set
            {
                isRegenerateEnabled = value;
                RaisePropertyChanged(nameof(IsRegenerateEnabled));
            }
        }

        public List<RegenerableEntity> SelectedEntities =>
            Entities.Where(e => e.IsSelected && e.CanSelectEntity).ToList();

        public bool HasReadyEntities => Entities.Any(e => e.CanSelectEntity);

        public bool HasIssueEntities => Entities.Any(e => e.HasAnyIssue);

        public int IssueCount => Entities.Count(e => e.HasAnyIssue);

        public int ReadyCount => Entities.Count(e => e.CanSelectEntity);

        public int TotalCount => Entities.Count;

        public void SelectAll()
        {
            foreach (var entity in Entities.Where(e => e.CanSelectEntity))
            {
                entity.IsSelected = true;
            }

            RefreshRegenerateEnabled();
        }

        public void DeselectAll()
        {
            foreach (var entity in Entities)
            {
                entity.IsSelected = false;
            }

            RefreshRegenerateEnabled();
        }

        public void RefreshRegenerateEnabled()
        {
            IsRegenerateEnabled = SelectedEntities.Any() &&
                (RegenerateCrud || RegenerateOption || RegenerateDto);
        }

        public string GetBlockageReason(RegenerableEntity entity, RegenerateFeatureType featureType)
        {
            return RegenerateEntitySelectionRule.GetBlockageReason(entity, featureType);
        }
    }
}
