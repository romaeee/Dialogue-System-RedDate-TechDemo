using System;
using System.Collections.Generic;
using UnityEngine;

public static class SaveMigrationSystem
{
    private static readonly List<ISaveMigration> migrations = new List<ISaveMigration>();

    public static string MigrateToCurrentVersion(string json, int currentVersion)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return json;
        }

        SaveVersionHeader header = JsonUtility.FromJson<SaveVersionHeader>(json);
        int version = header != null && header.version > 0 ? header.version : 1;

        while (version < currentVersion)
        {
            ISaveMigration migration = FindMigration(version);
            if (migration == null)
            {
                throw new InvalidOperationException($"Missing save migration from version {version}.");
            }

            json = migration.Migrate(json);
            version = migration.ToVersion;
        }

        return json;
    }

    public static void RegisterMigration(ISaveMigration migration)
    {
        if (migration == null || migrations.Contains(migration))
        {
            return;
        }

        migrations.Add(migration);
    }

    private static ISaveMigration FindMigration(int fromVersion)
    {
        for (int i = 0; i < migrations.Count; i++)
        {
            if (migrations[i].FromVersion == fromVersion)
            {
                return migrations[i];
            }
        }

        return null;
    }

    #pragma warning disable 0649
    [Serializable]
    private sealed class SaveVersionHeader
    {
        public int version;
    }
    #pragma warning restore 0649
}
