using AddressLibrary.Logging;
using AddressLibrary.Models;
using AddressLibrary.Services.AddressSearch;
using DocumentFormat.OpenXml.Vml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UglyToad.PdfPig.Core.PdfSubpath;

namespace AddressLibrary.Helpers
{
    public static class ResolveAmbiguity
    {
        /// <summary>
        /// 🆕 Próbuje rozstrzygnąć niejednoznaczność na podstawie hierarchii cech ulic
        /// </summary>
        public static Ulica? ResolveStreetAmbiguity(
            List<Ulica> candidates,
            string sPrefiks,
            string sStreet,
            string sDzielnica,
            string kodPocztowy,
            string miastoNazwa,
            AddressSearchCache? cache,
            GeneralLogger? _PostalCodesLogger)
        {
            if (candidates.Count <= 1)
                return candidates.FirstOrDefault();

            
            _PostalCodesLogger?.LogWarning($"[ResolveAmbiguity] ✓ Wykryto niejednoznaczność - próba rozstrzygnięcia, szukany prefiks '{sPrefiks}'");

            foreach (var ulica in candidates)
            {
                  string kody = ulica.KodyPocztowe != null && ulica.KodyPocztowe.Any()
                       ? string.Join(", ", ulica.KodyPocztowe
                           .Select(k => k.Kod)
                           .Where(kod => !string.IsNullOrWhiteSpace(kod))
                           .Distinct()
                           .OrderBy(k => k))
                       : "brak";
                var line = $"{ulica.Cecha}|{ulica.Nazwa2}|{ulica.Nazwa1}|{ulica.Dzielnica ?? ""}|{kody}".Trim();
                _PostalCodesLogger?.LogInfo($"[ResolveAmbiguity] Kandydat: {line}");
            }

// Porównanie dokładne po nazwie
            var Pasujace = new List<Ulica>();
            foreach (var ulica in candidates)
            {
                if ((ulica.Nazwa2+" "+ulica.Nazwa1).Trim() == sStreet)
                {
                    Pasujace.Add(ulica);
                }
            }

            if (Pasujace.Count() == 1)
            {
                _PostalCodesLogger?.LogInfo($"Istnieje dokładnie jeden obiekt dokładnie zgodny z [{sStreet}]");
                return Pasujace[0];
            }

            _PostalCodesLogger?.LogInfo($"Niestety istnieje {Pasujace.Count} obiektów dokładnie zgodnych z ulicą [{sStreet}]");

            Pasujace.Clear();
            // Porównanie po prefiksie
			foreach (var ulica in candidates)
			{
                if (ulica.Cecha == sPrefiks || ulica.Cecha == sPrefiks)
				{
					Pasujace.Add(ulica);
				}
			}

			if (Pasujace.Count() == 1)
            {
				_PostalCodesLogger?.LogInfo($"Istnieje dokładnie jeden obiekt z prefiksem [{sPrefiks}]");
				return Pasujace[0];
			}
            _PostalCodesLogger?.LogInfo($"Niestety istnieje {Pasujace.Count} obiektów dokładnie zgodnych z prefiksem [{sPrefiks}]");

            Pasujace.Clear();


// Porównanie po kodzie pocztowym
            if (!string.IsNullOrWhiteSpace(kodPocztowy))
            {
                _PostalCodesLogger?.LogWarning($"[ResolveAmbiguity] ✓ Szukanie po kodzie pocztowym '{kodPocztowy}'");
                var kodNormalized = UliceUtils.NormalizujKodPocztowy(kodPocztowy);

                foreach (var ulica in candidates)
                {
                    if(cache.TryGetKodyPocztoweUlicy(ulica.Id, out List<KodPocztowy> kody)) {
                        // ✅ Sprawdź czy ulica ma przypisany ten kod pocztowy
                        if (kody.Select(x=>x.Kod).Contains(kodNormalized))
                        {
                            Pasujace.Add(ulica);
                        }
                    }
                }
            }

            if (Pasujace.Count() == 1)
            {
                _PostalCodesLogger?.LogInfo($"Istnieje dokładnie jeden obiekt z kodem [{kodPocztowy}]");
                return Pasujace[0];
            }
            _PostalCodesLogger?.LogInfo($"Niestety istnieje {Pasujace.Count} obiektów dokładnie zgodnych z kodem [{kodPocztowy}]");

            _PostalCodesLogger?.LogWarning($"[ResolveAmbiguity] Szukanie po priorytecie ulic");

            // ✅ POPRAWKA: Case-insensitive porównanie
            var cechyPriorytet = new[] { "ul.", "al.", "pl." };  // ← WSZYSTKO lowercase!

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
                        
                    _PostalCodesLogger?.LogInfo($"[ResolveAmbiguity] ✓ Wybrano cechę '{cecha}': '{UliceUtils.GetPelnaNazwa(matches[0])}'");
                    
                    if (pominieteCechy.Count > 0)
                    {
                        _PostalCodesLogger?.LogInfo($"[ResolveAmbiguity] Pominięto cechy: {string.Join(", ", pominieteCechy)}");
                    }
                    return matches[0];
                }
            }
            _PostalCodesLogger?.LogError($"[ResolveAmbiguity] ✗ Nie można rozstrzygnąć - zwracam null");
            return null;
        }

    }
}
