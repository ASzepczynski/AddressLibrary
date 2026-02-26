// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using System.Reflection;

namespace AddressLibrary.Utils
{
    /// <summary>
    /// Narzędzie do kopiowania obiektów
    /// </summary>
    public static class ObjectCopier
    {
        /// <summary>
        /// Tworzy płytką kopię obiektu, kopiując wszystkie publiczne właściwości
        /// </summary>
        /// <typeparam name="T">Typ obiektu do skopiowania</typeparam>
        /// <param name="source">Obiekt źródłowy</param>
        /// <returns>Nowa instancja z skopiowanymi wartościami</returns>
        public static T ShallowCopy<T>(T source) where T : class, new()
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var target = new T();

            var properties = typeof(T).GetProperties(
                BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite);

            foreach (var property in properties)
            {
                var value = property.GetValue(source);
                property.SetValue(target, value);
            }

            return target;
        }

        /// <summary>
        /// Tworzy kopię obiektu, kopiując wybrane właściwości z możliwością modyfikacji
        /// </summary>
        /// <typeparam name="T">Typ obiektu do skopiowania</typeparam>
        /// <param name="source">Obiekt źródłowy</param>
        /// <param name="modifications">Akcja modyfikująca skopiowany obiekt</param>
        /// <returns>Nowa instancja z skopiowanymi i zmodyfikowanymi wartościami</returns>
        public static T CopyWith<T>(T source, Action<T>? modifications = null) where T : class, new()
        {
            var copy = ShallowCopy(source);
            modifications?.Invoke(copy);
            return copy;
        }
    }
}