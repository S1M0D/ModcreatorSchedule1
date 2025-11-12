using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Schedule1ModdingTool.Models
{
    /// <summary>
    /// Represents a custom field in the quest data class
    /// </summary>
    public class DataClassField : ObservableObject
    {
        private string _fieldName = "";
        private DataClassFieldType _fieldType = DataClassFieldType.Bool;
        private string _defaultValue = "";
        private string _comment = "";

        [Required(ErrorMessage = "Field name is required")]
        [JsonProperty("fieldName")]
        public string FieldName
        {
            get => _fieldName;
            set => SetProperty(ref _fieldName, value ?? "");
        }

        [JsonProperty("fieldType")]
        public DataClassFieldType FieldType
        {
            get => _fieldType;
            set => SetProperty(ref _fieldType, value);
        }

        [JsonProperty("defaultValue")]
        public string DefaultValue
        {
            get => _defaultValue;
            set => SetProperty(ref _defaultValue, value ?? "");
        }

        [JsonProperty("comment")]
        public string Comment
        {
            get => _comment;
            set => SetProperty(ref _comment, value ?? "");
        }

        public DataClassField DeepCopy()
        {
            return new DataClassField
            {
                FieldName = FieldName,
                FieldType = FieldType,
                DefaultValue = DefaultValue,
                Comment = Comment
            };
        }
    }

    public enum DataClassFieldType
    {
        Bool,
        Int,
        Float,
        String,
        ListString
    }
}

