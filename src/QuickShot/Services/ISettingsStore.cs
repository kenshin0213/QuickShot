using QuickShot.Models;

namespace QuickShot.Services;

public interface ISettingsStore
{
    AppSettings Load();
    void Save(AppSettings settings);
}
