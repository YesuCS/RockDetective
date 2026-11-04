# Rock Detective Uninstall Guide

The package ships with uninstall support (`uninstall/deletefile.lst` plus the migration `Down()` method), but Rock's package manager does not yet run plugin down-migrations. A clean removal is file deletion plus a short SQL pass, then some optional housekeeping in Rock.

Back up the database before you start. The SQL below deletes case data for good.

## Remove the files

Use the package manager's uninstall if your Rock version supports it, or delete these by hand:

```
RockWeb\Bin\com.yesuchum.RockDetective.dll
RockWeb\Plugins\com_yesuchum\RockDetective\   (whole folder)
```

## Remove the data

Run this against the Rock database. Order matters; children first.

```sql
DELETE FROM [PluginMigration] WHERE [PluginAssemblyName] LIKE 'com.yesuchum.RockDetective%';

DROP TABLE IF EXISTS [_com_yesuchum_RockDetective_CaseStatement];
DROP TABLE IF EXISTS [_com_yesuchum_RockDetective_Experiment];
DROP TABLE IF EXISTS [_com_yesuchum_RockDetective_Hypothesis];
DROP TABLE IF EXISTS [_com_yesuchum_RockDetective_CaseEntry];
DROP TABLE IF EXISTS [_com_yesuchum_RockDetective_CaseParticipant];
DROP TABLE IF EXISTS [_com_yesuchum_RockDetective_CaseFile];
```

If anyone was ever sent a witness statement request, Rock Detective also created a reminder type with reminders under it. Remove those before you delete the entity types:

```sql
DELETE r FROM [Reminder] r
INNER JOIN [ReminderType] rt ON rt.[Id] = r.[ReminderTypeId]
WHERE rt.[Guid] = 'A746FC6B-B670-4EF7-AC4D-D99998031480';

DELETE FROM [ReminderType] WHERE [Guid] = 'A746FC6B-B670-4EF7-AC4D-D99998031480';
```

## Remove the Rock configuration

All of this is optional housekeeping; none of it breaks anything if left behind.

- **Pages:** Admin Tools > Installed Plugins > Rock Detective. Delete the child pages first (Case Board, Witness Statement).
- **Block types:** the three under the "Rock Detective" category.
- **Security role:** the **Rock Detective Users** group.
- **System communications:** the three titled "Rock Detective ..." under Admin Tools > Communications > System Communications.
- **Jobs:** **Rock Detective Witness Reminders** and **Rock Detective Follow-Up Checks**.
- **Entity types:** the rows named `com.yesuchum.RockDetective.*`. Delete any that are still there after the restart.
- **Categories:** the four seeded Case File categories (Software Bug, Performance, Process, Ministry Operations).

## Restart

Restart the Rock application. Check that the Installed Plugins page no longer lists Rock Detective and that the exception log stays quiet.
