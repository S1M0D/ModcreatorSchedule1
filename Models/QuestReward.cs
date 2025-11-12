using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Schedule1ModdingTool.Models
{
    /// <summary>
    /// Represents a quest reward configuration
    /// </summary>
    public class QuestReward : ObservableObject
    {
        private QuestRewardType _rewardType = QuestRewardType.Money;
        private int _amount = 100;
        private string _itemId = "";
        private int _quantity = 1;

        [JsonProperty("rewardType")]
        public QuestRewardType RewardType
        {
            get => _rewardType;
            set => SetProperty(ref _rewardType, value);
        }

        [Range(1, int.MaxValue, ErrorMessage = "Amount must be positive")]
        [JsonProperty("amount")]
        public int Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        [JsonProperty("itemId")]
        public string ItemId
        {
            get => _itemId;
            set => SetProperty(ref _itemId, value ?? "");
        }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        [JsonProperty("quantity")]
        public int Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        public QuestReward DeepCopy()
        {
            return new QuestReward
            {
                RewardType = RewardType,
                Amount = Amount,
                ItemId = ItemId,
                Quantity = Quantity
            };
        }
    }

    public enum QuestRewardType
    {
        XP,
        Money,
        Item
    }
}

