namespace Azonera.UI
{
    /// <summary>
    /// Globalny, lekki stan UI. Pozwala gameplayowi (ruch, walka) wiedzieć,
    /// że otwarte jest okno modalne i wstrzymać reakcję na klik/klawisze.
    /// </summary>
    public static class UIState
    {
        /// <summary>Czy otwarte jest okno blokujące świat (dialog, menu).</summary>
        public static bool ModalOpen { get; set; }

        /// <summary>Czy kursor jest nad panelem UI (żeby klik nie szedł do świata).</summary>
        public static bool PointerOverUI { get; set; }

        public static bool BlockWorldInput => ModalOpen;
    }
}
