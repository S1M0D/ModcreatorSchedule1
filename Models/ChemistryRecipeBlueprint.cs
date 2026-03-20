using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace Schedule1ModdingTool.Models
{
    /// <summary>
    /// Represents a Chemistry Station recipe that produces the owning item.
    /// </summary>
    public class ChemistryRecipeBlueprint : ObservableObject
    {
        private string _title = "New Chemistry Recipe";
        private int _cookTimeMinutes = 60;
        private string _finalLiquidColorHex = "#FF4BD7A8";
        private int _productQuantity = 1;

        public ChemistryRecipeBlueprint()
        {
            Ingredients.CollectionChanged += IngredientsOnCollectionChanged;
        }

        [JsonProperty("title")]
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value ?? string.Empty);
        }

        [JsonProperty("cookTimeMinutes")]
        public int CookTimeMinutes
        {
            get => _cookTimeMinutes;
            set => SetProperty(ref _cookTimeMinutes, value < 1 ? 1 : value);
        }

        [JsonProperty("finalLiquidColorHex")]
        public string FinalLiquidColorHex
        {
            get => _finalLiquidColorHex;
            set => SetProperty(ref _finalLiquidColorHex, value ?? "#FF4BD7A8");
        }

        [JsonProperty("productQuantity")]
        public int ProductQuantity
        {
            get => _productQuantity;
            set => SetProperty(ref _productQuantity, value < 1 ? 1 : value);
        }

        [JsonProperty("ingredients")]
        public ObservableCollection<ChemistryRecipeIngredientBlueprint> Ingredients { get; } = new ObservableCollection<ChemistryRecipeIngredientBlueprint>();

        [JsonIgnore]
        public string DisplayName => string.IsNullOrWhiteSpace(Title) ? "Untitled Recipe" : Title;

        public void CopyFrom(ChemistryRecipeBlueprint source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            Title = source.Title;
            CookTimeMinutes = source.CookTimeMinutes;
            FinalLiquidColorHex = source.FinalLiquidColorHex;
            ProductQuantity = source.ProductQuantity;

            Ingredients.Clear();
            foreach (var ingredient in source.Ingredients)
            {
                Ingredients.Add(ingredient.DeepCopy());
            }
        }

        public ChemistryRecipeBlueprint DeepCopy()
        {
            var copy = new ChemistryRecipeBlueprint();
            copy.CopyFrom(this);
            return copy;
        }

        private void IngredientsOnCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.OfType<ChemistryRecipeIngredientBlueprint>())
                {
                    item.PropertyChanged += IngredientOnPropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems.OfType<ChemistryRecipeIngredientBlueprint>())
                {
                    item.PropertyChanged -= IngredientOnPropertyChanged;
                }
            }

            OnPropertyChanged(nameof(Ingredients));
            OnPropertyChanged(nameof(DisplayName));
        }

        private void IngredientOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(Ingredients));
        }
    }

    /// <summary>
    /// Represents one ingredient group in a Chemistry Station recipe.
    /// </summary>
    public class ChemistryRecipeIngredientBlueprint : ObservableObject
    {
        private int _quantity = 1;

        public ChemistryRecipeIngredientBlueprint()
        {
            ItemIds.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(ItemIds));
                OnPropertyChanged(nameof(DisplayLabel));
            };
        }

        [JsonProperty("quantity")]
        public int Quantity
        {
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, value < 1 ? 1 : value))
                {
                    OnPropertyChanged(nameof(DisplayLabel));
                }
            }
        }

        [JsonProperty("itemIds")]
        public ObservableCollection<string> ItemIds { get; } = new ObservableCollection<string>();

        [JsonIgnore]
        public string DisplayLabel
        {
            get
            {
                var itemList = ItemIds.Count == 0 ? "No items" : string.Join(" / ", ItemIds);
                return $"{Quantity}x {itemList}";
            }
        }

        public void CopyFrom(ChemistryRecipeIngredientBlueprint source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            Quantity = source.Quantity;
            ItemIds.Clear();
            foreach (var itemId in source.ItemIds)
            {
                ItemIds.Add(itemId);
            }
        }

        public ChemistryRecipeIngredientBlueprint DeepCopy()
        {
            var copy = new ChemistryRecipeIngredientBlueprint();
            copy.CopyFrom(this);
            return copy;
        }
    }
}
