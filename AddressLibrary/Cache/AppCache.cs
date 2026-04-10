using AddressLibrary.Data;

namespace AddressLibrary.Cache
{
    /// <summary>
    /// Agregat wszystkich cache'ów aplikacji.
    /// U¿ycie:
    ///   var cache = new AppCache(context);
    ///   await cache.InitializeAsync();                        // pe³na inicjalizacja (z kodami pocztowymi)
    ///   await cache.InitializeAsync(includeKodyPocztowe: false); // bez kodów (np. przy budowie hierarchii)
    /// </summary>
    public class AppCache
    {
        public CechyUlicCache     CechyUlic     { get; }
        public TytulyStopnieCache TytulyStopnie { get; }
        public MiastaCache        Miasta        { get; }
        public UliceCache         Ulice         { get; }
        public KodyPocztoweCache  KodyPocztowe  { get; }

        public AppCache(AddressDbContext context)
        {
            CechyUlic     = new CechyUlicCache(context);
            TytulyStopnie = new TytulyStopnieCache(context);
            Miasta        = new MiastaCache(context);
            Ulice         = new UliceCache(context);
            KodyPocztowe  = new KodyPocztoweCache(context);
        }

        /// <summary>
        /// Inicjalizuje wszystkie cache'y.
        /// </summary>
        /// <param name="includeKodyPocztowe">
        /// true  — inicjalizuj te¿ KodyPocztoweCache (wyszukiwanie, urzêdy skarbowe, weryfikacja)<br/>
        /// false — pomiñ KodyPocztoweCache (budowanie hierarchii, ³adowanie kodów PNA — dane jeszcze nie istniej¹)
        /// </param>
        public async Task InitializeAsync(bool includeKodyPocztowe = true)
        {
            await CechyUlic.InitializeAsync();
            await TytulyStopnie.InitializeAsync();
            await Miasta.InitializeAsync();
            await Ulice.InitializeAsync();
            if (includeKodyPocztowe)
                await KodyPocztowe.InitializeAsync();
        }

        /// <summary>
        /// Uniewa¿nia wszystkie cache'y (wymuœ prze³adowanie przy nastêpnym InitializeAsync).
        /// </summary>
        public void InvalidateAll()
        {
            CechyUlic.Invalidate();
            TytulyStopnie.Invalidate();
            Miasta.Invalidate();
            Ulice.Invalidate();
            KodyPocztowe.Invalidate();
        }

        /// <summary>
        /// Uniewa¿nia tylko KodyPocztoweCache — np. po za³adowaniu nowych danych PNA.
        /// </summary>
        public void InvalidateKodyPocztowe() => KodyPocztowe.Invalidate();
    }
}
