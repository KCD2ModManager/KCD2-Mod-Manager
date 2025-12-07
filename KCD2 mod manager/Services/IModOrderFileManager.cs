namespace KCD2_mod_manager.Services
{
    /// <summary>
    /// Interface für die Verwaltung von mod_order.txt und mod_order_backup.txt
    /// WICHTIG: Verwaltet nur die MAIN/Global-Mod-Order-Dateien, nicht Profile-spezifische
    /// </summary>
    public interface IModOrderFileManager
    {
        /// <summary>
        /// Wendet das ModOrderEnabled-Setting an:
        /// - Wenn enabled=true: Verschiebt mod_order_backup.txt -> mod_order.txt (falls Backup existiert)
        /// - Wenn enabled=false: Verschiebt mod_order.txt -> mod_order_backup.txt und löscht mod_order.txt
        /// </summary>
        Task ApplyModOrderSettingAsync(bool enabled, string modFolder, CancellationToken cancellationToken = default);

        /// <summary>
        /// Konsolidiert die Mod-Order-Dateien beim App-Start basierend auf dem aktuellen Setting
        /// </summary>
        Task ConsolidateModOrderFilesAsync(bool modOrderEnabled, string modFolder, CancellationToken cancellationToken = default);
    }
}

