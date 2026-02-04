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
        /// 🆕 Próbuje rozstrzygnąć niejednoznaczność na podstawie hierarchii cech ulic
        /// </summary>
        public static Ulica? ResolveAmbiguityPostal(
            List<Ulica> candidates,
            string kodPocztowy,
            string miastoNazwa,
            ILogger? _loadLogger)
        {
            if (candidates.Count <= 1)
                return candidates.FirstOrDefault();

            var caseId = $"{kodPocztowy}_{miastoNazwa}_{DateTime.Now:HHmmss.fff}";
            
            // ✅ POPRAWKA: Case-insensitive porównanie
            var cechyPriorytet = new[] { "ul.", "al.", "pl." };  // ← WSZYSTKO lowercase!
            
            _loadLogger?.LogWarning($"[ResolveAmbiguity #{caseId}] ✓ Wykryto niejednoznaczność - próba rozstrzygnięcia");

            foreach (var ulica in candidates)
            {
                var line = $"{ulica.Cecha} {ulica.Nazwa2} {ulica.Nazwa1} {ulica.Dzielnica ?? ""}".Trim();
                _loadLogger?.LogInfo($"[ResolveAmbiguity #{caseId}] Kandydat: {line}");
            }

            foreach (var cecha in cechyPriorytet)
            {
                // ✅ POPRAWKA: Porównanie case-insensitive
                var matches = candidates
                    .Where(u => u.Cecha.Equals(cecha, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                    
                if (matches.Count == 1)
                {
                    var pominieteCechy = candidates
                        .Where(u => !u.Cecha.Equals(cecha, StringComparison.OrdinalIgnoreCase))
                        .Select(x => x.Cecha)
                        .Distinct()
                        .ToList();
                        
                    _loadLogger?.LogInfo($"[ResolveAmbiguity #{caseId}] ✓ Wybrano cechę '{cecha}': '{UliceUtils.GetPelnaNazwa(matches[0])}'");
                    
                    if (pominieteCechy.Count > 0)
                    {
                        _loadLogger?.LogInfo($"[ResolveAmbiguity #{caseId}] Pominięto cechy: {string.Join(", ", pominieteCechy)}");
                    }
                    return matches[0];
                }
            }

            _loadLogger?.LogError($"[ResolveAmbiguity #{caseId}] ✗ Nie można rozstrzygnąć - zwracam null");
            return null;
        }

    }
}
