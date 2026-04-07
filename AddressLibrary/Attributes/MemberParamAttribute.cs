using System;

namespace AddressLibrary.Attributes
{
    /// <summary>
    /// Parametry konfiguracyjne dla w³aœciwoœci/cz³onka klasy
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class MemberParamAttribute : Attribute
    {
        /// <summary>
        /// Pe³ny opis kolumny wyœwietlany w interfejsie u¿ytkownika
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
    }
}