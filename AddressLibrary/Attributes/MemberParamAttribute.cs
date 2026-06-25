using System;

namespace AddressLibrary.Attributes
{
    /// <summary>
    /// Kompatybilny atrybut zastępczy. Używany tymczasowo dla kompatybilności
    /// z kodem oczekującym MemberParam. Preferowane: TableVisible + Display.
    /// </summary>
    [Obsolete("MemberParam is deprecated. Use TableVisible and Display instead.")]
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class MemberParamAttribute : Attribute
    {
        public string? Description { get; set; }

        public string? Desc
        {
            get => Description;
            set => Description = value;
        }

        public bool Visible { get; set; } = true;
    }
}
