using System;

namespace AddressLibrary.Attributes
{
    /// <summary>
    /// Parametry konfiguracyjne dla ca³ej tabeli/encji
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TableParamAttribute : Attribute
    {

        /// <summary>
        /// Pe³ny opis tablicy wyœwietlany w interfejsie u¿ytkownika
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Skrócony alias dla Description
        /// </summary>
        public string? Desc
        {
            get => Description;
            set => Description = value;
        }

        /// <summary>
        /// Tryb wyboru gdy ta encja jest u¿ywana jako Foreign Key w innych tabelach
        /// </summary>
        public ChoiceMode Choice { get; set; } = ChoiceMode.Standard;
    }

    /// <summary>
    /// Tryb wyboru dla relacji Foreign Key
    /// </summary>
    public enum ChoiceMode
    {
        /// <summary>
        /// Standardowa lista rozwijana (dropdown) - dla ma³ych zbiorów danych
        /// </summary>
        Standard = 0,

        /// <summary>
        /// Tryb dla du¿ych zbiorów - wyœwietlanie tylko opisu + przycisk wyboru
        /// </summary>
        Huge = 1
    }
}