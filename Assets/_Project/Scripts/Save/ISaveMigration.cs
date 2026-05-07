public interface ISaveMigration
{
    int FromVersion { get; }
    int ToVersion { get; }
    string Migrate(string json);
}
