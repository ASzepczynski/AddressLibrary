using AddressLibrary.Logging;
using AddressLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddressLibrary.Helpers
{
    public static class ResolveAmbiguity
    {
        /// <summary>
        /// 🆕 Próbuje rozstrzygnąć niejednoznaczność na podstawie kodu pocztowego
        /// </summary>
        public static Ulica? ResolveAmbiguityPostal(
            List<Ulica> candidates, 
            string kodPocztowy, 
            string miastoNazwa, 
            ILogger? _loadLogger)
        {
            if (candidates.Count <= 1)
                return candidates.FirstOrDefault();

            // STRATEGIA 1: Lista cech w kolejności priorytetu
            var cechyPriorytet = new[] { "ul.", "Al.", "Pl." };

            foreach (var cecha in cechyPriorytet)
            {
                var matches = candidates.Where(u => u.Cecha == cecha).ToList();
                var pominieteCechy = candidates.Where(u => u.Cecha != cecha).Select(x => x.Cecha).ToList();
                if (matches.Count == 1)
                {
                    _loadLogger?.LogError($"[UlicaMatcher] ✓ Wybrano cechę {kodPocztowy} {miastoNazwa} '{cecha}': '{UliceUtils.GetPelnaNazwa(matches[0])}'");
                    if (pominieteCechy.Count > 0)
                    {
                        _loadLogger?.LogError($"[UlicaMatcher] Pominięto cechy: {string.Join(", ", pominieteCechy)}");
                    }
                    return matches[0];
                }
            }

            _loadLogger?.LogError($"[UlicaMatcher] ✗ Nie można rozstrzygnąć - zwracam null");
            return null;
        }

    }
}
